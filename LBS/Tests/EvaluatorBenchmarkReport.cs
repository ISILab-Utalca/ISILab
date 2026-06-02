using ISILab.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class EvaluatorBenchmarkReport : MAPElitesBaseBenchmark
    {
        const string level9Rooms = "f584add7bb7a37144a9ea0bca12db4ec";
        const string level21Rooms = "917eb1d1b0892ba4092b71f862fd69d2";

        #region Only Evaluate

        // These functions are responsible for measuring only the evaluation time of each evaluator and the fitness of the map, without considering the entire MAP-Elites execution process.

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_21_Rooms_Exploration()
        {
            IEvaluator evaluator = new DCExploration();
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visited = new SampleGroup("Visited nodes",  SampleUnit.Undefined);

            Measure.Method(() =>
            {
                double fitness = (evaluator as ITestingEvaluator).EvaluateWithInfo(chromosome, out EvaluationInfo info);
                Measure.Custom(fitnessGroup, fitness);
                Measure.Custom(visited, info.visitedNodes);
            })
            .WarmupCount(5)
            .MeasurementCount(10)
            .IterationsPerMeasurement(10)
            .SetUp(() =>
            {
                SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                evaluator = preset.Optimizer.Evaluator;
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_9_Rooms_Exploration()
        {
            IEvaluator evaluator = new DCExploration();
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visited = new SampleGroup("Visited nodes",  SampleUnit.Undefined);

            Measure.Method(() =>
            {
                double fitness = (evaluator as ITestingEvaluator).EvaluateWithInfo(chromosome, out EvaluationInfo info);
                Measure.Custom(fitnessGroup, fitness);
                Measure.Custom(visited, info.visitedNodes);
            })
            .WarmupCount(5)
            .MeasurementCount(10)
            .IterationsPerMeasurement(10)
            .SetUp(() =>
            {
                SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                evaluator = preset.Optimizer.Evaluator;
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_9_Rooms_ResourceSafety()
        {
            IEvaluator evaluator = new DCResourceSafety();
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                double fitness = evaluator.Evaluate(chromosome);
                Measure.Custom(fitnessGroup, fitness);
            })
            .WarmupCount(5)
            .MeasurementCount(10)
            .IterationsPerMeasurement(10)
            .SetUp(() =>
            {
                SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                evaluator = preset.Optimizer.Evaluator;
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_9_Rooms_SafeArea()
        {
            IEvaluator evaluator = new DCSafeArea();
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                double fitness = evaluator.Evaluate(chromosome);
                Measure.Custom(fitnessGroup, fitness);
            })
            .WarmupCount(5)
            .MeasurementCount(10)
            .IterationsPerMeasurement(10)
            .SetUp(() =>
            {
                SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                evaluator = preset.Optimizer.Evaluator;
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        #endregion
    }
}
