using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Editor.UI.CustomComponents;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.MapTools.Editor.Templates
{
    [LBSCustomEditor("Layer template", typeof(LayerTemplate))]
    [CustomEditor(typeof(LayerTemplate))]
    public class LayerTemplateEditor : UnityEditor.Editor
    {
        #region Fields
        [SerializeField]
        private VisualTreeAsset _layerTemplateInspector;
        private VisualElement _root;

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

        private static Dictionary<Type, MonoScript> s_typeScriptCache = new Dictionary<Type, MonoScript>();

        private List<string> _unresolvedRequirements = new List<string>();
        private List<LBSBaseListGroup> _listGroups = new List<LBSBaseListGroup>();
        private Dictionary<LBSBaseListGroup, int> _prevSelectedIndexes = new Dictionary<LBSBaseListGroup, int>();
        private LBSCustomListView _warningList;
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
            Repaint();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();

            // ------------ DEBUG INSPECTOR ------------
            if (Template.DebugView)
            {
                _root.Add(new IMGUIContainer(DrawDebugInspector));
                return _root;
            }

            // ---------- SIMPLIFIED INSPECTOR ---------
            if (_layerTemplateInspector != null)
            {
                _root.Add(_layerTemplateInspector.CloneTree());
            }

            // Debug view toggle
            var debugToggle = _root.Q<LBSCustomToggle>("DebugViewToggle");
            DebugToggleSetup();

            // Template name field
            var nameField = _root.Q<LBSCustomTextField>("TemplateNameField");
            TextFieldSetup(nameField, Template.Name, Template.SetName, "Change Template Name");

            // Sorting order field
            var sortingField = _root.Q<LBSCustomIntField>("SortingOrderField");
            SortingFieldSetup();

            // Icon field
            var iconImage = _root.Q<VisualElement>("IconImage");
            var iconField = _root.Q<LBSCustomObjectField>("IconObjectField");
            IconFieldSetup();

            // Default name field
            var defaultNameField = _root.Q<LBSCustomTextField>("DefaultNameField");
            TextFieldSetup(
                defaultNameField, 
                Template.layer.Name, 
                (value) => { Template.layer.Name = value; }, 
                "Change Layer's Default Name");

            // ID field
            var idField = _root.Q<LBSCustomTextField>("IdField");
            TextFieldSetup(idField, Template.layer.ID, Template.layer.SetID, "Change Layer ID");

            // Tile size field
            var tileSizeField = _root.Q<LBSCustomVector2IntField>("TileSizeField");
            TileSizeFieldSetup();

            // Floor count field
            var floorCountField = _root.Q<LBSCustomUnsignedIntegerField>("FloorCountField");
            FloorCountFieldSetup();

            // Modules list
            var modulesListGroup = _root.Q<LBSBaseListGroup>("ModulesListGroup");
            ListGroupSetup(modulesListGroup, s_moduleOptions, Template.layer.FirstModules, (element, index) =>
            {
                var mod = Template.layer.FirstModules[index];

                var label = element.Q<LBSCustomLabel>("textL");
                label.text = mod?.ID ?? "Null Module";
                label.style.color = mod != null ? Color.white : Color.gray;
                if (ItemContentSetup(element, mod, $"Module {mod?.ID ?? "NULL"}") > 0)
                    label.text += " (...)";
            });
            _listGroups.Add(modulesListGroup);

            // Behaviours list
            var behavioursListGroup = _root.Q<LBSBaseListGroup>("BehavioursListGroup");
            ListGroupSetup(behavioursListGroup, s_behaviourOptions, Template.layer.FirstBehaviours, (element, index) =>
            {
                var beh = Template.layer.FirstBehaviours[index];

                var label = element.Q<LBSCustomLabel>("textL");
                label.text = beh?.Name ?? "Null Behaviour";
                label.style.color = beh?.ColorTint ?? Color.gray;
                if (ItemContentSetup(element, beh, $"Behaviour {beh?.Name ?? "NULL"}") > 0)
                    label.text += " (...)";
            });
            _listGroups.Add(behavioursListGroup);

            // Assistants list
            var assistantsListGroup = _root.Q<LBSBaseListGroup>("AssistantsListGroup");
            ListGroupSetup(assistantsListGroup, s_assistantOptions, Template.layer.FirstAssistants, (element, index) =>
            {
                var ass = Template.layer.FirstAssistants[index];

                var label = element.Q<LBSCustomLabel>("textL");
                label.text = ass?.Name ?? "Null Assistant";
                label.style.color = ass?.ColorTint ?? Color.gray;
                if (ItemContentSetup(element, ass, $"Behaviour {ass?.Name ?? "NULL"}") > 0)
                    label.text += " (...)";
            });
            _listGroups.Add(assistantsListGroup);

            // Generator rules list
            var rulesListGroup = _root.Q<LBSBaseListGroup>("GeneratorRulesListGroup");
            ListGroupSetup(rulesListGroup, s_ruleOptions, Template.layer.FirstGeneratorRules, (element, index) =>
            {
                var rul = Template.layer.FirstGeneratorRules[index];

                var label = element.Q<LBSCustomLabel>("textL");
                label.text = rul?.GetType().Name ?? "Null Rule";
                if (ItemContentSetup(element, rul, $"Behaviour {label.text}") > 0)
                    label.text += " (...)";
            });
            _listGroups.Add(rulesListGroup);

            // Warning list
            _warningList = _root.Q<LBSCustomListView>("WarningListView");
            WarningListSetup();

            return _root;


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
                if (iconImage == null || iconField == null) 
                { NotFoundErrorLog("IconImage"); return; }

                // Set initial icon if it exists
                if (!string.IsNullOrEmpty(Template.layer.iconGuid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(Template.layer.iconGuid);
                    VectorImage icon = AssetDatabase.LoadAssetAtPath<VectorImage>(path);
                    if (icon != null)
                    {
                        iconImage.style.backgroundImage = new StyleBackground(icon);
                        iconField.value = icon;
                    }
                }

                // Field behaviour
                iconField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == null || evt.newValue is not VectorImage) return;
                    var newImage = evt.newValue as VectorImage;

                    // Load and set the icon
                    Undo.RecordObject(Template, "Change Icon");
                    iconImage.style.backgroundImage = new StyleBackground(newImage); 
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.newValue));
                    Template.layer.iconGuid = guid;
                    EditorUtility.SetDirty(target);
                });
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

            void FloorCountFieldSetup()
            {
                if (floorCountField == null)
                { NotFoundErrorLog("FloorCountField"); return; }
                floorCountField.value = (uint) Template.layer.FloorCount;
                floorCountField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(Template, "Change Floor Count");
                    Template.layer.ChangeFloorCount(evt.newValue);
                    EditorUtility.SetDirty(Template);
                });
            }

            void ListGroupSetup<T>(LBSBaseListGroup listGroup, List<Type> options, List<T> items, Action<VisualElement, int> bindItem)
            {
                if(listGroup == null)
                { NotFoundErrorLog($"ListGroup<{typeof(T).Name}>"); return; }

                // Wrap the provided bindItem so we can store the index on the visual element (userData)
                Action<VisualElement, int> wrappedBindItem = (element, index) =>
                {
                    // Guard: ensure element isn't null
                    if (element != null)
                        element.userData = index;
                    bindItem?.Invoke(element, index);
                };

                listGroup.BindListView(items, 
                    // Show content when clicked
                    (selected) =>
                    {
                        var sel = selected.FirstOrDefault();

                        int selIndex = listGroup.SelectedIndex;
                        if (selIndex < 0 || selIndex >= items.Count)
                            return;

                        // Set all items' content visibility.
                        foreach (var item in listGroup.Query<LBSCustomLabelItem>().ToList())
                        {
                            if (item == null) continue;

                            if(item.userData is int i && i == selIndex)
                            {
                                item.SetContentVisibility(true);
                            }
                            else
                            {
                                item.SetContentVisibility(false);
                            }
                        }
                    },
                    // Ping script on project window when double clicked
                    (chosen) =>
                    {
                        var item = chosen.First();
                        Type itemType = item.GetType();
                        MonoScript script = FindScriptForType(itemType);
                        if (script != null)
                            EditorGUIUtility.PingObject(script);
                        else
                            Debug.LogWarning($"[LayerTemplateEditor] No se encontró el script para el tipo: {itemType.FullName}");

                    }, () => new LBSCustomLabelItem(), wrappedBindItem);

                // "Add" button config
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
                            RebuildAllLists();
                        });
                    }
                    menu.ShowAsContext();
                };

                // "Remove" button config
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
                        Template.layer.RemoveModuleInAllFloors(items[selectedIndex] as LBSModule);
                    else if (typeof(T) == typeof(LBSBehaviour))
                        Template.layer.RemoveBehaviour(items[selectedIndex] as LBSBehaviour);
                    else if (typeof(T) == typeof(LBSAssistant))
                        Template.layer.RemoveAssistant(items[selectedIndex] as LBSAssistant);
                    else if (typeof(T) == typeof(LBSGeneratorRule))
                        Template.layer.RemoveGeneratorRule(items[selectedIndex] as LBSGeneratorRule);
                    else
                        Debug.LogWarning($"Unsupported type for removal: {typeof(T).Name}");

                    EditorUtility.SetDirty(Template);
                    listGroup.Rebuild();
                    RebuildWarningList();
                };
            }
        
            int ItemContentSetup(VisualElement element, object obj, string name)
            {
                var members = ShowOnLayerTemplateAttribute.GetMembers(obj);
                var content = element.Q<VisualElement>("Content");

                string debug = $"{name} contains {members.Length} members: ";
                foreach (var member in members)
                {
                    object val;

                    // Get value
                    if (member is FieldInfo field)
                    {
                        val = field.GetValue(obj);
                    }
                    else if (member is PropertyInfo property)
                    {
                        val = property.GetValue(obj);
                    }
                    else continue;

                    var type = val != null ? val.GetType() : typeof(object);
                    VisualElement visualField = null;

                    // Enum
                    if (type.IsEnum)
                    {
                        visualField = new LBSCustomEnumField(member.Name, (Enum)val);
                        visualField.RegisterCallback<ChangeEvent<Enum>>(evt =>
                        {
                            MemberChangeCallback(member, evt);
                        });
                        content.Add(visualField);
                        continue;
                    }
                    /*
                    // Bundle
                    if(type == typeof(Bundle))
                    {
                        visualField = new LBSCustomObjectField()
                        {
                            label = member.Name,
                            value = (UnityEngine.Object) val
                        };
                        visualField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>
                        {
                            MemberChangeCallback(member, evt);
                        });
                        content.Add(visualField);
                        continue;
                    }//*/

                    // Common types
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.String:
                            visualField = new LBSCustomTextField(member.Name) { value = (string) val };
                            visualField.RegisterCallback<ChangeEvent<string>>
                                (evt => { MemberChangeCallback(member, evt); });
                            break;
                        case TypeCode.Int32:
                            visualField = new LBSCustomIntField(member.Name) { value = (int) val };
                            visualField.RegisterCallback<ChangeEvent<int>>
                                (evt => { MemberChangeCallback(member, evt); });
                            break;
                        case TypeCode.Boolean:
                            visualField = new LBSCustomToggleField(member.Name) { value = (bool) val };
                            visualField.RegisterCallback<ChangeEvent<bool>>
                                (evt => { MemberChangeCallback(member, evt); });
                            break;
                        default:
                            visualField = new LBSCustomObjectField()
                            {
                                label = member.Name,
                                value = (UnityEngine.Object)val,
                                dataSourceType = type,
                                objectType = type
                            };
                            visualField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>{
                                MemberChangeCallback(member, evt);
                            });
                            break;
                    }

                    if(visualField != null)
                    { 
                        content.Add(visualField); 
                    }
                }
                return members.Count();


                // Sub-Methods
                void MemberChangeCallback<T>(object info, ChangeEvent<T> evt)
                {
                    var infoType = info.GetType();
                    if (infoType == typeof(FieldInfo))
                    {
                        FieldChangeCallback<T>(info as FieldInfo, evt);
                    }
                    else if (infoType == typeof(PropertyInfo))
                    {
                        PropertyChangeCallback<T>(info as PropertyInfo, evt);
                    }
                }
                void FieldChangeCallback<T>(FieldInfo field, ChangeEvent<T> evt)
                {
                    Undo.RecordObject(Template, $"Change {field.Name} of {name}");
                    field.SetValue(obj, evt.newValue);
                    EditorUtility.SetDirty(Template);
                }
                void PropertyChangeCallback<T>(PropertyInfo field, ChangeEvent<T> evt)
                {
                    Undo.RecordObject(Template, $"Change {field.Name} of {name}");
                    field.SetValue(obj, evt.newValue);
                    EditorUtility.SetDirty(Template);
                }
            }

            void WarningListSetup()
            {
                if (_warningList == null)
                { NotFoundErrorLog($"WarningListView"); return; }

                _unresolvedRequirements = GetUnresolvedRequirements();
                _warningList.itemsSource = _unresolvedRequirements;
                _warningList.makeItem = () => new WarningPanel();
                _warningList.bindItem = (element, index) =>
                {
                    var panel = element as WarningPanel;
                    if(index >= _unresolvedRequirements.Count)
                    {
                        panel.RemoveFromHierarchy();
                    }
                    panel.Text = index < _unresolvedRequirements.Count ? _unresolvedRequirements[index] : "Empty";
                };

                // Mostrar/ocultar según el conteo de elementos
                _warningList.style.display = _unresolvedRequirements.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            void NotFoundErrorLog(string name)
            {
                Debug.LogError($"[LayerTemplateEditor]: {Template.layer.ID} couldn't find the {name} visual element.");
            }
        
            void RebuildAllLists()
            {
                foreach (var group in _listGroups)
                    group.Rebuild();
                RebuildWarningList();
            }
            void RebuildWarningList()
            {
                if (_warningList == null) return;
                _unresolvedRequirements = GetUnresolvedRequirements();
                _warningList.RefreshItems();
                _warningList.Rebuild();
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

        private static MonoScript FindScriptForType(Type type)
        {
            if (type == null) return null;
            if (s_typeScriptCache.TryGetValue(type, out var cached)) return cached;

            // Busca scripts cuyo nombre de archivo coincida con el nombre del tipo
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != type.Name) continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                var cls = script.GetClass();
                if (cls == type || script.name == type.Name)
                {
                    s_typeScriptCache[type] = script;
                    return script;
                }
            }

            // Fallback: buscar por clase dentro de todos los scripts
            guids = AssetDatabase.FindAssets("t:script");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                var cls = script.GetClass();
                if (cls == type)
                {
                    s_typeScriptCache[type] = script;
                    return script;
                }
            }

            s_typeScriptCache[type] = null;
            return null;
        }
        
        private List<string> GetUnresolvedRequirements()
        {
            List<string> unresolved = new List<string>();

            foreach (var obj in Template.layer.FirstModules)
            {
                unresolved.AddRange(GetRequieredModules(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredBehaviours(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredAssistants(obj, obj?.GetType().Name ?? "Unknown"));
            }
            foreach (var obj in Template.layer.FirstBehaviours)
            {
                unresolved.AddRange(GetRequieredModules(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredBehaviours(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredAssistants(obj, obj?.GetType().Name ?? "Unknown"));
            }
            foreach (var obj in Template.layer.FirstAssistants)
            {
                unresolved.AddRange(GetRequieredModules(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredBehaviours(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredAssistants(obj, obj?.GetType().Name ?? "Unknown"));
            }
            foreach (var obj in Template.layer.FirstGeneratorRules)
            {
                unresolved.AddRange(GetRequieredModules(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredBehaviours(obj, obj?.GetType().Name ?? "Unknown"));
                unresolved.AddRange(GetRequieredAssistants(obj, obj?.GetType().Name ?? "Unknown"));
            }
            return unresolved;

            List<string> GetRequieredModules(object obj, string name)
            {
                List<string> unresolved = new List<string>();

                // Check modules
                if (obj == null) return unresolved;
                var req = obj.GetType().GetCustomAttributes(typeof(RequieredModuleAttribute), true).FirstOrDefault() as RequieredModuleAttribute;
                if (req == null) return unresolved;

                foreach (var type in req.types)
                {
                    if (!Template.layer.FirstModules.Any(m => m != null && m.GetType() == type))
                    {
                        unresolved.Add($"'{name}' requires missing module: {type.Name}");
                    }
                }
                return unresolved;
            }
            List<string> GetRequieredBehaviours(object obj, string name)
            {
                List<string> unresolved = new List<string>();

                // Check behaviours
                if (obj == null) return unresolved;
                var req = obj.GetType().GetCustomAttributes(typeof(RequieredBehaviourAttribute), true).FirstOrDefault() as RequieredBehaviourAttribute;
                if (req == null) return unresolved;

                foreach (var type in req.types)
                {
                    if (!Template.layer.FirstBehaviours.Any(b => b != null && b.GetType() == type))
                    {
                        unresolved.Add($"'{name}' requires missing behaviour: {type.Name}");
                    }
                }
                return unresolved;
            }
            List<string> GetRequieredAssistants(object obj, string name)
            {
                List<string> unresolved = new List<string>();

                // Check assistants
                if (obj == null) return unresolved;
                var req = obj.GetType().GetCustomAttributes(typeof(RequieredAssistantAttribute), true).FirstOrDefault() as RequieredAssistantAttribute;
                if (req == null) return unresolved;

                foreach (var type in req.types)
                {
                    if (!Template.layer.FirstAssistants.Any(a => a != null && a.GetType() == type))
                    {
                        unresolved.Add($"'{name}' requires missing assistant: {type.Name}");
                    }
                }
                return unresolved;
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
            if (type == null) return;
            if (Activator.CreateInstance(type) is LBSModule instance)
            {
                for(int i = 0; i < Template.layer.FloorCount; i++)
                {
                    Template.layer.AddModule(instance.Clone() as LBSModule, i);
                }
            }
            else
            {
                Debug.LogError($"Failed to create instance of module type: {type.Name}");
            }
        }

        private void AddBehaviour(Type type)
        {
            if (type == null) return;
            if (Activator.CreateInstance(type, AssetMacro.GetGuidFromAsset(s_behaviourIcon), type.Name, LBSSettings.Instance.view.behavioursColor) is LBSBehaviour instance)
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
            if (Activator.CreateInstance(type, AssetMacro.GetGuidFromAsset(s_assistantIcon), type.Name, LBSSettings.Instance.view.assistantColor) is LBSAssistant instance)
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