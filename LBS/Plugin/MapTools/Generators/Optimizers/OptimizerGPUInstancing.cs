using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.Plugin.MapTools.Generators.LBSGpuInstancer;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class OptimizerGPUInstancing : Generator3DOptimizer
    {
        public override void Optimize(GameObject rootObject)
        {
            Dictionary<Bundle, List<GameObject>> decoratives = new();
            GetDecorativesRecursive(rootObject.transform, decoratives);

            foreach (KeyValuePair<Bundle, List<GameObject>> decoration in decoratives)
            {
                BuildGPUInstancer(decoration);
            }
        }

        private void BuildGPUInstancer(KeyValuePair<Bundle, List<GameObject>> decorator)
        {
            Bundle bundle = decorator.Key;
            List<GameObject> objects = decorator.Value;

            if (objects.Count <= 1) return;

            HashSet<SubMeshBatch> subMeshBatches = new();

            foreach (GameObject entry in objects)
            {
                foreach (MeshRenderer mr in GetLODRender(entry, 99))
                {
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (!mf || !mf.sharedMesh) continue;

                    Mesh mesh = mf.sharedMesh;
                    Material[] mats = mr.sharedMaterials;
                    if (mats == null || mats.Length == 0) continue;

                    int subCount = Mathf.Min(mesh.subMeshCount, mats.Length);

                    for (int sub = 0; sub < subCount; sub++)
                    {
                        Material mat = mats[sub];
                        if (!mat) continue;

                        mat.enableInstancing = true;

                        SubMeshBatch key = new(mesh, sub, mat);

                        if (!subMeshBatches.TryGetValue(key, out SubMeshBatch batch))
                        {
                            batch = key;
                            subMeshBatches.Add(batch);
                        }

                        batch.matrices.Add(mf.transform.localToWorldMatrix);
                    }

                    mr.enabled = false;
                }
            }

            if (subMeshBatches.Count == 0) return;

            GameObject parent = objects[0].transform.parent.gameObject;

            LBSGpuInstancer instancer =
                parent.TryGetComponent(out LBSGpuInstancer existing)
                    ? existing
                    : parent.AddComponent<LBSGpuInstancer>();

            foreach (SubMeshBatch batch in subMeshBatches)
            {
                if (batch.matrices.Count <= 1) continue;

                foreach (Matrix4x4 m in batch.matrices)
                {
                    instancer.AddBatch(batch.mesh, batch.subMeshIndex, batch.material, m);
                }
            }

            instancer.Build();

            Debug.Log(
                $"[GPU Instancing] Bundle '{bundle.name}' → " +
                $"{objects.Count} objects, {subMeshBatches.Count} batches."
            );
        }


        /// <summary>
        /// Gets a Level Of Detail from the mesh render of a gameobject
        /// </summary>
        /// <param name="go">game object to get the LODS from</param>
        /// <param name="desiredLOD">desired LOD; lowest index highest detail. Default of 0</param>
        /// <returns>meshrenderes in their desired LOD if it exists, else its default render</returns>
        private static IEnumerable<MeshRenderer> GetLODRender(GameObject go, int desiredLOD = 0)
        {
            LODGroup[] lodGroups = go.GetComponentsInChildren<LODGroup>(true);

            if (lodGroups.Length > desiredLOD)
            {
                foreach (LODGroup lg in lodGroups)
                {
                    LOD[] lods = lg.GetLODs();
                    if (lods == null || !lods.Any()) continue;

                    desiredLOD = Mathf.Clamp(desiredLOD, 0, lodGroups.Length-1);

                    foreach (Renderer r in lods[desiredLOD].renderers)
                    {
                        if (r is MeshRenderer mr) yield return mr;
                    }
                }
            }
            else
            {
                // No LODGroup get all renders
                foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true)) yield return mr;
            }
        }

        /// <summary>
        /// Recursively try to get all gameobjects with the "Decorative" lbs characteristic tag
        /// </summary>
        /// <param name="transform">transform of object</param>
        /// <param name="result">list of game objects that can be gpu instanced</param>
        private void GetDecorativesRecursive(Transform transform, Dictionary<Bundle, List<GameObject>> result)
        {
            if (transform.gameObject.TryGetComponent(out LBSGenerated lbsgen))
            {
                Bundle bundle = lbsgen.BundleRef;
                if (bundle != null && bundle.GetHasTagCharacteristic("Decorative"))
                {
                    if (!result.TryGetValue(bundle, out List<GameObject> list))
                    {
                        list = new List<GameObject>();
                        result[bundle] = list;
                    }

                    list.Add(transform.gameObject);
                }
            }

            foreach (Transform child in transform) GetDecorativesRecursive(child, result);
        }
    }
}
