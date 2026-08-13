using ISILab.LBS.Characteristics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/WFCRuleset")]
    public class WFCRuleset : ScriptableObject
    {
        [SerializeField]
        string rulesetName = "New WFC Ruleset";

        [SerializeField]
        List<LBSDirectionedChance.TileDirection> tileDirections = new();

        public string Name { get => rulesetName; set => rulesetName = name = value; }

        public List<LBSDirectionedChance.TileDirection> GetRules() => LBSDirectionedChance.DeepCopy(tileDirections);

        public void SetRules(List<LBSDirectionedChance.TileDirection> newRules) => tileDirections = LBSDirectionedChance.DeepCopy(newRules);
    }
}

