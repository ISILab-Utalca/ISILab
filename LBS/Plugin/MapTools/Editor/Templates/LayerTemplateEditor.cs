using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.Generators;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Assistants;
using ISILab.Macros;

namespace ISILab.LBS.Template.Editor
{
    [LBSCustomEditor("Layer template", typeof(LayerTemplate))]
    [CustomEditor(typeof(LayerTemplate))]
    public class LayerTemplateEditor : UnityEditor.Editor
    {
        private int behaviourIndex;
        private int assistantIndex;
        private int ruleIndex;

        private List<Type> behaviourOptions;
        private List<Type> assistantOptions;
        private List<Type> ruleOptions;

        private string[] behaviourNames;
        private string[] assistantNames;
        private string[] ruleNames;

        private VectorImage behaviourIcon;
        private VectorImage assistantIcon;

        private const string DefaultBehaviorIcon = "e17eb0e02534666439fca8ea30b4d4e4";
        private const string DefaultAssistantIcon = "ad8feef201665454ca79e31b7d798ac3";

        private LayerTemplate Template => (LayerTemplate)target;

        private void OnEnable()
        {
            // Cache derived types only once
            behaviourOptions = typeof(LBSBehaviour).GetDerivedTypes().ToList();
            assistantOptions = typeof(LBSAssistant).GetDerivedTypes().ToList();
            ruleOptions = typeof(LBSGeneratorRule).GetDerivedTypes().ToList();

            // Cache string arrays for popups
            behaviourNames = behaviourOptions.Select(t => t.Name).ToArray();
            assistantNames = assistantOptions.Select(t => t.Name).ToArray();
            ruleNames = ruleOptions.Select(t => t.Name).ToArray();

            // Load icons once
            behaviourIcon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(DefaultBehaviorIcon);
            assistantIcon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(DefaultAssistantIcon);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(15);
            DrawAddSection("Behaviour", ref behaviourIndex, behaviourNames, behaviourOptions, AddBehaviour);
            DrawAddSection("Assistant", ref assistantIndex, assistantNames, assistantOptions, AddAssistant);
            DrawAddSection("Generator", ref ruleIndex, ruleNames, ruleOptions, AddGeneratorRule);

            GUILayout.Space(20);

            DrawPresetButtons();

            if (GUILayout.Button("Apply Changes"))
                ApplyChanges();
        }

        #region UI Helpers

        private void DrawAddSection(string label, ref int index, string[] names, List<Type> types, Action<Type> onAdd)
        {
            GUILayout.BeginHorizontal();
            index = EditorGUILayout.Popup($"{label} Type:", index, names);
            if (GUILayout.Button($"Add {label}", GUILayout.Width(130)))
                onAdd?.Invoke(types[index]);
            GUILayout.EndHorizontal();
        }

        private void DrawPresetButtons()
        {
            GUILayout.Label("Presets", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Set as Interior")) InteriorConstruct();
            if (GUILayout.Button("Set as Exterior")) ExteriorConstruct();
            if (GUILayout.Button("Set as Population")) PopulationConstruct();
            if (GUILayout.Button("Set as Quest")) QuestConstruct();
            if (GUILayout.Button("Set as Simulation")) SimulationConstruct();
        }

        private void ApplyChanges()
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Add Methods

        private void AddBehaviour(Type type)
        {
            var instance = Activator.CreateInstance(type, behaviourIcon, type.Name, Color.clear) as LBSBehaviour;
            Template.layer.AddBehaviour(instance);
        }

        private void AddAssistant(Type type)
        {
            var instance = Activator.CreateInstance(type, assistantIcon, type.Name, Color.clear) as LBSAssistant;
            Template.layer.AddAssistant(instance);
        }

        private void AddGeneratorRule(Type type)
        {
            var instance = Activator.CreateInstance(type) as LBSGeneratorRule;
            Template.layer.AddGeneratorRule(instance);
        }

        #endregion

        #region Presets

        private void InteriorConstruct()
        {
            Template.Clear();
            var layer = Template.layer;
            layer.ID = "Interior";
            layer.Name = "Layer Interior";
            layer.iconGuid = "Assets/ISI Lab/Commons/Assets2D/Resources/Icons/Vectorial/Icon=InteriorLayerIcon.png";

            layer.AddBehaviour(new SchemaBehaviour(behaviourIcon, "Schema behaviour", Settings.LBSSettings.Instance.view.behavioursColor));
            layer.AddAssistant(new HillClimbingAssistant(assistantIcon, "HillClimbing", Settings.LBSSettings.Instance.view.assistantColor));

            layer.AddGeneratorRule(new SchemaRuleGenerator());
            layer.AddGeneratorRule(new SchemaRuleGeneratorExterior());

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Interior"
            };

            Debug.Log("Set Interior Default");
            ApplyChanges();
        }

        private void ExteriorConstruct()
        {
            Template.Clear();
            var layer = Template.layer;
            layer.ID = "Exterior";
            layer.Name = "Layer Exterior";
            layer.iconGuid = "Assets/ISI Lab/LBS/Plugin/Assets2D/Resources/Icons/pine-tree.png";

            var bh = new ExteriorBehaviour(behaviourIcon, "Exterior behaviour", Settings.LBSSettings.Instance.view.behavioursColor);
            bh.OnAttachLayer(layer);
            layer.AddBehaviour(bh);

            var ass = new AssistantWFC(assistantIcon, "Assistant WFC", Settings.LBSSettings.Instance.view.assistantColor);
            ass.OnAttachLayer(layer);
            layer.AddAssistant(ass);

            layer.AddGeneratorRule(new ExteriorRuleGenerator());

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Exterior"
            };

            Debug.Log("Set Exterior Default");
            ApplyChanges();
        }

        private void PopulationConstruct()
        {
            Template.Clear();
            var layer = Template.layer;
            layer.ID = "Population";
            layer.Name = "Layer Population";
            layer.iconGuid = "Assets/ISI Lab/LBS/Plugin/Assets2D/Resources/Icons/ghost.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Population"
            };

            layer.AddBehaviour(new PopulationBehaviour(behaviourIcon, "Population Behavior", Settings.LBSSettings.Instance.view.behavioursColor));

            var ass = new AssistantMapElite(assistantIcon, "Map Elite - Genetic Algorithm", Settings.LBSSettings.Instance.view.assistantColor);
            ass.OnAttachLayer(layer);
            layer.AddAssistant(ass);

            layer.AddGeneratorRule(new PopulationRuleGenerator());

            Debug.Log("Set Population Default");
            ApplyChanges();
        }

        private void QuestConstruct()
        {
            Template.Clear();
            var layer = Template.layer;
            layer.ID = "Quest";
            layer.Name = "Layer Quest";
            layer.iconGuid = "Assets/ISI Lab/LBS/Plugin/Assets2D/Resources/Icons/Stamp_Icon.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Quest"
            };

            var bh = new QuestBehaviour(behaviourIcon, "Quest Behavior", Settings.LBSSettings.Instance.view.behavioursColor);
            bh.OnAttachLayer(layer);
            layer.AddBehaviour(bh);

            var ass1 = new GrammarAssistant(assistantIcon, "Grammar Assistant", Settings.LBSSettings.Instance.view.assistantColor);
            ass1.OnAttachLayer(layer);
            layer.AddAssistant(ass1);

            var ass2 = new GrammarAssistant(assistantIcon, "Quest Assistant", Settings.LBSSettings.Instance.view.assistantColor);
            ass2.OnAttachLayer(layer);
            layer.AddAssistant(ass2);

            layer.AddGeneratorRule(new QuestRuleGenerator());

            Debug.Log("Set Quest Default");
            ApplyChanges();
        }

        private void SimulationConstruct()
        {
            Template.Clear();
            var layer = Template.layer;
            layer.ID = "Simulation";
            layer.Name = "Layer Simulation";
            layer.iconGuid = "Assets/ISI Lab/LBS/GABO/Resources/Icons/TinyIconPathOSModule16x16.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Simulation"
            };

            layer.AddBehaviour(new PathOSBehaviour(behaviourIcon, "Simulation Behaviour", Settings.LBSSettings.Instance.view.behavioursColor));
            layer.AddAssistant(new TestingAssistant(assistantIcon, "Simulation Assistant", Settings.LBSSettings.Instance.view.assistantColor));
            layer.AddGeneratorRule(new PathOSRuleGenerator());

            Debug.Log("Set Simulation Default");
            ApplyChanges();
        }

        #endregion
    }
}
