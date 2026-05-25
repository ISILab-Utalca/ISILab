using ISILab.AI.Optimization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    [System.Serializable]
    public enum PathfindingAlgorithm
    {
        Flood_Fill,
        JPS_Plus,
        A_Star
    }

    [System.Serializable]
    public enum PathfindingHeuristic
    {
        /// <summary>
        /// No diagonals
        /// </summary>
        Manhattan,
        /// <summary>
        /// Diagonals = x sqrt(2)
        /// </summary>
        Octile,
        /// <summary>
        /// Diagonals = x
        /// </summary>
        Chebyshev
    }

    public struct EvaluationInfo
    {
        public int visitedNodes;
        public Stopwatch sw;
        public List<double> measures;

        public EvaluationInfo(int visitedNodes)
        {
            this.visitedNodes = visitedNodes;
            sw = new Stopwatch();
            measures = new List<double>();
        }

        public void StartMeasure()
        {
            sw.Reset();
            sw.Start();
        }

        public void StopMeasure()
        {
            sw.Stop();
            double measure = sw.Elapsed.TotalMilliseconds;//(double)sw.ElapsedTicks / Stopwatch.Frequency / 1000.0;
            measures.Add(measure);
        }

        public double Average() => measures.Average();
        public int MeasureCount() => measures.Count();
    }

    public interface IDistanceEvaluator : IEvaluator
    {
        public Dictionary<(int, int), float> DistancePool { get; set; }
    }

    public interface ITestingEvaluator : IEvaluator
    {
        public EvaluationInfo EvaluationInfo { get; set; }

        public float EvaluateWithInfo(IOptimizable evaluable, out EvaluationInfo evalInfo);
    }
}
