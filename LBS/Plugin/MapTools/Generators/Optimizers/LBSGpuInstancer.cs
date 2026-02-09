using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class LBSGpuInstancer : MonoBehaviour
    {
        [Serializable]
        public class SubMeshBatch
        {
            [SerializeField]
            public Mesh mesh;

            [SerializeField]
            public int subMeshIndex;

            [SerializeField]
            public Material material;

            [SerializeField]
            public List<Matrix4x4> matrices = new();
            
            [SerializeField][HideInInspector] 
            public List<Matrix4x4[]> chunks = new();

            public SubMeshBatch(Mesh mesh, int sub, Material mat)
            {
                this.mesh = mesh;
                this.subMeshIndex = sub;
                this.material = mat;
            }

            public override bool Equals(object obj)
            {
                if (obj is not SubMeshBatch other) return false;
                return mesh == other.mesh &&
                       subMeshIndex == other.subMeshIndex &&
                       material == other.material;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (mesh ? mesh.GetHashCode() : 0);
                    hash = hash * 23 + subMeshIndex.GetHashCode();
                    hash = hash * 23 + (material ? material.GetHashCode() : 0);
                    return hash;
                }
            }
        }

        #region FIELDS

        [SerializeField] 
        private List<SubMeshBatch> batches = new();

        private RenderParams _renderParams;

        #endregion

        #region METHODS

        private void Awake() => Build();

        void LateUpdate()
        {

            foreach (SubMeshBatch batch in batches)
            {
                if (batch.mesh == null || batch.material == null) continue;

                foreach (Matrix4x4[] chunk in batch.chunks)
                {
                    RenderParams rp = new(batch.material)
                    {
                        matProps = _renderParams.matProps
                    };
                    Graphics.RenderMeshInstanced(rp, batch.mesh, batch.subMeshIndex, chunk);
                }
            }
        }

        private static SubMeshBatch FindBatch(List<SubMeshBatch> batches, Mesh mesh, int subMesh, Material material)
        {
            foreach (SubMeshBatch batch in batches)
            {
                if (batch.mesh == mesh && batch.subMeshIndex == subMesh && batch.material == material)
                {
                    return batch;
                }
            }

            return null;
        }


        public void AddBatch(Mesh mesh, int subMesh, Material material, Matrix4x4 localToWorld)
        {
            SubMeshBatch batch = FindBatch(batches, mesh, subMesh, material);

            if (batch == null)
            {
                batch = new SubMeshBatch(mesh, subMesh, material);
                batches.Add(batch);
            }

            batch.matrices.Add(localToWorld);
        }

        public void Build()
        {
            foreach (SubMeshBatch batch in batches)
            {
                batch.chunks ??= new List<Matrix4x4[]>();
                batch.chunks.Clear();

                int count = batch.matrices.Count;
                int i = 0;

                while (i < count)
                {
                    int len = Mathf.Min(1023, count - i);
                    Matrix4x4[] chunk = new Matrix4x4[len];
                    batch.matrices.CopyTo(i,  chunk, 0, len);
                    batch.chunks.Add(chunk);
                    i += len;
                }

                if (!batch.material) return;

                // material must be gpuinstansable}
                batch.material.enableInstancing = true;
            }

            _renderParams = new RenderParams(batches[0].material)
            {
                matProps = new MaterialPropertyBlock()
            };
        }

        #endregion
    }
}


