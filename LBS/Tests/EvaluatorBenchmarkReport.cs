using ISILab.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class EvaluatorBenchmarkReport : MAPElitesBaseBenchmark
    {
        const string level4Rooms = "04acda0b4a6f7ca4da575ba34b30d554";
        const string level20Rooms = "b93245dd9ffc3d84d9b6bb9e58d1d05e";

        #region Only Evaluate

        // These functions are responsible for measuring only the evaluation time of each evaluator and the fitness of the map, without considering the entire MAP-Elites execution process.

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_20_Rooms_Exploration()
        {
            IEvaluator evaluator = new DCExploration();
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
                SetUpMAPElitesTest(level20Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
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
        public void OnlyEvaluateMAPElites_4_Rooms_Exploration()
        {
            DCExploration evaluator = new DCExploration();
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
                SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_4_Rooms_ResourceSafety()
        {
            DCResourceSafety evaluator = new DCResourceSafety();
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
                SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_4_Rooms_SafeArea()
        {
            DCSafeArea evaluator = new DCSafeArea();
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
                SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                //evaluator.InitializeContext(levelData.ContextLayers, assistant.RawToolRect);
                //evaluator.InitializeDefault();
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        #endregion
    }
}
