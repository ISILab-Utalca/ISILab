using ISILab.AI.Categorization;
using ISILab.Commons.JsonNet;
using ISILab.LBS;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public abstract class MAPElitesBaseBenchmark
    {
        protected LBSLevelData levelData;
        protected AssistantMapElite assistant;
        protected MAPElitesPreset preset;
        protected IRangedEvaluator og_optimizer;
        protected IRangedEvaluator og_xEvaluator;
        protected IRangedEvaluator og_yEvaluator;

        protected const string dungeonPresetPath = "Assets/ISILab/LBS/Presets/Assistants/DungeonPreset.asset";
        protected const int timeout = 600000;

        #region auxiliary methods

        protected bool GetLevel(string guid)
        {
            levelData = JSONDataManager.LoadDataByGUID<LBSLevelData>(guid);
            return levelData is not null;
        }

        // This method sets up the MAP-Elites assistant for testing, loading the level data, configuring the evaluators, and initializing the assistant with the provided preset.
        protected void SetUpMAPElitesTest(string _guid, string presetPath, IRangedEvaluator optimizer, IRangedEvaluator xEvaluator, IRangedEvaluator yEvaluator, string baseLayer = "", Rect area = default)
        {
            levelData ??= JSONDataManager.LoadDataByGUID<LBSLevelData>(_guid);
            Assert.IsNotNull(levelData, "Could not load level.");
            LBSLayer fistLayer = string.IsNullOrEmpty(baseLayer) ? levelData.GetPopulationLayer() : levelData.Layers.Find(l => l.Name.Equals(baseLayer));
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

            if(area == default)
            {
                assistant.AutoSelectArea(out _);
            }
            else
            {
                assistant.RawToolRect = area;
            }
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

            assistant.InitializeEvaluator(preset.Optimizer.Evaluator, true);
            assistant.InitializeEvaluator(preset.XEvaluator, true);
            assistant.InitializeEvaluator(preset.YEvaluator, true);

            assistant.LoadPresset(preset);
            assistant.SetAdam(assistant.RawToolRect, levelData.ContextLayers);
        }

        // This method cleans up after each test, restoring the original evaluators and clearing the level data and assistant instances.
        protected void CleanUpMAPElitesTest()
        {
            preset.Optimizer.Evaluator = og_optimizer;
            preset.XEvaluator = og_xEvaluator;
            preset.YEvaluator = og_yEvaluator;

            if (levelData is not null)
            {
                LBSLayer firstLayer = levelData.GetPopulationLayer();
                firstLayer.RemoveAll();

                assistant = null;
                levelData = null;
            }
        }
        // This method retrieves the current chromosome (individual) from the MAP-Elites assistant for evaluation purposes.
        protected BundleTilemapChromosome GetChromosomeFromAssistant()
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
    }
}

