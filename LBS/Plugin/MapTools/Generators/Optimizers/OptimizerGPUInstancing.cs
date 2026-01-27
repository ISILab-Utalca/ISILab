using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections.Generic;
using UnityEngine;

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
                BuildGPUInstancers(rootObject, decoration);
            }
        }

        private void BuildGPUInstancers(GameObject rootObject,KeyValuePair<Bundle, List<GameObject>> decorator)
        {
            Bundle bundle = decorator.Key;
            List<GameObject> objects = decorator.Value;

            if (objects.Count <= 1)
                return;

            // Group by (mesh + material)
            Dictionary<(Mesh mesh, Material mat), List<GameObject>> groups = new();

            foreach (GameObject entry in objects)
            {
                MeshRenderer[] mrs = entry.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in mrs)
                {
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;


                    Material[] mats = mr.sharedMaterials;
                    if (mats == null || mats.Length == 0) continue;


                    for (int i = 0; i < mats.Length; i++)
                    {
                        Material mat = mats[i];
                        if (mat == null) continue;

                        mat.enableInstancing = true;

                        (Mesh sharedMesh, Material mat) key = (mf.sharedMesh, mat);

                        if (!groups.TryGetValue(key, out List<GameObject> list))
                        {
                            list = new List<GameObject>();
                            groups[key] = list;
                        }

                        list.Add(mr.gameObject);
                    }

                    // Disable original renderer (we will draw it manually)
                    mr.enabled = false;
                }
            }

            // Create one instancer component per (mesh + material)
            foreach (KeyValuePair<(Mesh mesh, Material mat), List<GameObject>> kvp in groups)
            {
                (Mesh mesh, Material mat) = kvp.Key;

                List<GameObject> objs = kvp.Value;
                List<Transform> transforms = new();

                if (objs.Count <= 1) continue;

                GameObject parent = objs[0].transform.parent.gameObject;


                foreach (GameObject obj in objs) transforms.Add(obj.transform);

                LBSGpuInstancer instancer = parent.AddComponent<LBSGpuInstancer>();

                foreach (GameObject instanceRoot in objects)
                {
                    MeshFilter[] mfs = instanceRoot.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter mf in mfs)
                    {
                        MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                        if (mr == null) continue;

                        Mesh innerMesh = mf.sharedMesh;
                        Material[] innerMat = mr.sharedMaterials;

                        int subCount = Mathf.Min(innerMesh.subMeshCount, innerMat.Length);

                        for (int sub = 0; sub < subCount; sub++)
                        {
                            Material matParam = innerMat[sub];
                            if (matParam == null) continue;

                            instancer.AddInstance(innerMesh, sub, matParam, mf.transform.localToWorldMatrix);
                        }

                        // disable original renderer
                        mr.enabled = false;
                    }
                }

                instancer.Build();

            }

            Debug.Log(
                $"[GPU Instancing] Bundle '{bundle.name}' → " +
                $"{objects.Count} objects, {groups.Count} instancing groups."
            );
        }

        private void GetDecorativesRecursive(Transform transform, Dictionary<Bundle, List<GameObject>> result)
        {
            if (transform.gameObject.TryGetComponent<LBSGenerated>(
                    out LBSGenerated lbsgen))
            {
                Bundle bundle = lbsgen.BundleRef;
                if (bundle != null &&
                    bundle.GetHasTagCharacteristic("Decorative"))
                {
                    if (!result.TryGetValue(bundle, out List<GameObject> list))
                    {
                        list = new List<GameObject>();
                        result[bundle] = list;
                    }
                    Debug.Log($"instancing prefab: {transform.gameObject}");
                    list.Add(transform.gameObject);
                }
            }

            foreach (Transform child in transform)
                GetDecorativesRecursive(child, result);
        }


    }

}
