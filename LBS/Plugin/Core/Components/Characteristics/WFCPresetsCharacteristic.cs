using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    [System.Serializable]
    //[LBSCharacteristic("WFC Presets", "")]
    public class WFCPresetsCharacteristic : LBSCharacteristic
    {
        [SerializeField]
        private List<WFCPreset> presets = new List<WFCPreset>();

        [JsonIgnore]
        public List<WFCPreset> Presets
        {
            get
            {
                for(int i = 0; i < presets.Count; i++)
                {
                    if (presets[i] == null)
                    {
                        presets.RemoveAt(i);
                        i--;
                    }
                }
                return presets;
            }
        }

        public override void OnEnable()
        {
            Owner.OnRemoveCharacteristic -= ConfirmRemove;
            Owner.OnRemoveCharacteristic += ConfirmRemove;
        }

        private void ConfirmRemove(LBSCharacteristic c)
        {
            if (c is null || !c.Equals(this)) return;

            Owner.OnRemoveCharacteristic -= ConfirmRemove;

            if (EditorUtility.DisplayDialog("Delete Presets?", "By removing this characteristic, all associated WFC presets will be lost.", "Continue", "Cancel"))
            {
                AssetDatabase.DeleteAssets(Presets.Select(p => AssetDatabase.GetAssetPath(p)).ToArray(), new List<string>());

            }
            else
            {
                var clone = c.Clone() as WFCPresetsCharacteristic;
                c = null;
                Owner.AddCharacteristic(clone);
                Selection.activeObject = null;
                EditorApplication.delayCall += () => Selection.activeObject = Owner;
            }
        }

        public override object Clone()
        {
            var clone = new WFCPresetsCharacteristic();
            clone.presets = new List<WFCPreset>(Presets);
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is not WFCPresetsCharacteristic other) return false;

            return other.Presets.Equals(Presets);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}