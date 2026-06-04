using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Editor.UI.CustomComponents;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.MapTools.Editor.Templates
{
    [LBSCustomEditor("Layer template", typeof(LayerTemplate))]
    [CustomEditor(typeof(LayerTemplate))]
    public class LayerTemplateEditor : UnityEditor.Editor
    {
        #region Fields
        public VisualTreeAsset _layerTemplateInspector;

        private int _moduleIndex;
        private int _behaviourIndex;
        private int _assistantIndex;
        private int _ruleIndex;

        private static List<Type> s_moduleOptions;
        private static List<Type> s_behaviourOptions;
        private static List<Type> s_assistantOptions;
        private static List<Type> s_ruleOptions;

        private static string[] s_moduleNames;
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
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            //EditorUtility.SetDirty(Template);
            Repaint();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }


        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // ------------ DEBUG INSPECTOR ------------
            if (Template.DebugView)
            {
                root.Add(new IMGUIContainer(DrawDebugInspector));
                return root;
            }

            // ---------- SIMPLIFIED INSPECTOR ---------
            if (_layerTemplateInspector != null)
            {
                root.Add(_layerTemplateInspector.CloneTree());
            }

            // Debug view toggle
            var debugToggle = root.Q<LBSCustomToggle>("DebugViewToggle");
            DebugToggleSetup();

            // Template name field
            var nameField = root.Q<LBSCustomTextField>("TemplateNameField");
            TextFieldSetup(nameField, Template.Name, Template.SetName, "Change Template Name");

            // Sorting order field
            var sortingField = root.Q<LBSCustomIntField>("SortingOrderField");
            SortingFieldSetup();

            // Icon field
            var iconImage = root.Q<VisualElement>("IconImage");
            var iconButton = root.Q<LBSCustomButton>("IconButton");
            var iconPathLabel = root.Q<LBSCustomLabel>("IconPathLabel");
            IconFieldSetup();

            // Default name field
            var defaultNameField = root.Q<LBSCustomTextField>("DefaultNameField");
            TextFieldSetup(
                defaultNameField, 
                Template.layer.Name, 
                (value) => { Template.layer.Name = value; }, 
                "Change Layer's Default Name");

            // ID field
            var idField = root.Q<LBSCustomTextField>("IdField");
            TextFieldSetup(idField, Template.layer.ID, Template.layer.SetID, "Change Layer ID");

            // Tile size field
            var tileSizeField = root.Q<LBSCustomVector2IntField>("TileSizeField");
            TileSizeFieldSetup();

            // Floor count field
            var floorCountField = root.Q<LBSCustomUnsignedIntegerField>("FloorCountField");
            if(floorCountField != null)
            {
                // IN CONSTRUCTION...
                Template.layer.ChangeFloorCount(1);
            }

            // Modules list
            var modulesListGroup = root.Q<LBSBaseListGroup>("ModulesListGroup");
            ListGroupSetup(modulesListGroup, s_moduleOptions, Template.layer.Modules(), (element, index) =>
            {
                /*var modules = Template.layer.Modules();
                if (index < 0 || index >= modules.Count)
                {
                    element.RemoveFromHierarchy();
                    return;
                }//*/
                element.Q<LBSCustomLabel>("textL").text = Template.layer.Modules()[index].ID;
            });

            // Behaviours list
            var behavioursListGroup = root.Q<LBSBaseListGroup>("BehavioursListGroup");
            ListGroupSetup(behavioursListGroup, s_behaviourOptions, Template.layer.Behaviours, (element, index) =>
            {
                var label = element.Q<LBSCustomLabel>("textL");
                label.text = Template.layer.Behaviours[index].Name;
                label.style.color = Template.layer.Behaviours[index].ColorTint;
            });

            // Assistants list
            var assistantsListGroup = root.Q<LBSBaseListGroup>("AssistantsListGroup");
            ListGroupSetup(assistantsListGroup, s_assistantOptions, Template.layer.Assistants, (element, index) =>
            {
                var label = element.Q<LBSCustomLabel>("textL");
                label.text = Template.layer.Assistants[index].Name;
                label.style.color = Template.layer.Assistants[index].ColorTint;
            });

            // Generator rules list
            var rulesListGroup = root.Q<LBSBaseListGroup>("GeneratorRulesListGroup");
            ListGroupSetup(rulesListGroup, s_ruleOptions, Template.layer.GeneratorRules, (element, index) =>
            {
                var label = element.Q<LBSCustomLabel>("textL");
                label.text = Template.layer.GeneratorRules[index].GetType().Name;
            });

            return root;


            void DebugToggleSetup()
            {
                if (debugToggle == null)
                { NotFoundErrorLog("DebugViewToggle"); return; }
                debugToggle.value = Template.DebugView;

                debugToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(Template, "Toggle Debug View");
                    Template.DebugView = evt.newValue;
                    EditorUtility.SetDirty(Template);

                    // Fuerza reconstrucción del inspector
                    ActiveEditorTracker.sharedTracker.ForceRebuild();
                });
            }

            void TextFieldSetup(LBSCustomTextField field, string value, Action<string> OnValueChanged, string changeName)
            {
                if (field == null) 
                { NotFoundErrorLog("TemplateNameField"); return; }
                field.value = value;

                field.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(Template, changeName);
                    OnValueChanged.Invoke(evt.newValue);
                    EditorUtility.SetDirty(Template);
                });
            }

            void SortingFieldSetup()
            {
                if (sortingField == null) 
                { NotFoundErrorLog("SortingOrderField"); return; }
                sortingField.value = Template.Order;

                sortingField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(Template, "Change Sorting Order");
                    Template.Order = evt.newValue;
                    EditorUtility.SetDirty(Template);
                });
            }

            void IconFieldSetup()
            {
                if (iconImage == null || iconButton == null) 
                { NotFoundErrorLog("IconImage"); return; }

                // Set initial icon if it exists
                if (!string.IsNullOrEmpty(Template.layer.iconGuid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(Template.layer.iconGuid);
                    VectorImage icon = AssetDatabase.LoadAssetAtPath<VectorImage>(path);
                    if (icon != null)
                        iconImage.style.backgroundImage = new StyleBackground(icon);
                    iconPathLabel.text = path;
                }

                // Button behaviour
                iconButton.clicked += () =>
                {
                    string filePath = EditorUtility.OpenFilePanel(
                        "Select SVG Icon",
                        Application.dataPath,
                        "svg");
                    if (string.IsNullOrEmpty(filePath))
                        return;

                    // Convert absolute path to Assets-relative path
                    if (!filePath.StartsWith(Application.dataPath))
                    {
                        Debug.LogWarning("Selected file must be inside the project Assets folder.");
                        return;
                    }
                    string assetPath = "Assets" + filePath.Substring(Application.dataPath.Length);

                    // Load and set the icon
                    VectorImage vectorImage = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
                    if (vectorImage == null)
                    {
                        Debug.LogError($"Could not load SVG at: {assetPath}");
                        return;
                    }
                    iconImage.style.backgroundImage = new StyleBackground(vectorImage);
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    Template.layer.iconGuid = guid;
                    iconPathLabel.text = assetPath;
                    EditorUtility.SetDirty(target);
                };
            }
        
            void TileSizeFieldSetup()
            {
                if (tileSizeField == null) 
                { NotFoundErrorLog("TileSizeField"); return; }
                tileSizeField.value = Template.layer.TileSize;

                tileSizeField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(Template, "Change Tile Size");
                    Template.layer.TileSize = evt.newValue;
                    EditorUtility.SetDirty(Template);
                });
            }

            void ListGroupSetup<T>(LBSBaseListGroup listGroup, List<Type> options, List<T> items, Action<VisualElement, int> bindItem)
            {
                if(listGroup == null)
                { NotFoundErrorLog($"ListGroup<{typeof(T).Name}>"); return; }

                listGroup.BindListView(items, (_) => { }, () => new LBSCustomLabelItem(), bindItem);

                var addButton = listGroup.Q<LBSToolbarButton>("AddButton");
                if(addButton == null)
                { NotFoundErrorLog($"AddButton for {typeof(T).Name}"); return; }

                addButton.clicked += () =>
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < options.Count; i++)
                    {
                        int index = i; // Capture index for closure
                        menu.AddItem(new GUIContent(options[i].Name), false, () =>
                        {
                            try
                            {
                                if (typeof(T) == typeof(LBSModule))
                                    AddModule(options[index]);
                                else if (typeof(T) == typeof(LBSBehaviour))
                                    AddBehaviour(options[index]);
                                else if (typeof(T) == typeof(LBSAssistant))
                                    AddAssistant(options[index]);
                                else if (typeof(T) == typeof(LBSGeneratorRule))
                                    AddGeneratorRule(options[index]);
                                else
                                    Debug.LogWarning($"Unsupported type for addition: {typeof(T).Name}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }

                            EditorUtility.SetDirty(Template);
                            listGroup.SetItemsSource(items);
                            listGroup.Rebuild();
                        });
                    }
                    menu.ShowAsContext();
                };

                var removeButton = listGroup.Q<LBSToolbarButton>("RemoveButton");
                if(removeButton == null)
                { NotFoundErrorLog($"RemoveButton for {typeof(T).Name}"); return; }

                removeButton.clicked += () =>
                {
                    int selectedIndex = listGroup.SelectedIndex;
                    if (selectedIndex < 0 || selectedIndex >= items.Count)
                        return;
                    Undo.RecordObject(Template, $"Remove {typeof(T).Name}");
                    if (typeof(T) == typeof(LBSModule))
                        Template.layer.RemoveModule(items[selectedIndex] as LBSModule);
                    else if (typeof(T) == typeof(LBSBehaviour))
                        Template.layer.RemoveBehaviour(items[selectedIndex] as LBSBehaviour);
                    else if (typeof(T) == typeof(LBSAssistant))
                        Template.layer.RemoveAssistant(items[selectedIndex] as LBSAssistant);
                    else if (typeof(T) == typeof(LBSGeneratorRule))
                        Template.layer.RemoveGeneratorRule(items[selectedIndex] as LBSGeneratorRule);
                    else
                        Debug.LogWarning($"Unsupported type for removal: {typeof(T).Name}");

                    EditorUtility.SetDirty(Template);
                    listGroup.SetItemsSource(items);
                    listGroup.Rebuild();
                };
            }
        
            void NotFoundErrorLog(string name)
            {
                Debug.LogError($"[LayerTemplateEditor]: {Template.layer.ID} couldn't find the {name} visual element.");
            }
        }

        private static void EnsureCaches()
        {
            if (s_behaviourOptions != null) return; // already cached

            // Cache derived types safely
            s_moduleOptions = typeof(LBSModule).GetDerivedTypes().ToList();
            s_behaviourOptions = typeof(LBSBehaviour).GetDerivedTypes().ToList();
            s_assistantOptions = typeof(LBSAssistant).GetDerivedTypes().ToList();
            s_ruleOptions = typeof(LBSGeneratorRule).GetDerivedTypes().ToList();

            s_moduleNames = s_moduleOptions.Select(t => t.Name).ToArray();
            s_behaviourNames = s_behaviourOptions.Select(t => t.Name).ToArray();
            s_assistantNames = s_assistantOptions.Select(t => t.Name).ToArray();
            s_ruleNames = s_ruleOptions.Select(t => t.Name).ToArray();

            // Load icons (AssetDatabase is editor-only and cheap here)
            try
            {
                s_behaviourIcon = AssetMacro.LoadAssetByGuid<VectorImage>(DefaultBehaviorIcon);
            }
            catch
            {
                s_behaviourIcon = null;
            }

            try
            {
                s_assistantIcon = AssetMacro.LoadAssetByGuid<VectorImage>(DefaultAssistantIcon);
            }
            catch
            {
                s_assistantIcon = null;
            }
        }
        #endregion

        #region Inspector GUI
        private void DrawDebugInspector()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            bool debugView = EditorGUILayout.Toggle("Debug View", Template.DebugView);

            if (EditorGUI.EndChangeCheck())
            {
                Template.DebugView = debugView;
                EditorUtility.SetDirty(Template);

                ActiveEditorTracker.sharedTracker.ForceRebuild();
                return;
            }

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

            DrawAddSection("Modules", ref _moduleIndex, s_moduleNames, s_moduleOptions, AddModule);
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
        private void AddModule(Type type)
        {
            /*
            if (type == null) return;
            if (Activator.CreateInstance(type, AssetMacro.GetGuidFromAsset(s_moduleIcon), type.Name, Color.clear) is LBSModule instance)
            {
                Template.layer.AddModule(instance);
            }
            else
            {
                Debug.LogError($"Failed to create instance of module type: {type.Name}");
            }//*/
        }

        private void AddBehaviour(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type, AssetMacro.GetGuidFromAsset(s_behaviourIcon), type.Name, Color.clear) is LBSBehaviour instance)
            {
                Template.layer.AddBehaviour(instance);
            }
            else
            {
                Debug.LogError($"Failed to create instance of behaviour type: {type.Name}");
            }
        }

        private void AddAssistant(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type, AssetMacro.GetGuidFromAsset(s_assistantIcon), type.Name, Color.clear) is LBSAssistant instance)
            {
                Template.layer.AddAssistant(instance);
            }
            else
            {
                Debug.LogError($"Failed to create instance of assistant type: {type.Name}");
            }
        }

        private void AddGeneratorRule(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type) is LBSGeneratorRule instance)
            {
                Template.layer.AddGeneratorRule(instance);
            }
            else
            {
                Debug.LogError($"Failed to create instance of gen rule type: {type.Name}");
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
                layer.SetID("Interior");
                layer.Name = "Layer Interior";
                layer.iconGuid = "8c78cf0f5376fd846a188536ff3497ae";

                AddObject<SchemaBehaviour>(layer, "Schema behaviour", AssetMacro.GetGuidFromAsset(s_behaviourIcon), LBSSettings.Instance.view.behavioursColor);
                AddObject<HillClimbingAssistant>(layer, "HillClimbing", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);

                AddObject<SchemaRuleGenerator>(layer, "Schema Rule Generator", "", Color.clear);
                AddObject<SchemaRuleGeneratorExterior>(layer, "Schema Rule Generator Exterior", "", Color.clear);

                layer.TileSize = new Vector2Int(2, 2);
            });
        }

        private void ExteriorConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.SetID("Exterior");
                layer.Name = "Layer Exterior";
                layer.iconGuid = "02a644759487ae249bc3a20d019c8745";

                AddObject<ExteriorBehaviour>(layer, "Exterior behaviour", AssetMacro.GetGuidFromAsset(s_behaviourIcon), LBSSettings.Instance.view.behavioursColor);
                AddObject<AssistantWFC>(layer, "Assistant WFC", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);
                AddObject<ExteriorRuleGenerator>(layer, "Exterior Rule Generator", "", Color.clear);

                layer.TileSize = new Vector2Int(2, 2);
            });
        }

        private void PopulationConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.SetID("Population");
                layer.Name = "Layer Population";
                layer.iconGuid = "48f2011efc0f7b2449db9f824c895d9d";

                AddObject<PopulationBehaviour>(layer, "Population Behavior", AssetMacro.GetGuidFromAsset(s_behaviourIcon), LBSSettings.Instance.view.behavioursColor);
                AddObject<AssistantMapElite>(layer, "Map Elite - Genetic Algorithm", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);
                AddObject<PopulationRuleGenerator>(layer, "Population Rule Generator", "", Color.clear);

                layer.TileSize = new Vector2Int(2, 2);
            });
        }

        private void QuestConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.SetID("Quest");
                layer.Name = "Layer Quest";
                layer.iconGuid = "9fc8ac6f82a8b39458c73185d378ffbf";

                AddObject<QuestBehaviour>(layer, "Quest Behavior", AssetMacro.GetGuidFromAsset(s_behaviourIcon), LBSSettings.Instance.view.behavioursColor);
                AddObject<GrammarAssistant>(layer, "Grammar Assistant", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);
                AddObject<GrammarAssistant>(layer, "Quest Assistant", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);
                AddObject<QuestRuleGenerator>(layer, "Quest Rule Generator", "", Color.clear);

                layer.TileSize = new Vector2Int(2, 2);
            });
        }

        private void SimulationConstruct()
        {
            ApplyPreset(layer =>
            {
                layer.SetID("Simulation");
                layer.Name = "Layer Simulation";
                layer.iconGuid = "13f64883312513a41adeb7dec75a3a5f";

                AddObject<SimulationBehaviour>(layer, "Simulation Behaviour", AssetMacro.GetGuidFromAsset(s_behaviourIcon), LBSSettings.Instance.view.behavioursColor);
                AddObject<SimulationAssistant>(layer, "Simulation Assistant", AssetMacro.GetGuidFromAsset(s_assistantIcon), LBSSettings.Instance.view.assistantColor);
                AddObject<SimulationRuleGenerator>(layer, "Simulation Rule Generator", "", Color.clear);

                layer.TileSize = new Vector2Int(2, 2);
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