using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class OptimizerGeometry : Generator3DOptimizer
    {
        public override void Optimize(GameObject rootObject)
        {
            // immediate children only (layers)
            foreach (Transform group in rootObject.transform) 
            {
                CombineGroup(group.gameObject);
            }
        }

        private void CombineGroup(GameObject groupRoot)
        {
            // Remove previous combined meshes
            List<GameObject> toRemove = new();
            foreach (Transform child in groupRoot.transform)
            {
                if (child.name.Contains("_Combined_"))
                    toRemove.Add(child.gameObject);
            }
            foreach (GameObject go in toRemove) Object.DestroyImmediate(go);

            // get mesh entries (children and submeshes)
            List<MeshEntry> entries = ExtractMeshes(groupRoot);
            if (entries.Count == 0) return;

            // group in dicot, using (mesh + material + submesh)
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

            // Track exactly which renderers were merged
            HashSet<MeshRenderer> disabledRenderers = new();

            // combine groups by compatibility. i.e same mesh and materials(order matters)
            foreach (KeyValuePair<MeshID, List<MeshEntry>> pair in groups)
            {
                MeshID id = pair.Key;
                List<MeshEntry> list = pair.Value;

                if (list.Count <= 1)
                    continue;

                List<CombineInstance> combines = new();

                foreach (MeshEntry entry in list)
                {
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = entry.Filter.sharedMesh,
                        subMeshIndex = id.SubMesh,
                        transform = entry.Filter.transform.localToWorldMatrix
                    };

                    combines.Add(ci);
                    disabledRenderers.Add(entry.Renderer);
                }

                Mesh combinedMesh = new Mesh
                {
                    indexFormat = IndexFormat.UInt32
                };

                combinedMesh.CombineMeshes(combines.ToArray(), true, true);
                combinedMesh.RecalculateBounds();
                combinedMesh.RecalculateNormals();

                GameObject combinedGO = new GameObject(
                    $"{groupRoot.name}_Combined_{id.Mesh.name}_{id.Material.name}_sub{id.SubMesh}"
                );

                combinedGO.transform.SetParent(groupRoot.transform, false);
                combinedGO.isStatic = true;

                MeshFilter mf = combinedGO.AddComponent<MeshFilter>();
                MeshRenderer mr = combinedGO.AddComponent<MeshRenderer>();

                mf.sharedMesh = combinedMesh;
                mr.sharedMaterial = id.Material;
            }

            // if combined disable original render
            foreach (MeshRenderer r in disabledRenderers)
            {
                if (r != null) r.enabled = false;

            }
        }



        private Dictionary<MeshID, List<MeshEntry>> GroupByMeshID(Dictionary<GameObject, List<MeshEntry>> objectMeshes)
        {
            var groups = new Dictionary<MeshID, List<MeshEntry>>();

            foreach (KeyValuePair<GameObject, List<MeshEntry>> kvp in objectMeshes)
            {
                foreach (MeshEntry entry in kvp.Value)
                {
                    if (!groups.TryGetValue(entry.Id, out List<MeshEntry> list))
                    {
                        list = new List<MeshEntry>();
                        groups[entry.Id] = list;
                    }

                    list.Add(entry);
                }
            }

            return groups;
        }

        private List<MeshEntry> ExtractMeshes(GameObject groupRoot)
        {
            List<MeshEntry> result = new();

            MeshFilter[] filters = groupRoot.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in filters)
            {
                if (!mf.gameObject.isStatic) continue;
                if (mf.name.Contains("_Combined_")) continue;

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


        /// <summary>
        /// Each group corresponds to a game object per layer
        /// </summary>
        /// <param name="groupRoot">direct child of the root, from where children's meshes
        /// are combined</param>
        [System.Obsolete]
        private void CombinePerGroup(GameObject groupRoot)
        {
            // Remove previous combined meshes (important when regenerating)
            List<GameObject> toRemove = new();
            foreach (Transform child in groupRoot.transform)
            {
                if (child.name.Contains("_Combined_"))
                    toRemove.Add(child.gameObject);
            }

            foreach (GameObject go in toRemove)
                Object.DestroyImmediate(go);

            // Collect all mesh filters in this group
            MeshFilter[] filters = groupRoot.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return;

            // Key = (mesh, material, subMeshIndex)
            var combines = new Dictionary<(Mesh mesh, Material mat, int subMesh), List<(CombineInstance ci, MeshRenderer renderer)>>();

            // Track exactly which renderers were merged
            HashSet<MeshRenderer> combinedRenderers = new();

            foreach (MeshFilter mf in filters)
            {
                if (!mf.gameObject.isStatic) continue;
                if (mf.gameObject == groupRoot) continue;
                if (mf.name.Contains("_Combined_")) continue;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;

                Mesh mesh = mf.sharedMesh;
                Material[] materials = mr.sharedMaterials;

                // Handle multi-submesh meshes correctly
                int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);

                for (int sub = 0; sub < subMeshCount; sub++)
                {
                    Material mat = materials[sub];
                    if (mat == null) continue;

                    (Mesh mesh, Material mat, int sub) key = (mesh, mat, sub);

                    if (!combines.TryGetValue(key, out List<(CombineInstance ci, MeshRenderer renderer)> list))
                    {
                        list = new List<(CombineInstance, MeshRenderer)>();
                        combines[key] = list;
                    }

                    CombineInstance ci = new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = sub,
                        transform = mf.transform.localToWorldMatrix
                    };

                    list.Add((ci, mr));
                    combinedRenderers.Add(mr);
                }
            }

            // Create combined meshes
            foreach (KeyValuePair<(Mesh mesh, Material mat, int subMesh), List<(CombineInstance ci, MeshRenderer renderer)>> pair in combines)
            {
                (Mesh mesh, Material mat, int subMesh) key = pair.Key;
                List<(CombineInstance ci, MeshRenderer renderer)> list = pair.Value;

                // Only combine if more than one instance
                if (list.Count <= 1)
                    continue;

                Mesh combinedMesh = new Mesh
                {
                    indexFormat = IndexFormat.UInt32
                };

                combinedMesh.CombineMeshes(list.Select(x => x.ci).ToArray(), true, true);
                combinedMesh.RecalculateBounds();
                combinedMesh.RecalculateNormals();

                GameObject combinedGO = new GameObject(
                    $"{groupRoot.name}_Combined_{key.mesh.name}_{key.mat.name}_sub{key.subMesh}"
                );

                combinedGO.transform.SetParent(groupRoot.transform, false);
                combinedGO.isStatic = true;

                MeshFilter mf = combinedGO.AddComponent<MeshFilter>();
                MeshRenderer mr = combinedGO.AddComponent<MeshRenderer>();

                mf.sharedMesh = combinedMesh;
                mr.sharedMaterial = key.mat;
            }

            // Disable ONLY the renderers that were actually combined
            foreach (MeshRenderer r in combinedRenderers)
            {
                if (r != null)
                    r.enabled = false;
            }
        }
    }
}
