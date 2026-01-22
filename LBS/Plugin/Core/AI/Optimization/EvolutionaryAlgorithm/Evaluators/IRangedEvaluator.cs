using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    /// <summary>
    /// Represents a ranged evaluator.
    /// </summary>
    public interface IRangedEvaluator : IEvaluator
    {
        public float MaxValue { get;}
        public float MinValue { get;}
    }
}

