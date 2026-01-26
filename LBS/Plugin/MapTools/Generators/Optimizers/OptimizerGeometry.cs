using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class OptimizerGeometry : Generator3DOptimizer
    {

        private const string combinedPrefix = "Combined_";
        private static readonly Bundle.EElementFlag[] combineExceptions =
            
            {
                Bundle.EElementFlag.Character,
                Bundle.EElementFlag.Item,
                Bundle.EElementFlag.Interactable
            };


        public override void Optimize(GameObject rootObject)
        {
            // immediate children only (layers)
            foreach (Transform group in rootObject.transform) 
            {
                TryCombine(group.gameObject);
            }
        }

        private void TryCombine(GameObject groupRoot)
        {
            // Remove previous combined meshes
            CleanPreviousCombined(groupRoot);

            // get mesh entries (children and submeshes)
            List<MeshEntry> entries = ExtractMeshes(groupRoot);
            if (entries.Count == 0) return;

            // group in dicot, using (mesh + material + submesh)
            Dictionary<MeshID, List<MeshEntry>> groups = GroupMeshes(entries);
            CombineColliders(groupRoot, entries);

            // track combined mesh renders
            HashSet<MeshRenderer> disabledRenderers = new();

            // combine groups by compatibility. i.e same mesh and materials(order matters)
            CombineMeshes(groupRoot, groups, disabledRenderers);

            // if combined disable original render
            foreach (MeshRenderer r in disabledRenderers)
            {
                if (r != null) r.enabled = false;

            }
        }

        private static void CombineColliders(GameObject groupRoot, List<MeshEntry> entries)
        {
            HashSet<MeshFilter> uniqueFilters = new();
            List<CombineInstance> physicsCombines = new();

            foreach (MeshEntry entry in entries)
            {
                if (uniqueFilters.Add(entry.Filter))
                {
                    CombineInstance pci = new CombineInstance
                    {
                        mesh = entry.Filter.sharedMesh,
                        subMeshIndex = -1, // ALL submeshes
                        transform = entry.Filter.transform.localToWorldMatrix
                    };

                    physicsCombines.Add(pci);
                }
            }

         
        }

        private static void CombineMeshes(GameObject groupRoot, Dictionary<MeshID, List<MeshEntry>> groups, HashSet<MeshRenderer> disabledRenderers)
        {


            foreach (KeyValuePair<MeshID, List<MeshEntry>> pair in groups)
            {
                MeshID id = pair.Key;
                List<MeshEntry> list = pair.Value;

                if (list.Count <= 1) continue;

                List<CombineInstance> meshCombines = new();
                HashSet<CombineInstance> physicsCombines = new();

                Matrix4x4 rootWorldToLocal = groupRoot.transform.worldToLocalMatrix;

                foreach (MeshEntry entry in list)
                {
                    // validity check
                    if (!entry.Renderer.gameObject.TryGetComponent(out LBSGenerated lbsgen))
                        continue;

                    if (lbsgen.BundleRef.HasAnyFlag(combineExceptions))
                        continue;

                    Matrix4x4 localMatrix =
                        rootWorldToLocal * entry.Filter.transform.localToWorldMatrix;

                    // render mesh
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = entry.Filter.sharedMesh,
                        subMeshIndex = id.SubMesh,
                        transform = localMatrix
                    };

                    meshCombines.Add(ci);
                    // collider mesh
                    CombineInstance pci = new CombineInstance
                    {
                        mesh = entry.Filter.sharedMesh,
                        subMeshIndex = -1, // all submeshes
                        transform = localMatrix
                    };

                    physicsCombines.Add(pci);

                    disabledRenderers.Add(entry.Renderer);
                }


                Mesh combinedMesh = new()
                {
                    indexFormat = IndexFormat.UInt32
                };

                combinedMesh.CombineMeshes(meshCombines.ToArray(), true, true);
                combinedMesh.RecalculateBounds();
                combinedMesh.RecalculateNormals();

                GameObject combinedGO = new GameObject(
                    $"{combinedPrefix}_" +
                    $"{groupRoot.name}{id.Mesh.name}_" +
                    $"{id.Material.name}_sub{id.SubMesh}"
                );

                combinedGO.transform.SetParent(groupRoot.transform, false);
                combinedGO.isStatic = true;

                MeshFilter mf = combinedGO.AddComponent<MeshFilter>();
                MeshRenderer mr = combinedGO.AddComponent<MeshRenderer>();

                mf.sharedMesh = combinedMesh;
                mr.sharedMaterial = id.Material;
                MeshCollider col = combinedGO.AddComponent<MeshCollider>();
                col.sharedMesh = combinedMesh;
                col.convex = false; // must be false for big static terrain-like meshes
            }

        }

        private static Dictionary<MeshID, List<MeshEntry>> GroupMeshes(List<MeshEntry> entries)
        {
            Dictionary<MeshID, List<MeshEntry>> groups = new();

            foreach (MeshEntry entry in entries)
            {
                if (!groups.TryGetValue(entry.Id, out List<MeshEntry> list))
                {
                    list = new List<MeshEntry>();
                    groups[entry.Id] = list;
                }

                list.Add(entry);
            }

            return groups;
        }

        private static void CleanPreviousCombined(GameObject groupRoot)
        {
            List<GameObject> toRemove = new();
            foreach (Transform child in groupRoot.transform)
            {
                if (child.name.Contains(combinedPrefix))
                    toRemove.Add(child.gameObject);
            }

            foreach (GameObject go in toRemove) Object.DestroyImmediate(go);

            // remove old combined collider
            MeshCollider oldCol = groupRoot.GetComponent<MeshCollider>();
            if (oldCol != null) Object.DestroyImmediate(oldCol);
        }

        private List<MeshEntry> ExtractMeshes(GameObject groupRoot)
        {
            List<MeshEntry> result = new();

            MeshFilter[] filters = groupRoot.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in filters)
            {
                if (!mf.gameObject.isStatic) continue;
                if (mf.name.Contains(combinedPrefix)) continue;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;

                Mesh mesh = mf.sharedMesh;
                Material[] materials = mr.sharedMaterials;

                int subCount = Mathf.Min(mesh.subMeshCount, materials.Length);

                for (int sub = 0; sub < subCount; sub++)
                {
                    Material mat = materials[sub];
                    if (mat == null) continue;

                    MeshID id = new MeshID(mesh, mat, sub);

                    result.Add(new MeshEntry(id, mf, mr));
                }
            }

            return result;
        }
    }
}
