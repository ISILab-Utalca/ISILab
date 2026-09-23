using System.Collections.Generic;
using UnityEngine;
using static ISILab.LBS.Characteristics.LBSDirectionedChance;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    /// <summary>
    /// Object used by <see cref="Characteristics.LBSDirectionedChance"/> to save and load tiles and its neighbourhood probabilities.
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/WFCRuleset")]
    public class WFCRuleset : ScriptableObject
    {
        /// <summary>
        /// Name of this ruleset.
        /// </summary>
        [SerializeField]
        string rulesetName = "New WFC Ruleset";

        /// <summary>
        /// Tiles with neighbourhood probabilities.
        /// </summary>
        [SerializeField]
        List<TileDirection> tileDirections = new();

        /// <summary>
        /// Name of this ruleset.
        /// </summary>
        public string Name { get => rulesetName; set => rulesetName = name = value; }

        /// <summary>
        /// Makes a copy of the stored tiles and its neighbourhood rules.
        /// </summary>
        /// <returns>A deep copy of the stored tiles.</returns>
        public List<TileDirection> GetRules() => DeepCopy(tileDirections);

        /// <summary>
        /// Replace the current neighbourhood rules with new ones.
        /// </summary>
        /// <param name="newRules">The new tiles to store and its neighbourhood rules.</param>
        public void SetRules(List<TileDirection> newRules) => tileDirections = DeepCopy(newRules);
    }
}

