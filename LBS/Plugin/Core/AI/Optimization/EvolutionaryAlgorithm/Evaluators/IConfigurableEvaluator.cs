using ISILab.LBS.AI.Categorization;
using System.Collections.Generic;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public interface IConfigurableEvaluator : IEvaluator
    {
        public void ReadConfiguration();
        public List<EvaluatorConfiguration.EvaluatorConfigurationField> GetEvaluatorFields();
    }
}
