using System.Collections.Generic;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    [System.Serializable]
    public enum PathfindingAlgorithm 
    {
        /// <summary>
        /// Preferable for laberynthin levels.
        /// </summary>
        Flood_Fill,
        /// <summary>
        /// Preferable for open areas with few obstacles.
        /// </summary>
        JPS_Plus 
    }

    public interface IDistanceEvaluator : IEvaluator
    {
        public Dictionary<(int, int), int> DistancePool { get; set; }
    }
}
