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
                EnableGPUInstancing(decoration);
            }

        }

        private void EnableGPUInstancing(KeyValuePair<Bundle, List<GameObject>> decorator)
        {
            Bundle bundle = decorator.Key;
            List<GameObject> objects = decorator.Value;

            if (objects.Count <= 1) return; 

            List<MeshRenderer> renderers = new();

            // get renderers
            foreach (GameObject entry in objects)
            {
                MeshRenderer[] mrs = entry.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in mrs)
                {
                    if (mr.sharedMaterials == null || mr.sharedMaterials.Length == 0) continue;
                    renderers.Add(mr);
                }
            }

            if (renderers.Count <= 1) return;

            // enable instancing in material
            HashSet<Material> touchedMaterials = new();

            foreach (MeshRenderer mr in renderers)
            {
                Material[] mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;

                    if (!touchedMaterials.Contains(mat))
                    {
                        mat.enableInstancing = true;
                        touchedMaterials.Add(mat);
                    }
                }

                // (maybe add as paremter or rmeove completely ask NICO)
                mr.allowOcclusionWhenDynamic = true;
            }

            Debug.Log(
                $"[GPU Instancing] Bundle '{bundle.name}' → " +
                $"{objects.Count} objects, {renderers.Count} renderers, {touchedMaterials.Count} materials instanced."
            );
        }

        private void GetDecorativesRecursive(Transform transform, Dictionary<Bundle, List<GameObject>> result)
        {
            if (transform.gameObject.TryGetComponent<LBSGenerated>(out LBSGenerated lbsgen))
            {
                Bundle bundle = lbsgen.BundleRef;
                if (bundle != null && bundle.GetHasTagCharacteristic("Decorative"))
                {
                    if (result.ContainsKey(bundle)) result[bundle].Add(transform.gameObject);
                    else result.Add(bundle, new List<GameObject> { transform.gameObject });
                }
            }

            foreach (Transform child in transform) GetDecorativesRecursive(child, result);
        }
    }

}
