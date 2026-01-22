using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class OptimizerBatcher : Generator3DOptimizer
    {
        public override void Optimize(GameObject rootObject)
        {
            // only batching statics
            List<GameObject> staticObjects = new();
            CollectStaticMeshObjects(rootObject.transform, staticObjects);

            if (staticObjects.Count == 0) return;

            // reapplies static to objs, we only need those generated decalred as static
            StaticBatchingUtility.Combine(staticObjects.ToArray(), rootObject);

            Debug.Log($"[OptimizerBatcher] Batched {staticObjects.Count} static objects.");
        }

        private void CollectStaticMeshObjects(Transform root, List<GameObject> results)
        {
            foreach (Transform child in root)
            {
                GameObject go = child.gameObject;

                if (go.isStatic &&
                    go.TryGetComponent<MeshFilter>(out _) &&
                    go.TryGetComponent<MeshRenderer>(out _))
                {
                    results.Add(go);
                }

                CollectStaticMeshObjects(child, results);
            }
        }
    }
}
