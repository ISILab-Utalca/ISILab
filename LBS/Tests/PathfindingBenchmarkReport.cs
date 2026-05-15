using ISILab.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Unity.PerformanceTesting;
using UnityEngine;

namespace ISILab.LBS.Tests.Pathfinding
{
    [TestFixture]
    public class PathfindingBenchmarkReport : MAPElitesBaseBenchmark
    {
        const string pathfindRoom = "69adc1e45b1df6645a15c4293b4f58ad";

        protected const PathfindingAlgorithm FF = PathfindingAlgorithm.Flood_Fill;
        protected const PathfindingAlgorithm JPS = PathfindingAlgorithm.JPS_Plus;
        protected const PathfindingAlgorithm AStar = PathfindingAlgorithm.A_Star;
        
        protected const PathfindingHeuristic Manhattan = PathfindingHeuristic.Manhattan;
        protected const PathfindingHeuristic Octile = PathfindingHeuristic.Octile;
        protected const PathfindingHeuristic Chebyshev = PathfindingHeuristic.Chebyshev;

        const int WARM_UP_COUNT = 5;
        const int MEASUREMENT_COUNT = 20;

        static readonly string[] levels = new string[]
        {
            level1,
            level2,
            level3,
            level4,
            level5,
            level6,
            level7,
            level8,
            level9,
            level10,
            level11,
            level12
        };

        const string level1 =  "8fcb3f8620553e34b8968069d24dc0a8";
        const string level2 =  "489960a220fb386498c0d94c22a979c0";
        const string level3 =  "99aea4d41c519c34389bb29a54bcb41f";
        const string level4 =  "46fded00b5f05974bb4c2f200ed65ce0";
        const string level5 =  "f655e86b2ca92ff4b824400014562fde";
        const string level6 =  "fe7e011a4f10daa4ebb6ff3701516bfd";
        const string level7 =  "9db451314b628c84ca6b366fdb87f81c";
        const string level8 =  "a652c5df35f5e9945951556497349b62";
        const string level9 =  "4d30b728085df3843a89b4c4def7e354";
        const string level10 = "8701497f3bda71342893a133381b8cb0";
        const string level11 = "9e52fa05cc04b5a4cbb0752588af5a27";
        const string level12 = "a49e34122d48f53438e7dd8051abd8e5";

        const string ultraLabyrinth =
            //"995a3afcb9fc6e64c90596fd7328d761" // Micro (100) // Sobreescribi Half (90) sin querer xdn't pero se puede rehacer a partir del 150
            "a2389a54af9bb5b49899b56d1551992a" // Micro (77)
            //"be4a28232962e824fa38cea4a871fb04" // Half (150)
            //"50a1ad21d6d768e4eac31c15c86d695a" // Half (200)
            //"f061d26c69f5ae74db19d2671f0776b9" // 150
            //"9418ed86a5efcf34983d879ebf43f084" // 200
            ;
        static readonly string[] cheese = new string[]
        {
            "0553b266d7302e6449ebb9b2961e8d3e",
            "f3d011ec0655b1e48a1fbfa5c9e3dfa5"
        };

        protected void ColoniesPathfind(int level, PathfindingAlgorithm searchType, PathfindingHeuristic heuristic)
        {
            var evaluator = Activator.CreateInstance(typeof(Colonies)) as ITestingEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time", SampleUnit.Microsecond);

            int mapSize = Mathf.CeilToInt(level / 3f);

            int c = 0;
            Measure.Method(() =>
            {
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                c++;
                if (c <= WARM_UP_COUNT) return;
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
                Measure.Custom(meanExecutionTime, info.Average() * 1000);
            })
            .WarmupCount(WARM_UP_COUNT)
            .MeasurementCount(MEASUREMENT_COUNT)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                GetLevel(levels[level-1]);
                IRangedEvaluator eval = evaluator as IRangedEvaluator;
                SetUpMAPElitesTest(levels[level-1], dungeonPresetPath, eval, eval, eval, "", GetArea(mapSize));
                chromosome = GetChromosomeFromAssistant();
                (eval as Colonies).searchType = searchType;
                (eval as Colonies).searchHeuristic = heuristic;
            })
            .CleanUp(() =>
            {
                (evaluator as Colonies).searchHeuristic = PathfindingHeuristic.Chebyshev;
                CleanUpMAPElitesTest();
            })
            .Run();
        }
        protected void UltraLabyrinthPathfind(PathfindingAlgorithm searchType, PathfindingHeuristic heuristic)
        {
            var evaluator = Activator.CreateInstance(typeof(Colonies)) as ITestingEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time", SampleUnit.Microsecond);
            SampleGroup measures = new SampleGroup("Measures", SampleUnit.Undefined);

            //int mapSize = Mathf.CeilToInt(level / 3f);

            int c = 0;
            Measure.Method(() =>
            {
                UnityEngine.Assertions.Assert.IsTrue((evaluator as Colonies).searchType == searchType);
                UnityEngine.Assertions.Assert.IsTrue((evaluator as Colonies).searchHeuristic == heuristic);
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                c++;
                if (c <= WARM_UP_COUNT) return;
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
                Measure.Custom(meanExecutionTime, info.Average() * 1000);
                Measure.Custom(measures, info.MeasureCount());
            })
            .WarmupCount(WARM_UP_COUNT)
            .MeasurementCount(5)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                GetLevel(ultraLabyrinth);
                IRangedEvaluator eval = evaluator as IRangedEvaluator;
                SetUpMAPElitesTest(ultraLabyrinth, dungeonPresetPath, eval, eval, eval, "", GetArea(4));
                chromosome = GetChromosomeFromAssistant();
                (eval as Colonies).searchType = searchType;
                (eval as Colonies).searchHeuristic = heuristic;
            })
            .CleanUp(() =>
            {
                (evaluator as Colonies).searchHeuristic = PathfindingHeuristic.Chebyshev;
                CleanUpMAPElitesTest();
            })
            .Run();
        }

        protected void CheesePathfind(int ind, PathfindingAlgorithm searchType, PathfindingHeuristic heuristic)
        {
            var evaluator = Activator.CreateInstance(typeof(Colonies)) as ITestingEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time", SampleUnit.Microsecond);
            SampleGroup measures = new SampleGroup("Measures", SampleUnit.Undefined);

            //int mapSize = Mathf.CeilToInt(level / 3f);
            Vector2Int[] sizes = new Vector2Int[]
            {
                new Vector2Int(11, 11),
                new Vector2Int(25, 23)
            };

            int c = 0;
            Measure.Method(() =>
            {
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                c++;
                if (c <= WARM_UP_COUNT) return;
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
                Measure.Custom(meanExecutionTime, info.Average() * 1000);
                Measure.Custom(measures, info.MeasureCount());
            })
            .WarmupCount(WARM_UP_COUNT)
            .MeasurementCount(5)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                GetLevel(cheese[ind]);
                IRangedEvaluator eval = evaluator as IRangedEvaluator;
                SetUpMAPElitesTest(cheese[ind], dungeonPresetPath, eval, eval, eval, "", new Rect(Vector2.zero, sizes[ind]));
                chromosome = GetChromosomeFromAssistant();
                (eval as Colonies).searchType = searchType;
                (eval as Colonies).searchHeuristic = heuristic;
            })
            .CleanUp(() =>
            {
                (evaluator as Colonies).searchHeuristic = PathfindingHeuristic.Chebyshev;
                CleanUpMAPElitesTest();
            })
            .Run();
        }

        private void Pathfind(Type type, int mapSize, int enemyQuantity, int wallQuantity, PathfindingAlgorithm searchType)
        {
            var evaluator = Activator.CreateInstance(type) as ITestingEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time");

            List<LBSLayer> oldContext = new();

            int c = 0;
            const int warmUp = 5;
            Measure.Method(() =>
            {
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                c++;
                if (c > warmUp) return;
                //Measure.Custom(fitnessGroup, fitness);
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
                double avg = info.Average();
                Measure.Custom(meanExecutionTime, avg);
            })
            .WarmupCount(warmUp)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                GetLevel(pathfindRoom);

                while(levelData.ContextLayers.Count > 0)
                {
                    oldContext.Add(levelData.ContextLayers[0]);
                    levelData.RemoveLayerFromContext(levelData.ContextLayers[0]);
                }

                //if (enemyQuantity >= 1) levelData.AddLayerToContext(levelData.Layers.Find(l => l.Name.Equals("Population 1")));
                //if (enemyQuantity >= 2) levelData.AddLayerToContext(levelData.Layers.Find(l => l.Name.Equals("Population 2")));
                //if (enemyQuantity >= 3) levelData.AddLayerToContext(levelData.Layers.Find(l => l.Name.Equals("Population 3")));
                //LBSLayer populationLayer = levelData.Layers.Find(l => l.Name.Equals("Population " + enemyQuantity));
                //if(populationLayer is not null) levelData.AddLayerToContext(populationLayer);
                LBSLayer wallLayer = levelData.Layers.Find(l => l.Name.Equals("Walls " + wallQuantity));
                if(wallLayer is not null) levelData.AddLayerToContext(wallLayer);

                SetUpMAPElitesTest(pathfindRoom, dungeonPresetPath, evaluator as IRangedEvaluator, new DCResourceSafety(), new DCSafeArea(), "Population " + enemyQuantity, GetArea(mapSize));
                chromosome = GetChromosomeFromAssistant();
                if(evaluator is Colonies colonies)
                    colonies.searchType = searchType;
            })
            .CleanUp(() =>
            {
                while (levelData.ContextLayers.Count > 0)
                {
                    levelData.RemoveLayerFromContext(levelData.ContextLayers[0]);
                }
                for(int i = 0; i < oldContext.Count; i++)
                {
                    levelData.AddLayerToContext(oldContext[i]);
                }
                CleanUpMAPElitesTest();
            })
            .Run();
        }

        string pathfindExample1 =//"dd3a891e0aa36dd4c9a3df0826e5a6cb"
            //"835410d40128b35468bbd7f01deccbb3" // (small 1)
            //"c582f8c0beac2a64f817ce3e1ea3c000" // (medium 1)
            //"340b5a38ad6dc6340a509604941ea5f3" // (big 1)

            "bbc52eb299a0c5542b700c6332e9f978" // (medium 2)
            //"9d647ab227a18bb49b015b055efe51de" // (big 2)
            ;
        public void PathfindExample1(PathfindingAlgorithm searchType)
        {
            var evaluator = Activator.CreateInstance(typeof(Colonies)) as ITestingEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);
            SampleGroup meanExecutionTime = new SampleGroup("Mean Execution Time");

            List<LBSLayer> oldContext = new();

            int c = 0;
            const int warmUp = 5;
            Measure.Method(() =>
            {
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                c++;
                if (c <= warmUp) return;
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
                double avg = info.Average();
                Measure.Custom(meanExecutionTime, avg);
            })
            .WarmupCount(warmUp)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .SetUp(() =>
            {
                UnityEngine.Assertions.Assert.IsTrue(GetLevel(pathfindExample1));

                SetUpMAPElitesTest(pathfindExample1, dungeonPresetPath, evaluator as IRangedEvaluator, new DCResourceSafety(), new DCSafeArea()/*, "New Layer 1", new Rect(new Vector2(-21, 3), new Vector2(19, 40))*/);
                chromosome = GetChromosomeFromAssistant();
                if (evaluator is Colonies colonies)
                    colonies.searchType = searchType;
            })
            .CleanUp(() =>
            {
                CleanUpMAPElitesTest();
            })
            .Run();
        }
        //[Test, Performance, Timeout(timeout)] public void PathfindExample1_FloodFill () => PathfindExample1(FF);
        //[Test, Performance, Timeout(timeout)] public void PathfindExample1_JPSPlus () => PathfindExample1(JPS);
        //[Test, Performance, Timeout(timeout)] public void PathfindExample1_AStar() => PathfindExample1(AStar);

        private Rect GetArea(int value)
        {
            Vector2 size = Vector2.zero;
            switch(value)
            {
                case 1: size = new Vector2(15, 10); break;
                case 2: size = new Vector2(30, 20); break;
                case 3: size = new Vector2(40, 40); break;
                case 4: size = new Vector2(75, 75); break;
                default: size = default; break;
            }

            return new Rect(Vector2.zero, size);
        }

        [Test, Performance, Timeout(timeout)] public void UltraLabyrinth_Chebyshev_FloodFill() => UltraLabyrinthPathfind(FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void UltraLabyrinth_Chebyshev_JPS() => UltraLabyrinthPathfind(JPS, Chebyshev); 
        [Test, Performance, Timeout(timeout)] public void Cheese1_Chebyshev_FloodFill() => CheesePathfind(0, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Cheese1_Chebyshev_JPS() => CheesePathfind(0, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Cheese2_Chebyshev_FloodFill() => CheesePathfind(1, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Cheese2_Chebyshev_JPS() => CheesePathfind(1, JPS, Chebyshev);

        #region OLD TESTS

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_FewWalls        () => Pathfind(typeof(Colonies), 1, 1, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_FewWalls       () => Pathfind(typeof(Colonies), 2, 1, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_FewWalls          () => Pathfind(typeof(Colonies), 3, 1, 1, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_FewWalls     () => Pathfind(typeof(Colonies), 1, 2, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_FewWalls    () => Pathfind(typeof(Colonies), 2, 2, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_FewWalls       () => Pathfind(typeof(Colonies), 3, 2, 1, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_FewWalls       () => Pathfind(typeof(Colonies), 1, 3, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_FewWalls      () => Pathfind(typeof(Colonies), 2, 3, 1, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_FewWalls         () => Pathfind(typeof(Colonies), 3, 3, 1, FF);


        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_MediumWalls     () => Pathfind(typeof(Colonies), 1, 1, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 2, 1, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_MediumWalls       () => Pathfind(typeof(Colonies), 3, 1, 2, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_MediumWalls  () => Pathfind(typeof(Colonies), 1, 2, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_MediumWalls () => Pathfind(typeof(Colonies), 2, 2, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 3, 2, 2, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 1, 3, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_MediumWalls   () => Pathfind(typeof(Colonies), 2, 3, 2, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 3, 3, 2, FF);


        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 1, 1, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 2, 1, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_ManyWalls         () => Pathfind(typeof(Colonies), 3, 1, 3, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_ManyWalls    () => Pathfind(typeof(Colonies), 1, 2, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_ManyWalls   () => Pathfind(typeof(Colonies), 2, 2, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 3, 2, 3, FF);

        //[Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 1, 3, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 2, 3, 3, FF);
        //[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 3, 3, 3, FF);

        ////[Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_TooManyEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 3, 4, 3, FF);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_FewWalls          () => Pathfind(typeof(Colonies), 1, 1, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_FewWalls         () => Pathfind(typeof(Colonies), 2, 1, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_FewWalls            () => Pathfind(typeof(Colonies), 3, 1, 1, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_FewWalls       () => Pathfind(typeof(Colonies), 1, 2, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_FewWalls      () => Pathfind(typeof(Colonies), 2, 2, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_FewWalls         () => Pathfind(typeof(Colonies), 3, 2, 1, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_FewWalls         () => Pathfind(typeof(Colonies), 1, 3, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_FewWalls        () => Pathfind(typeof(Colonies), 2, 3, 1, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_FewWalls           () => Pathfind(typeof(Colonies), 3, 3, 1, JPS);


        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_MediumWalls       () => Pathfind(typeof(Colonies), 1, 1, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 2, 1, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_MediumWalls         () => Pathfind(typeof(Colonies), 3, 1, 2, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 1, 2, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_MediumWalls   () => Pathfind(typeof(Colonies), 2, 2, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 3, 2, 2, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 1, 3, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_MediumWalls     () => Pathfind(typeof(Colonies), 2, 3, 2, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_MediumWalls        () => Pathfind(typeof(Colonies), 3, 3, 2, JPS);


        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_ManyWalls         () => Pathfind(typeof(Colonies), 1, 1, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 2, 1, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_ManyWalls           () => Pathfind(typeof(Colonies), 3, 1, 3, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 1, 2, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 2, 2, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 3, 2, 3, JPS);

        //[Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 1, 3, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 2, 3, 3, JPS);
        //[Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_ManyWalls          () => Pathfind(typeof(Colonies), 3, 3, 3, JPS);

        ////[Test, Performance, Timeout(timeout)] public void JPS_BigMap_TooManyEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 3, 4, 3, JPS);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_FewEnemies_FewWalls        () => Pathfind(typeof(Colonies), 1, 1, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_FewEnemies_FewWalls       () => Pathfind(typeof(Colonies), 2, 1, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_FewEnemies_FewWalls          () => Pathfind(typeof(Colonies), 3, 1, 1, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_MediumEnemies_FewWalls     () => Pathfind(typeof(Colonies), 1, 2, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_MediumEnemies_FewWalls    () => Pathfind(typeof(Colonies), 2, 2, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_MediumEnemies_FewWalls       () => Pathfind(typeof(Colonies), 3, 2, 1, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_ManyEnemies_FewWalls       () => Pathfind(typeof(Colonies), 1, 3, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_ManyEnemies_FewWalls      () => Pathfind(typeof(Colonies), 2, 3, 1, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_ManyEnemies_FewWalls         () => Pathfind(typeof(Colonies), 3, 3, 1, AStar);


        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_FewEnemies_MediumWalls     () => Pathfind(typeof(Colonies), 1, 1, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_FewEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 2, 1, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_FewEnemies_MediumWalls       () => Pathfind(typeof(Colonies), 3, 1, 2, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_MediumEnemies_MediumWalls  () => Pathfind(typeof(Colonies), 1, 2, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_MediumEnemies_MediumWalls () => Pathfind(typeof(Colonies), 2, 2, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_MediumEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 3, 2, 2, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_ManyEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 1, 3, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_ManyEnemies_MediumWalls   () => Pathfind(typeof(Colonies), 2, 3, 2, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_ManyEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 3, 3, 2, AStar);


        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_FewEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 1, 1, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_FewEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 2, 1, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_FewEnemies_ManyWalls         () => Pathfind(typeof(Colonies), 3, 1, 3, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_MediumEnemies_ManyWalls    () => Pathfind(typeof(Colonies), 1, 2, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_MediumEnemies_ManyWalls   () => Pathfind(typeof(Colonies), 2, 2, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_MediumEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 3, 2, 3, AStar);

        //[Test, Performance, Timeout(timeout)] public void AStar_SmallMap_ManyEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 1, 3, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_MediumMap_ManyEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 2, 3, 3, AStar);
        //[Test, Performance, Timeout(timeout)] public void AStar_BigMap_ManyEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 3, 3, 3, AStar);

        #endregion
    }

    [TestFixture]
    public class ManhattanFloodFill : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Manhattan_FloodFill() => ColoniesPathfind(1, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_B_Manhattan_FloodFill() => ColoniesPathfind(2, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_C_Manhattan_FloodFill() => ColoniesPathfind(3, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_D_Manhattan_FloodFill() => ColoniesPathfind(4, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_E_Manhattan_FloodFill() => ColoniesPathfind(5, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_F_Manhattan_FloodFill() => ColoniesPathfind(6, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_G_Manhattan_FloodFill() => ColoniesPathfind(7, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_H_Manhattan_FloodFill() => ColoniesPathfind(8, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_I_Manhattan_FloodFill() => ColoniesPathfind(9, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_J_Manhattan_FloodFill() => ColoniesPathfind(10, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_K_Manhattan_FloodFill() => ColoniesPathfind(11, FF, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_L_Manhattan_FloodFill() => ColoniesPathfind(12, FF, Manhattan);
    }

    [TestFixture]
    public class ManhattanAStar : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Manhattan_AStar() => ColoniesPathfind(1, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_B_Manhattan_AStar() => ColoniesPathfind(2, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_C_Manhattan_AStar() => ColoniesPathfind(3, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_D_Manhattan_AStar() => ColoniesPathfind(4, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_E_Manhattan_AStar() => ColoniesPathfind(5, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_F_Manhattan_AStar() => ColoniesPathfind(6, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_G_Manhattan_AStar() => ColoniesPathfind(7, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_H_Manhattan_AStar() => ColoniesPathfind(8, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_I_Manhattan_AStar() => ColoniesPathfind(9, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_J_Manhattan_AStar() => ColoniesPathfind(10, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_K_Manhattan_AStar() => ColoniesPathfind(11, AStar, Manhattan);
        [Test, Performance, Timeout(timeout)] public void Level_L_Manhattan_AStar() => ColoniesPathfind(12, AStar, Manhattan);
    }

    [TestFixture]
    public class OctileJPS : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Octile_JPS() => ColoniesPathfind(1, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_B_Octile_JPS() => ColoniesPathfind(2, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_C_Octile_JPS() => ColoniesPathfind(3, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_D_Octile_JPS() => ColoniesPathfind(4, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_E_Octile_JPS() => ColoniesPathfind(5, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_F_Octile_JPS() => ColoniesPathfind(6, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_G_Octile_JPS() => ColoniesPathfind(7, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_H_Octile_JPS() => ColoniesPathfind(8, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_I_Octile_JPS() => ColoniesPathfind(9, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_J_Octile_JPS() => ColoniesPathfind(10, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_K_Octile_JPS() => ColoniesPathfind(11, JPS, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_L_Octile_JPS() => ColoniesPathfind(12, JPS, Octile);
    }

    
    [TestFixture]
    public class OctileAStar : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Octile_AStar() => ColoniesPathfind(1, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_B_Octile_AStar() => ColoniesPathfind(2, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_C_Octile_AStar() => ColoniesPathfind(3, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_D_Octile_AStar() => ColoniesPathfind(4, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_E_Octile_AStar() => ColoniesPathfind(5, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_F_Octile_AStar() => ColoniesPathfind(6, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_G_Octile_AStar() => ColoniesPathfind(7, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_H_Octile_AStar() => ColoniesPathfind(8, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_I_Octile_AStar() => ColoniesPathfind(9, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_J_Octile_AStar() => ColoniesPathfind(10, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_K_Octile_AStar() => ColoniesPathfind(11, AStar, Octile);
        [Test, Performance, Timeout(timeout)] public void Level_L_Octile_AStar() => ColoniesPathfind(12, AStar, Octile);
    }

    [TestFixture]
    public class ChebyshevFloodFill : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Chebyshev_FloodFill() => ColoniesPathfind(1, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_B_Chebyshev_FloodFill() => ColoniesPathfind(2, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_C_Chebyshev_FloodFill() => ColoniesPathfind(3, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_D_Chebyshev_FloodFill() => ColoniesPathfind(4, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_E_Chebyshev_FloodFill() => ColoniesPathfind(5, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_F_Chebyshev_FloodFill() => ColoniesPathfind(6, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_G_Chebyshev_FloodFill() => ColoniesPathfind(7, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_H_Chebyshev_FloodFill() => ColoniesPathfind(8, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_I_Chebyshev_FloodFill() => ColoniesPathfind(9, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_J_Chebyshev_FloodFill() => ColoniesPathfind(10, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_K_Chebyshev_FloodFill() => ColoniesPathfind(11, FF, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_L_Chebyshev_FloodFill() => ColoniesPathfind(12, FF, Chebyshev);
    }

    [TestFixture]
    public class ChebyshevJPS : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Chebyshev_JPS() => ColoniesPathfind(1, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_B_Chebyshev_JPS() => ColoniesPathfind(2, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_C_Chebyshev_JPS() => ColoniesPathfind(3, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_D_Chebyshev_JPS() => ColoniesPathfind(4, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_E_Chebyshev_JPS() => ColoniesPathfind(5, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_F_Chebyshev_JPS() => ColoniesPathfind(6, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_G_Chebyshev_JPS() => ColoniesPathfind(7, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_H_Chebyshev_JPS() => ColoniesPathfind(8, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_I_Chebyshev_JPS() => ColoniesPathfind(9, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_J_Chebyshev_JPS() => ColoniesPathfind(10, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_K_Chebyshev_JPS() => ColoniesPathfind(11, JPS, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_L_Chebyshev_JPS() => ColoniesPathfind(12, JPS, Chebyshev);
    }

    [TestFixture, Obsolete]
    public class ChebyshevAStar : PathfindingBenchmarkReport
    {
        [Test, Performance, Timeout(timeout)] public void Level_A_Chebyshev_AStar() => ColoniesPathfind(1,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_B_Chebyshev_AStar() => ColoniesPathfind(2,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_C_Chebyshev_AStar() => ColoniesPathfind(3,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_D_Chebyshev_AStar() => ColoniesPathfind(4,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_E_Chebyshev_AStar() => ColoniesPathfind(5,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_F_Chebyshev_AStar() => ColoniesPathfind(6,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_G_Chebyshev_AStar() => ColoniesPathfind(7,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_H_Chebyshev_AStar() => ColoniesPathfind(8,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_I_Chebyshev_AStar() => ColoniesPathfind(9,  AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_J_Chebyshev_AStar() => ColoniesPathfind(10, AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_K_Chebyshev_AStar() => ColoniesPathfind(11, AStar, Chebyshev);
        [Test, Performance, Timeout(timeout)] public void Level_L_Chebyshev_AStar() => ColoniesPathfind(12, AStar, Chebyshev);
    }
}

