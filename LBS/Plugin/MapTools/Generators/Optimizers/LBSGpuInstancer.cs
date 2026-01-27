using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LBSGpuInstancer : MonoBehaviour
{
    [System.Serializable]
    public class SubMeshBatch
    {
        public Mesh mesh;
        public int subMeshIndex;
        public Material material;

        // per-instance transforms (world space)
        public List<Matrix4x4> matrices = new();

        // cached chunks of ≤1023
        [HideInInspector] public List<Matrix4x4[]> chunks = new();
    }

    [SerializeField] private List<SubMeshBatch> batches = new();

    private RenderParams _renderParams;
    private bool _initialized;

    public void AddInstance(
    Mesh mesh,
    int subMesh,
    Material material,
    Matrix4x4 localToWorld)
    {
        SubMeshBatch batch = batches.Find(b =>
            b.mesh == mesh &&
            b.subMeshIndex == subMesh &&
            b.material == material);

        if (batch == null)
        {
            batch = new SubMeshBatch
            {
                mesh = mesh,
                subMeshIndex = subMesh,
                material = material
            };
            batches.Add(batch);
        }

        batch.matrices.Add(localToWorld);
    }

    public void Build()
    {
        foreach (SubMeshBatch batch in batches)
        {
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

        _initialized = true;
    }

    private void Awake()
    {
        Build();
    }

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
}


