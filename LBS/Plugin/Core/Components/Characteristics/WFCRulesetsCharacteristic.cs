using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic that stores rulesets for WFC. Used by Exterior Main Bundles.
    /// </summary>
    [System.Serializable]
    public class WFCRulesetsCharacteristic : LBSCharacteristic
    {
        [SerializeField]
        private List<WFCRuleset> rulesets = new List<WFCRuleset>();

        /// <summary>
        /// List of non-null rulesets stored.
        /// </summary>
        [JsonIgnore]
        public List<WFCRuleset> Rulesets
        {
            get
            {
                for(int i = 0; i < rulesets.Count; i++)
                {
                    if (rulesets[i] == null)
                    {
                        rulesets.RemoveAt(i);
                        i--;
                    }
                }
                return rulesets;
            }
        }

        public override void OnEnable()
        {
            Owner.OnRemoveCharacteristic -= ConfirmRemove;
            Owner.OnRemoveCharacteristic += ConfirmRemove;
        }

        /// <summary>
        /// Event method that asks confirmation when removing this characteristic from its owner bundle.
        /// </summary>
        /// <param name="c">This characteristic.</param>
        private void ConfirmRemove(LBSCharacteristic c)
        {
            if (c is null || !c.Equals(this)) return;

            Owner.OnRemoveCharacteristic -= ConfirmRemove;

            if(EditorUtility.DisplayDialog("Delete Rulesets?", "By removing this characteristic, all associated WFC rulesets will be lost.", "Continue", "Cancel"))
            {
                AssetDatabase.DeleteAssets(Rulesets.Select(r => AssetDatabase.GetAssetPath(r)).ToArray(), new List<string>());
            }
            else
            {
                // Restores the characteristic that was going to be removed.
                var clone = c.Clone() as WFCRulesetsCharacteristic;
                c = null;
                Owner.AddCharacteristic(clone);
                Selection.activeObject = null;
                EditorApplication.delayCall += () => Selection.activeObject = Owner;
            }
        }

        public override object Clone()
        {
            var clone = new WFCRulesetsCharacteristic();
            clone.rulesets = new List<WFCRuleset>(Rulesets);
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is not WFCRulesetsCharacteristic other) return false;

            return other.Rulesets.Equals(Rulesets);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

