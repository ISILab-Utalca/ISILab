using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class PopulationRuleGenerator : LBSGeneratorRule // FIX: Change to a better name
    {
        public PopulationRuleGenerator() : base() { }
        // For template construction
        public PopulationRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }

        public override bool CheckViability(LBSLayer layer)
        {
            return true; // TODO: Implement CheckViability method
        }

        public override object Clone()
        {
            return new PopulationRuleGenerator();
        }

        public override bool Equals(object obj)
        {
            PopulationRuleGenerator other = obj as PopulationRuleGenerator;

            if (other == null) return false;

            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            //Get references
            BundleTileMap data = layer.GetModule<BundleTileMap>();
            List<Bundle> bundles = LBSAssetsStorage.Instance.Get<Bundle>();
            Vector2 scale = settings.scale;

            //Create container objects
            GameObject parent = new GameObject("Types");

            GameObject parentEntity = new GameObject("Entity");
            GameObject parentObject = new GameObject("Object");
            GameObject parentInteractable = new GameObject("Interactable");
            GameObject parentTrigger = new GameObject("Trigger");
            GameObject parentProp = new GameObject("Prop");
            GameObject parentMisc = new GameObject("Misc");

            List<TileBundleGroup> groups = data.Groups;
            Dictionary<GameObject, Bundle.EElementFlag> objects = new Dictionary<GameObject, Bundle.EElementFlag>();

            foreach (TileBundleGroup group in groups)
            {
                //Get tile positions
                Vector2 centerposition = Vector2.zero;
                List<Vector2Int> positions = new List<Vector2Int>();
                foreach (LBSTile tile in group.TileGroup)
                {
                    // get interpolated center
                    positions!.Add(tile.Position);
                }

                int sumX = 0;
                int sumY = 0;

                foreach (Vector2Int pos in positions)
                {
                    sumX += pos.x;
                    sumY += pos.y;
                }
                centerposition = new Vector2(sumX / (float)positions.Count, sumY / (float)positions.Count);

                //Get bundle
                Bundle current = null;
                foreach (Bundle b in bundles)
                {
                    var id = b.name;

                    if (id.Equals(group.BundleData.BundleName))
                        current = b;
                }
                if (current == null) continue;

                //Get asset from bundle
                Asset pref = current.Assets[Random.Range(0, current.Assets.Count)];
                if (pref == null)
                {
                    Debug.LogError("Null reference in asset: " + current.Name);
                    continue;
                }

                //Instantiate prefab
                GameObject go;

#if UNITY_EDITOR
          
                go = PrefabUtility.InstantiatePrefab(pref.obj) as GameObject;
#else
                var go = GameObject.Instantiate(pref.obj);
#endif
                if (go == null && current.GetHasTagCharacteristic("TriggerArea")) go = new GameObject(current.Name);
                if (go == null && current.GetHasTagCharacteristic("TriggerUnlock")) go = new GameObject(current.Name);
                else if (go == null)
                {
                    Debug.LogError("Could not find prefab for: " + current.Name);             
                    continue;
                }

                if (current.GetHasTagCharacteristic("Player")) go.tag = "Player"; // TODO this is hardcoded - shouldnt be

                //Set rotation
                var r = Directions.Bidimencional.Edges.FindIndex(v => v == group.Rotation);
                //Debug.Log("rotation: " + r);
                go.transform.rotation = Quaternion.Euler(0, 90 * (r + 2), 0);

                // Set General position
                go.transform.position =
                    settings.position +
                    new Vector3(centerposition.x * scale.x, 0, centerposition.y * scale.y) +
                    -(new Vector3(scale.x, 0, scale.y) / 2f);

                //Micro population tool
                go.transform.position += current.GetMicroGenTool().MicroPosVector(go.transform, scale, r);

                //Add components
                LBSGeneratedPopulation generatedComponent = go.AddComponent<LBSGeneratedPopulation>();
                generatedComponent.Addons = group.Addons;
                generatedComponent.BundleRef = current;
                generatedComponent.LayerName = layer.Name;
                objects.Add(go, current.ElementFlag);
            }

            if (objects.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(parentEntity);
                UnityEngine.Object.DestroyImmediate(parentObject);
                UnityEngine.Object.DestroyImmediate(parentInteractable);
                UnityEngine.Object.DestroyImmediate(parentTrigger);
                UnityEngine.Object.DestroyImmediate(parentProp);
                UnityEngine.Object.DestroyImmediate(parentMisc);
                return new GeneratedGO(parent, 
                    new LBSLog("No population objects were created. Assign a valid bundle type", LogType.Error));
            }

            var x = objects.Keys.Average(o => o.transform.position.x);
            var y = objects.Keys.Min(o => o.transform.position.y);
            var z = objects.Keys.Average(o => o.transform.position.z);
            parent.transform.position = new Vector3(x, y, z);

            foreach (KeyValuePair<GameObject, Bundle.EElementFlag> obj in objects)
            {
                GameObject go = obj.Key;
                Bundle.EElementFlag flag = obj.Value;

                Transform parentTransform = GetParentForFlag(flag);
                go.transform.SetParent(parentTransform);

                // continue your existing logic
                go.AddComponent<DestroyNotifier>();
            }

            parentEntity.transform.SetParent(parent.transform);
            parentObject.transform.SetParent(parent.transform);
            parentInteractable.transform.SetParent(parent.transform);
            parentTrigger.transform.SetParent(parent.transform);
            parentProp.transform.SetParent(parent.transform);

            parentMisc.transform.SetParent(parent.transform);
            parent.transform.position += settings.position;

            return new GeneratedGO(parent, new LBSLog(0));

            Transform GetParentForFlag(Bundle.EElementFlag flag)
            {
                // reads by flag
                if ((flag & Bundle.EElementFlag.Character) != 0) return parentEntity.transform;
                if ((flag & Bundle.EElementFlag.Item) != 0) return parentObject.transform;
                if ((flag & Bundle.EElementFlag.Interactable) != 0) return parentInteractable.transform;
                if ((flag & Bundle.EElementFlag.Trigger) != 0) return parentTrigger.transform;
                if ((flag & Bundle.EElementFlag.Prop) != 0) return parentProp.transform;
                if ((flag & Bundle.EElementFlag.Misc) != 0) return parentMisc.transform;

                // in theory it shoudln enter here
                return parent.transform;
            }
        }
    }
}