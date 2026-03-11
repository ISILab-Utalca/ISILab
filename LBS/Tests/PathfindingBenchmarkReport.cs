using ISILab.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class PathfindingBenchmarkReport : MAPElitesBaseBenchmark
    {
        const string pathfindRoom = "69adc1e45b1df6645a15c4293b4f58ad";

        const PathfindingAlgorithm FF = PathfindingAlgorithm.Flood_Fill;
        const PathfindingAlgorithm JPS = PathfindingAlgorithm.JPS_Plus;

        private void Pathfind(Type type, int mapSize, int enemyQuantity, int wallQuantity, PathfindingAlgorithm searchType)
        {
            var evaluator = Activator.CreateInstance(type) as IDistanceEvaluator;
            BundleTilemapChromosome chromosome = null;
            SampleGroup fitnessGroup = new SampleGroup("Fitness Score", SampleUnit.Undefined);
            SampleGroup visitedNodesGroup = new SampleGroup("Visited Nodes", SampleUnit.Undefined);

            List<LBSLayer> oldContext = new();

            Measure.Method(() =>
            {
                double fitness = evaluator.EvaluateWithInfo(chromosome, out EvaluationInfo info);
                //Measure.Custom(fitnessGroup, fitness);
                Measure.Custom(visitedNodesGroup, info.visitedNodes);
            })
            .WarmupCount(5)
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

        private Rect GetArea(int value)
        {
            Vector2 size = Vector2.zero;
            switch(value)
            {
                case 1: size = new Vector2(15, 10); break;
                case 2: size = new Vector2(30, 20); break;
                case 3: size = new Vector2(40, 40); break;
                default: size = default; break;
            }

            return new Rect(Vector2.zero, size);
        }

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_FewWalls        () => Pathfind(typeof(Colonies), 1, 1, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_FewWalls       () => Pathfind(typeof(Colonies), 2, 1, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_FewWalls          () => Pathfind(typeof(Colonies), 3, 1, 1, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_FewWalls     () => Pathfind(typeof(Colonies), 1, 2, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_FewWalls    () => Pathfind(typeof(Colonies), 2, 2, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_FewWalls       () => Pathfind(typeof(Colonies), 3, 2, 1, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_FewWalls       () => Pathfind(typeof(Colonies), 1, 3, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_FewWalls      () => Pathfind(typeof(Colonies), 2, 3, 1, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_FewWalls         () => Pathfind(typeof(Colonies), 3, 3, 1, FF);


        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_MediumWalls     () => Pathfind(typeof(Colonies), 1, 1, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 2, 1, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_MediumWalls       () => Pathfind(typeof(Colonies), 3, 1, 2, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_MediumWalls  () => Pathfind(typeof(Colonies), 1, 2, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_MediumWalls () => Pathfind(typeof(Colonies), 2, 2, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 3, 2, 2, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 1, 3, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_MediumWalls   () => Pathfind(typeof(Colonies), 2, 3, 2, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 3, 3, 2, FF);


        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_FewEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 1, 1, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_FewEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 2, 1, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_FewEnemies_ManyWalls         () => Pathfind(typeof(Colonies), 3, 1, 3, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_MediumEnemies_ManyWalls    () => Pathfind(typeof(Colonies), 1, 2, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_MediumEnemies_ManyWalls   () => Pathfind(typeof(Colonies), 2, 2, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_MediumEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 3, 2, 3, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_SmallMap_ManyEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 1, 3, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_MediumMap_ManyEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 2, 3, 3, FF);
        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_ManyEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 3, 3, 3, FF);

        [Test, Performance, Timeout(timeout)] public void FloodFill_BigMap_TooManyEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 3, 4, 3, FF);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_FewWalls          () => Pathfind(typeof(Colonies), 1, 1, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_FewWalls         () => Pathfind(typeof(Colonies), 2, 1, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_FewWalls            () => Pathfind(typeof(Colonies), 3, 1, 1, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_FewWalls       () => Pathfind(typeof(Colonies), 1, 2, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_FewWalls      () => Pathfind(typeof(Colonies), 2, 2, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_FewWalls         () => Pathfind(typeof(Colonies), 3, 2, 1, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_FewWalls         () => Pathfind(typeof(Colonies), 1, 3, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_FewWalls        () => Pathfind(typeof(Colonies), 2, 3, 1, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_FewWalls           () => Pathfind(typeof(Colonies), 3, 3, 1, JPS);


        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_MediumWalls       () => Pathfind(typeof(Colonies), 1, 1, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 2, 1, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_MediumWalls         () => Pathfind(typeof(Colonies), 3, 1, 2, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_MediumWalls    () => Pathfind(typeof(Colonies), 1, 2, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_MediumWalls   () => Pathfind(typeof(Colonies), 2, 2, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 3, 2, 2, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_MediumWalls      () => Pathfind(typeof(Colonies), 1, 3, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_MediumWalls     () => Pathfind(typeof(Colonies), 2, 3, 2, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_MediumWalls        () => Pathfind(typeof(Colonies), 3, 3, 2, JPS);


        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_FewEnemies_ManyWalls         () => Pathfind(typeof(Colonies), 1, 1, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_FewEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 2, 1, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_FewEnemies_ManyWalls           () => Pathfind(typeof(Colonies), 3, 1, 3, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_MediumEnemies_ManyWalls      () => Pathfind(typeof(Colonies), 1, 2, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_MediumEnemies_ManyWalls     () => Pathfind(typeof(Colonies), 2, 2, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_MediumEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 3, 2, 3, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_SmallMap_ManyEnemies_ManyWalls        () => Pathfind(typeof(Colonies), 1, 3, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_MediumMap_ManyEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 2, 3, 3, JPS);
        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_ManyEnemies_ManyWalls          () => Pathfind(typeof(Colonies), 3, 3, 3, JPS);

        [Test, Performance, Timeout(timeout)] public void JPS_BigMap_TooManyEnemies_ManyWalls       () => Pathfind(typeof(Colonies), 3, 4, 3, JPS);
    }
}

