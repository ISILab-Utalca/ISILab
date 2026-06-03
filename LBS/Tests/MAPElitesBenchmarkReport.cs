using ISILab.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class MAPElitesBenchmarkReport : MAPElitesBaseBenchmark
    {

        const string level9Rooms = "f584add7bb7a37144a9ea0bca12db4ec";//"04acda0b4a6f7ca4da575ba34b30d554";
        const string level21Rooms = "917eb1d1b0892ba4092b71f862fd69d2";//"b93245dd9ffc3d84d9b6bb9e58d1d05e";


        #region MAP Elites - Dungeon Crawler evaluators

        // These functions are responsible for measuring time and fitness (fitness for each map) present in the entire MapElites execution process.

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_9_Rooms_Exploration()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
            .WarmupCount(0)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                //Selection.activeObject = null;
                DCExploration exploration = new();
                DCResourceSafety resourceSafety = new();
                DCSafeArea safeArea = new();
                exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, exploration, resourceSafety, safeArea);
            })
            .CleanUp(CleanUpMAPElitesTest)
            //.GC()
            .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_9_Rooms_ResourceSafety()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    DCExploration exploration = new();
                    DCResourceSafety resourceSafety = new();
                    DCSafeArea safeArea = new();
                    exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                    SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, resourceSafety, safeArea, exploration);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_9_Rooms_SafeArea()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    DCExploration exploration = new();
                    DCResourceSafety resourceSafety = new();
                    DCSafeArea safeArea = new();
                    exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                    SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, safeArea, exploration, resourceSafety);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_21_Rooms_Exploration()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    DCExploration exploration = new();
                    DCResourceSafety resourceSafety = new();
                    DCSafeArea safeArea = new();
                    exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, exploration, resourceSafety, safeArea);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_21_Rooms_ResourceSafety()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    DCExploration exploration = new();
                    DCResourceSafety resourceSafety = new();
                    DCSafeArea safeArea = new();
                    exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, resourceSafety, safeArea, exploration);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void MeasureMAPElites_21_Rooms_SafeArea()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    DCExploration exploration = new();
                    DCResourceSafety resourceSafety = new();
                    DCSafeArea safeArea = new();
                    exploration.searchType = PathfindingAlgorithm.JPS_Plus;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, safeArea, exploration, resourceSafety);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }
        #endregion


        #region MAP Elites - Custom evaluators

        // These functions are responsible for measuring time and fitness (fitness for each map) present in the entire MapElites execution process.

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_9_Rooms_Colonies()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time", SampleUnit.Microsecond);

            BundleTilemapChromosome chromosome = null;
            Colonies colonies = new();
            SingleRatio singleRatio = new();
            PairRatio pairRatio = new();

            Measure.Method(() =>
            {
                //(colonies as ITestingEvaluator).EvaluateWithInfo(chromosome, out EvaluationInfo info);

                //Measure.Custom(visitedNodesGroup, info.visitedNodes);
                //Measure.Custom(meanExecutionTime, info.Average() * 1000);

                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
            .WarmupCount(0)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                //Selection.activeObject = null;
                colonies.searchType = PathfindingAlgorithm.JPS_Plus;// Flood_Fill;
                colonies.searchHeuristic = PathfindingHeuristic.Octile;// Manhattan;
                SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, colonies, singleRatio, pairRatio);
                chromosome = GetChromosomeFromAssistant();
            })
            .CleanUp(CleanUpMAPElitesTest)
            //.GC()
            .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_9_Rooms_SingleRatio()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    Colonies colonies = new();
                    SingleRatio singleRatio = new();
                    PairRatio pairRatio = new();
                    colonies.searchType = PathfindingAlgorithm.JPS_Plus;
                    colonies.searchHeuristic = PathfindingHeuristic.Octile;
                    SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, singleRatio, pairRatio, colonies);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_9_Rooms_PairRatio()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    Colonies colonies = new();
                    SingleRatio singleRatio = new();
                    PairRatio pairRatio = new();
                    colonies.searchType = PathfindingAlgorithm.JPS_Plus;
                    colonies.searchHeuristic = PathfindingHeuristic.Octile;
                    SetUpMAPElitesTest(level9Rooms, dungeonPresetPath, pairRatio, colonies, singleRatio);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_21_Rooms_Colonies()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time", SampleUnit.Microsecond);

            BundleTilemapChromosome chromosome = null;
            Colonies colonies = new();
            SingleRatio singleRatio = new();
            PairRatio pairRatio = new();
            Measure.Method(() =>
            {
                //(colonies as ITestingEvaluator).EvaluateWithInfo(chromosome, out EvaluationInfo info);

                //Measure.Custom(visitedNodesGroup, info.visitedNodes);
                //Measure.Custom(meanExecutionTime, info.Average() * 1000);
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    colonies.searchType = PathfindingAlgorithm.JPS_Plus;// Flood_Fill;
                    colonies.searchHeuristic = PathfindingHeuristic.Octile;// Manhattan;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, colonies, singleRatio, pairRatio);
                    chromosome = GetChromosomeFromAssistant();
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_21_Rooms_SingleRatio()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    Colonies colonies = new();
                    SingleRatio singleRatio = new();
                    PairRatio pairRatio = new();
                    colonies.searchType = PathfindingAlgorithm.JPS_Plus;
                    colonies.searchHeuristic = PathfindingHeuristic.Octile;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, singleRatio, pairRatio, colonies);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureMAPElites_21_Rooms_PairRatio()
        {
            SampleGroup fitnessGroup = new SampleGroup("Generated Fitness", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                assistant.Execute(true);
                //var matrix = assistant.Samples;

                //if (matrix != null)
                //{
                //    foreach (var individual in matrix)
                //    {
                //        if (individual != null)
                //        {
                //            Measure.Custom(fitnessGroup, individual.Fitness);
                //        }
                //    }
                //}
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() =>
                {
                    Colonies colonies = new();
                    SingleRatio singleRatio = new();
                    PairRatio pairRatio = new();
                    colonies.searchType = PathfindingAlgorithm.JPS_Plus;
                    colonies.searchHeuristic = PathfindingHeuristic.Octile;
                    SetUpMAPElitesTest(level21Rooms, dungeonPresetPath, pairRatio, colonies, singleRatio);
                })
                .CleanUp(CleanUpMAPElitesTest)
                .Run();
        }
        #endregion

    }
}
