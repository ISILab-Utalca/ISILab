using ISILab.AI.Optimization;
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
        JPS_Plus,
        A_Star
    }

    public struct EvaluationInfo
    {
        public int visitedNodes;

        public EvaluationInfo(int visitedNodes)
        {
            this.visitedNodes = visitedNodes;
        }
    }

    public interface IDistanceEvaluator : IEvaluator
    {
        public Dictionary<(int, int), int> DistancePool { get; set; }
    }

    public interface ITestingEvaluator : IEvaluator
    {
        public EvaluationInfo EvaluationInfo { get; set; }

        public float EvaluateWithInfo(IOptimizable evaluable, out EvaluationInfo evalInfo);
    }
}
