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
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.MapTools.Generators;
using LBS.Components;

namespace ISILab.LBS.Template.Editor
{
    [LBSCustomEditor("Layer template", typeof(LayerTemplate))]
    [CustomEditor(typeof(LayerTemplate))]
    public class LayerTemplateEditor : UnityEditor.Editor
    {

        #region Fields
        private int _behaviourIndex;
        private int _assistantIndex;
        private int _ruleIndex;
        
        private static List<Type> s_behaviourOptions;
        private static List<Type> s_assistantOptions;
        private static List<Type> s_ruleOptions;
        private static string[] s_behaviourNames;
        private static string[] s_assistantNames;
        private static string[] s_ruleNames;
        
        private static VectorImage s_behaviourIcon;
        private static VectorImage s_assistantIcon;

        private const string DefaultBehaviorIcon = "e17eb0e02534666439fca8ea30b4d4e4";
        private const string DefaultAssistantIcon = "ad8feef201665454ca79e31b7d798ac3";

        #endregion
        
        #region Properties
        private LayerTemplate Template => (LayerTemplate)target;
        #endregion
        
        #region Lifecycle
        private void OnEnable()
        {
            EnsureCaches();
        }

        private static void EnsureCaches()
        {
            if (s_behaviourOptions != null) return; // already cached

            // Cache derived types safely
            s_behaviourOptions = typeof(LBSBehaviour).GetDerivedTypes().ToList();
            s_assistantOptions = typeof(LBSAssistant).GetDerivedTypes().ToList();
            s_ruleOptions = typeof(LBSGeneratorRule).GetDerivedTypes().ToList();

            s_behaviourNames = s_behaviourOptions.Select(t => t.Name).ToArray();
            s_assistantNames = s_assistantOptions.Select(t => t.Name).ToArray();
            s_ruleNames = s_ruleOptions.Select(t => t.Name).ToArray();

            // Load icons (AssetDatabase is editor-only and cheap here)
            try
            {
                s_behaviourIcon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(DefaultBehaviorIcon);
            }
            catch
            {
                s_behaviourIcon = null;
            }

            try
            {
                s_assistantIcon = LBSAssetMacro.LoadAssetByGuid<VectorImage>(DefaultAssistantIcon);
            }
            catch
            {
                s_assistantIcon = null;
            }
        }
        #endregion

        #region Inspector GUI
        public override void OnInspectorGUI()
        {
            // Draw default inspector using serializedObject (keeps undo/redo & prefab workflows)
            serializedObject.Update();
            DrawDefaultInspectorExcludingInternalFields();

            EditorGUILayout.Space(12);

            DrawAddBlock();

            EditorGUILayout.Space(14);

            DrawPresetsBlock();

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Apply Changes"))
            {
                ApplyChanges();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultInspectorExcludingInternalFields()
        {
            // place o hide any properties or field not desired example in commented parameter:
            DrawPropertiesExcluding(serializedObject, new string[] { /* generatorRules */ });
        }

        private void DrawAddBlock()
        {
            EditorGUILayout.LabelField("Add to Template", EditorStyles.boldLabel);

            DrawAddSection("Behaviour", ref _behaviourIndex, s_behaviourNames, s_behaviourOptions, AddBehaviour);
            DrawAddSection("Assistant", ref _assistantIndex, s_assistantNames, s_assistantOptions, AddAssistant);
            DrawAddSection("Generator", ref _ruleIndex, s_ruleNames, s_ruleOptions, AddGeneratorRule);
        }

        private void DrawPresetsBlock()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Interior")) InteriorConstruct();
            if (GUILayout.Button("Exterior")) ExteriorConstruct();
            if (GUILayout.Button("Population")) PopulationConstruct();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Quest")) QuestConstruct();
            if (GUILayout.Button("Simulation")) SimulationConstruct();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region UI Helpers
        private void DrawAddSection(string label, ref int index, string[] names, List<Type> types, Action<Type> onAdd)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                index = EditorGUILayout.Popup($"{label} Type:", index, names);
                if (!GUILayout.Button($"Add {label}", GUILayout.Width(130))) return;
                
                try
                {
                    onAdd?.Invoke(types[index]);
                    EditorUtility.SetDirty(Template);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        #endregion

        #region Add Methods
        private void AddBehaviour(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type, s_behaviourIcon, type.Name, Color.clear) is LBSBehaviour instance)
            {
                Template.layer.AddBehaviour(instance);
            }
        }

        private void AddAssistant(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type, s_assistantIcon, type.Name, Color.clear) is LBSAssistant instance)
            {
                Template.layer.AddAssistant(instance);
            }
        }

        private void AddGeneratorRule(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type) is LBSGeneratorRule instance)
            {
                Template.layer.AddGeneratorRule(instance);
            }
        }
        #endregion

        #region Preset helpers
        private void ApplyPreset(Action<LBSLayer> fill)
        {
            Template.Clear();
            LBSLayer layer = Template.layer;
            fill(layer);
            ApplyChanges();
        }

        /// <summary>
        /// Generic factory for creating and attaching Behaviours, Assistants, or Rules.
        /// </summary>
        private void AddObject<T>(LBSLayer layer, string objectName, string iconGuid, Color color)
            where T : class
        {
            if (layer == null)
            {
                Debug.LogError("Layer is null — cannot create object.");
                return;
            }

            // Try to construct with the standard (guid, name, color) pattern
            object[] constructorArgs = { iconGuid, objectName, color };
            T instance = Activator.CreateInstance(typeof(T), constructorArgs) as T;

            if (instance == null)
            {
                Debug.LogError($"Failed to create instance of {typeof(T).Name}");
                return;
            }

            // Attach to layer if the method exists
            var attachMethod = typeof(T).GetMethod("OnAttachLayer", new[] { typeof(LBSLayer) });
            attachMethod?.Invoke(instance, new object[] { layer });

            // Add instance to the correct list
            switch (instance)
            {
                case LBSBehaviour behaviour:
                    layer.AddBehaviour(behaviour);
                    break;

                case LBSAssistant assistant:
                    layer.AddAssistant(assistant);
                    break;

                case LBSGeneratorRule rule:
                    layer.AddGeneratorRule(rule);
                    break;

                default:
                    Debug.LogWarning($"Unsupported object type: {typeof(T).Name}");
                    break;
            }
        }

        private void InteriorConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.ID = "Interior";
                layer.Name = "Layer Interior";
                layer.iconGuid = "8c78cf0f5376fd846a188536ff3497ae";

                AddObject<SchemaBehaviour>(layer, "Schema behaviour", LBSAssetMacro.GetGuidFromAsset(s_behaviourIcon), Settings.LBSSettings.Instance.view.behavioursColor);
                AddObject<HillClimbingAssistant>(layer, "HillClimbing", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);

                AddObject<SchemaRuleGenerator>(layer, "Schema Rule Generator", "", Color.clear);
                AddObject<SchemaRuleGeneratorExterior>(layer, "Schema Rule Generator Exterior", "", Color.clear);

                layer.Settings = new Generator3D.Settings { scale = new Vector2Int(2, 2), name = "Interior" };
            });
        }

        private void ExteriorConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.ID = "Exterior";
                layer.Name = "Layer Exterior";
                layer.iconGuid = "02a644759487ae249bc3a20d019c8745";

                AddObject<ExteriorBehaviour>(layer, "Exterior behaviour", LBSAssetMacro.GetGuidFromAsset(s_behaviourIcon), Settings.LBSSettings.Instance.view.behavioursColor);
                AddObject<AssistantWFC>(layer, "Assistant WFC", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);
                AddObject<ExteriorRuleGenerator>(layer, "Exterior Rule Generator", "", Color.clear);

                layer.Settings = new Generator3D.Settings { scale = new Vector2Int(2, 2), name = "Exterior" };
            });
        }

        private void PopulationConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.ID = "Population";
                layer.Name = "Layer Population";
                layer.iconGuid = "48f2011efc0f7b2449db9f824c895d9d";

                AddObject<PopulationBehaviour>(layer, "Population Behavior", LBSAssetMacro.GetGuidFromAsset(s_behaviourIcon), Settings.LBSSettings.Instance.view.behavioursColor);
                AddObject<AssistantMapElite>(layer, "Map Elite - Genetic Algorithm", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);
                AddObject<PopulationRuleGenerator>(layer, "Population Rule Generator", "", Color.clear);

                layer.Settings = new Generator3D.Settings { scale = new Vector2Int(2, 2), name = "Population" };
            });
        }

        private void QuestConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.ID = "Quest";
                layer.Name = "Layer Quest";
                layer.iconGuid = "9fc8ac6f82a8b39458c73185d378ffbf";

                AddObject<QuestBehaviour>(layer, "Quest Behavior", LBSAssetMacro.GetGuidFromAsset(s_behaviourIcon), Settings.LBSSettings.Instance.view.behavioursColor);
                AddObject<GrammarAssistant>(layer, "Grammar Assistant", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);
                AddObject<GrammarAssistant>(layer, "Quest Assistant", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);
                AddObject<QuestRuleGenerator>(layer, "Quest Rule Generator", "", Color.clear);

                layer.Settings = new Generator3D.Settings { scale = new Vector2Int(2, 2), name = "Quest" };
            });
        }

        private void SimulationConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.ID = "Simulation";
                layer.Name = "Layer Simulation";
                layer.iconGuid = "13f64883312513a41adeb7dec75a3a5f";

                AddObject<PathOSBehaviour>(layer, "Simulation Behaviour", LBSAssetMacro.GetGuidFromAsset(s_behaviourIcon), Settings.LBSSettings.Instance.view.behavioursColor);
                AddObject<TestingAssistant>(layer, "Simulation Assistant", LBSAssetMacro.GetGuidFromAsset(s_assistantIcon), Settings.LBSSettings.Instance.view.assistantColor);
                AddObject<PathOSRuleGenerator>(layer, "Simulation Rule Generator", "", Color.clear);

                layer.Settings = new Generator3D.Settings { scale = new Vector2Int(2, 2), name = "Simulation" };
            });
        }

        #endregion
        
        #region Utilities
        private void ApplyChanges()
        {
            EditorUtility.SetDirty(Template);
            AssetDatabase.SaveAssets();
            Debug.Log("LayerTemplate saved.");
        }
        #endregion
    }
}


/* OLD
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

            if (GUILayout.Button("Apply Changes")) ApplyChanges();
               
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
            layer.iconGuid = "Assets/isi-lab-unity-module/Commons/Assets2D/Resources/Icons/Vectorial/Icon=InteriorLayerIcon.png";

            layer.AddBehaviour(new SchemaBehaviour(LBSAssetMacro.GetGuidFromAsset(behaviourIcon), "Schema behaviour", Settings.LBSSettings.Instance.view.behavioursColor));
            layer.AddAssistant(new HillClimbingAssistant(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "HillClimbing", Settings.LBSSettings.Instance.view.assistantColor));

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
            layer.iconGuid = "Assets/isi-lab-unity-module/LBS/Plugin/Assets2D/Resources/Icons/pine-tree.png";

            var bh = new ExteriorBehaviour(LBSAssetMacro.GetGuidFromAsset(behaviourIcon), "Exterior behaviour", Settings.LBSSettings.Instance.view.behavioursColor);
            bh.OnAttachLayer(layer);
            layer.AddBehaviour(bh);

            var ass = new AssistantWFC(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "Assistant WFC", Settings.LBSSettings.Instance.view.assistantColor);
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
            layer.iconGuid = "Assets/isi-lab-unity-module/LBS/Plugin/Assets2D/Resources/Icons/ghost.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Population"
            };

            layer.AddBehaviour(new PopulationBehaviour(LBSAssetMacro.GetGuidFromAsset(behaviourIcon), "Population Behavior", Settings.LBSSettings.Instance.view.behavioursColor));

            var ass = new AssistantMapElite(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "Map Elite - Genetic Algorithm", Settings.LBSSettings.Instance.view.assistantColor);
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
            layer.iconGuid = "Assets/isi-lab-unity-module/LBS/Plugin/Assets2D/Resources/Icons/Stamp_Icon.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Quest"
            };

            var bh = new QuestBehaviour(LBSAssetMacro.GetGuidFromAsset(behaviourIcon), "Quest Behavior", Settings.LBSSettings.Instance.view.behavioursColor);
            bh.OnAttachLayer(layer);
            layer.AddBehaviour(bh);

            var ass1 = new GrammarAssistant(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "Grammar Assistant", Settings.LBSSettings.Instance.view.assistantColor);
            ass1.OnAttachLayer(layer);
            layer.AddAssistant(ass1);

            var ass2 = new GrammarAssistant(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "Quest Assistant", Settings.LBSSettings.Instance.view.assistantColor);
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
            layer.iconGuid = "Assets/isi-lab-unity-module/LBS/GABO/Resources/Icons/TinyIconPathOSModule16x16.png";

            layer.Settings = new Generator3D.Settings
            {
                scale = new Vector2Int(2, 2),
                name = "Simulation"
            };

            layer.AddBehaviour(new PathOSBehaviour(LBSAssetMacro.GetGuidFromAsset(behaviourIcon), "Simulation Behaviour", Settings.LBSSettings.Instance.view.behavioursColor));
            layer.AddAssistant(new TestingAssistant(LBSAssetMacro.GetGuidFromAsset(assistantIcon), "Simulation Assistant", Settings.LBSSettings.Instance.view.assistantColor));
            layer.AddGeneratorRule(new PathOSRuleGenerator());

            Debug.Log("Set Simulation Default");
            ApplyChanges();
        }

        #endregion
    }
}
*/