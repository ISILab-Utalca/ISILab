using Commons.Optimization.Evaluator;
using ISILab.AI.Categorization;
using ISILab.Commons.JsonNet;   
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;


namespace ISILab.LBS.Tests
{

    [TestFixture]
    public class MAPElitesBenchmarkReport
    {

        LBSLevelData levelData;
        AssistantMapElite assistant;
        MAPElitesPreset preset;
        IRangedEvaluator og_optimizer;
        IRangedEvaluator og_xEvaluator;
        IRangedEvaluator og_yEvaluator;

        const string level4Rooms = "04acda0b4a6f7ca4da575ba34b30d554";
        const string level20Rooms = "b93245dd9ffc3d84d9b6bb9e58d1d05e";

        const string dungeonPresetPath = "Assets/ISILab/LBS/Presets/Assistants/DungeonPreset.asset";

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

        #region auxiliary methods

        // This method sets up the MAP-Elites assistant for testing, loading the level data, configuring the evaluators, and initializing the assistant with the provided preset.
        private void SetUpMAPElitesTest(string _guid, string presetPath, IRangedEvaluator optimizer, IRangedEvaluator xEvaluator, IRangedEvaluator yEvaluator)
        {
            levelData = JSONDataManager.LoadDataByGUID<LBSLevelData>(_guid);
            Assert.IsNotNull(levelData, "Could not load level.");
            LBSLayer fistLayer = levelData.GetLayer("Population");
            Assert.IsNotNull(fistLayer, "Layer was null.");
            assistant = fistLayer.GetAssistant<AssistantMapElite>();
            Assert.IsNotNull(assistant, "Assistant Map Elite was null");
            fistLayer.Reload();

            assistant.Testing = true; // Prevents the algorithm from trying to use the window

            //Assert.IsNotNull(assistant.LayerPopulation, "Cannot get Population Behaviour through assistant.");
            //Assert.IsNotNull(assistant.LayerPopulation.OwnerLayer, "Cannot get layer through assistant.");
            //Assert.IsNotNull(assistant.LayerPopulation.OwnerLayer.Parent, "Layer parent was null."); // <--- Falla
            //Assert.IsNotNull(assistant.Data, "Could not read level data through assistant.");
            //Assert.IsNotNull(assistant.Data.ContextLayers, "Could not read level context layers.");
            assistant.LayerPopulation.OwnerLayer.Parent = levelData;
            Assert.IsTrue(assistant.Data.ContextLayers.Count > 0, "No context layers found.");
            assistant.AutoSelectArea(out _);
            Assert.IsTrue(assistant.RawToolRect.width > 0 && assistant.RawToolRect.height > 0, "Area selection is 0.");

            preset = AssetDatabase.LoadAssetAtPath<MAPElitesPreset>(presetPath);
            Assert.IsNotNull(preset, "Could not load preset.");
            Assert.IsNotNull(preset.Optimizer, "Optimizer was null");
            //Assert.IsNotNull(preset.Optimizer.Evaluator, "Optimizer evaluator was null");
            //Assert.IsNotNull(preset.XEvaluator)

            og_optimizer = preset.Optimizer.Evaluator as IRangedEvaluator;
            og_xEvaluator = preset.XEvaluator;
            og_yEvaluator = preset.YEvaluator;

            preset.Optimizer.Evaluator = optimizer;
            preset.XEvaluator = xEvaluator;
            preset.YEvaluator = yEvaluator;

            assistant.InitializeEvaluator(preset.Optimizer.Evaluator);
            assistant.InitializeEvaluator(preset.XEvaluator);
            assistant.InitializeEvaluator(preset.YEvaluator);

            assistant.LoadPresset(preset);
            assistant.SetAdam(assistant.RawToolRect, levelData.ContextLayers);
        }

        // This method cleans up after each test, restoring the original evaluators and clearing the level data and assistant instances.
        private void CleanUpMAPElitesTest()
        {
            preset.Optimizer.Evaluator = og_optimizer;
            preset.XEvaluator = og_xEvaluator;
            preset.YEvaluator = og_yEvaluator;

            if (levelData is not null)
            {
                LBSLayer firstLayer = levelData.GetLayer(0);
                firstLayer.RemoveAll();

                assistant = null;
                levelData = null;
            }
        }
        // This method retrieves the current chromosome (individual) from the MAP-Elites assistant for evaluation purposes.
        private BundleTilemapChromosome GetChromosomeFromAssistant()
        {
            var mapElitesField = typeof(AssistantMapElite).GetField("mapElites", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var mapElitesObj = mapElitesField.GetValue(assistant);

            if (mapElitesObj != null)
            {
                var adamProp = mapElitesObj.GetType().GetProperty("Adam");
                return adamProp.GetValue(mapElitesObj) as BundleTilemapChromosome;
            }
            return null;
        }

        #endregion

        #region Only Evaluate

        // These functions are responsible for measuring only the evaluation time of each evaluator and the fitness of the map, without considering the entire MAP-Elites execution process.

        [Test, Performance]
        [Timeout(600000)]
        public void OnlyEvaluateMAPElites_20_Rooms_Exploration()
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
                SetUpMAPElitesTest(level20Rooms, dungeonPresetPath, new DCExploration(), new DCResourceSafety(), new DCSafeArea());
                chromosome = GetChromosomeFromAssistant();
                evaluator.InitializeDefaultWithContext(levelData.ContextLayers, assistant.RawToolRect);
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
                evaluator.InitializeDefaultWithContext(levelData.ContextLayers, assistant.RawToolRect);
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
                evaluator.InitializeDefaultWithContext(levelData.ContextLayers, assistant.RawToolRect);
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
                evaluator.InitializeDefaultWithContext(levelData.ContextLayers, assistant.RawToolRect);
            })
            .CleanUp(CleanUpMAPElitesTest)
            .Run();
        }

        #endregion


    }
}
