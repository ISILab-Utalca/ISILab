using System.Collections.Generic;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public interface IDistanceEvaluator : IEvaluator
    {
        public enum PathfindingAlgorithm { Flood_Fill, JPS_Plus }

        public Dictionary<(int, int), int> DistancePool { get; set; }
    }
}

