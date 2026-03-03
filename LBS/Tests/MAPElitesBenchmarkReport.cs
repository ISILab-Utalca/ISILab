using ISILab.AI.Categorization;
using ISILab.Commons.JsonNet;   
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;


namespace ISILab.LBS.Tests
{

    [TestFixture]
    public class MAPElitesBenchmarkReport : MAPElitesBaseBenchmark
    {

        const string level4Rooms = "04acda0b4a6f7ca4da575ba34b30d554";
        const string level20Rooms = "b93245dd9ffc3d84d9b6bb9e58d1d05e";
        

        #region Full MAP-Elites Execution

        // These functions are responsible for measuring time and fitness (fitness for each map) present in the entire MapElites execution process.

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_4_Rooms_Exploration()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                var matrix = assistant.Samples;

                if (matrix != null)
                {
                    foreach (var individual in matrix)
                    {
                        if (individual != null)
                        {
                            Measure.Custom(fitnessGroup, individual.Fitness);
                        }
                    }
                }
            })
            .WarmupCount(0)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                Selection.activeObject = null;
                SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
            })
            .CleanUp(CleanUpMAPElitesTest)
            .GC()
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_4_Rooms_ResourceSafety()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                var matrix = assistant.Samples;

                if (matrix != null)
                {
                    foreach (var individual in matrix)
                    {
                        if (individual != null)
                        {
                            Measure.Custom(fitnessGroup, individual.Fitness);
                        }
                    }
                }
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCResourceSafety(), new DCSafeArea(), new DCExploration()))
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_4_Rooms_SafeArea()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                var matrix = assistant.Samples;

                if (matrix != null)
                {
                    foreach (var individual in matrix)
                    {
                        if (individual != null)
                        {
                            Measure.Custom(fitnessGroup, individual.Fitness);
                        }
                    }
                }
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetUpMAPElitesTest(level4Rooms, dungeonPresetPath, new DCSafeArea(), new DCExploration(), new DCResourceSafety()))
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_20_Rooms_Exploration()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                var matrix = assistant.Samples;

                if (matrix != null)
                {
                    foreach (var individual in matrix)
                    {
                        if (individual != null)
                        {
                            Measure.Custom(fitnessGroup, individual.Fitness);
                        }
                    }
                }
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetUpMAPElitesTest(level20Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea()))
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }
        #endregion

        

        
    }
}
