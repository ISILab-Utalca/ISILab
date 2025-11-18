using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using LBS;
using LBS.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ISILab.LBS.VisualElements
{
    
    [LBSCustomEditor("QuestFlowBehaviour", typeof(QuestNodeBehaviour))]
    public class QuestNodeBehaviourEditor : LBSCustomEditor, IToolProvider
    {
        #region FIELDS
        private QuestNodeBehaviour _behaviour;
        
        private const float ActionBorderThickness = 1f;
        private const float BackgroundOpacity = 0.25f;
        
        private static readonly Dictionary<Type, Type> TypeToPanelMap = new()
        {
            { typeof(DataExplore), typeof(NodeEditorExplore) },
            { typeof(DataKill), typeof(NodeEditorKill) },
            { typeof(DataStealth), typeof(NodeEditorStealth) },
            { typeof(DataTake), typeof(NodeEditorTake) },
            { typeof(DataRead), typeof(NodeEditorRead) },
            { typeof(DataExchange), typeof(NodeEditorExchange) },
            { typeof(DataGive), typeof(NodeEditorGive) },
            { typeof(DataReport), typeof(NodeEditorReport) },
            { typeof(DataGather), typeof(NodeEditorGather) },
            { typeof(DataSpy), typeof(NodeEditorSpy) },
            { typeof(DataCapture), typeof(NodeEditorCapture) },
            { typeof(DataListen), typeof(NodeEditorListen) }
        };
        
        private List<(GameObject, Component, MethodInfo)> _availableMethods = new();
        #endregion
        
        #region VIEW FIELDS
        /// <summary>
        /// Displays the action string
        /// </summary>
        private Label _paramActionLabel;
        /// <summary>
        /// To identify which node has been clicked 
        /// </summary>
        private Label _nodeIDLabel;
        
        private VisualElement _nodePanel;
        private VisualElement _actionPanel;
        private VisualElement _noNodeSelectedPanel;
        private VisualElement _instancedContent;
        
        private VisualElement _actionColor;
        private VisualElement _actionIcon;
        
        private PickerVector2Int _targetPosition;
        private RectField _area;
        private VisualElement _selectedNodePanel;
        private LBSCustomObjectField _gameObjectSelector;
        private IMGUIContainer _imguiContainer;
        
        private ListView _selectedMethodsList;
        private ListView _availableMethodsList;
        private VisualElement _onEventCompleteVe;

        #endregion
        
        #region CONSTRUCTORS
        public QuestNodeBehaviourEditor(object target) : base(target)
        {
            SetInfo(target);
            CreateVisualElement();
            OnSelectNode(_behaviour.Graph.SelectedGraphNode);
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            _behaviour = paramTarget as QuestNodeBehaviour;
            if (_behaviour == null) return;
            _behaviour.Graph!.OnGraphNodeSelected += OnSelectNode;
            DrawManager.Instance.RedrawLayer(_behaviour.OwnerLayer);

        }
        protected sealed override VisualElement CreateVisualElement()
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestNodeBehaviourEditor");
            visualTree.CloneTree(this);
            
            #region Get VisualElements from UXML
            _nodePanel = this.Q<VisualElement>("ID");
            _actionPanel = this.Q<VisualElement>("Action");
            _noNodeSelectedPanel = this.Q<VisualElement>("NoNodeSelectedPanel");
            _selectedNodePanel = this.Q<VisualElement>("NodeSelectedPanel");
            
            _instancedContent = this.Q<VisualElement>("InstancedContent");
            
            _actionColor = this.Q<VisualElement>("ActionColor");
            _actionIcon = this.Q<VisualElement>("ActionIcon");
            
            _paramActionLabel = this.Q<Label>("ParamAction");
            _nodeIDLabel = this.Q<Label>("ParamID");
            #endregion
            
            #region Picker Position in Graph
            _targetPosition = this.Q<PickerVector2Int>("TargetPosition");
            _targetPosition.SetInfo("Trigger Position", "The position of the trigger in the graph.");
            _targetPosition.DisplayVectorField(false);
            
            _targetPosition._onClicked = () =>
            {
                if (ToolKit.Instance.GetActiveManipulatorInstance() is not QuestPicker pickerManipulator) return;
                
                BaseQuestNodeData nodeData = _behaviour.Graph.GetNodeData();
                if (nodeData is null) return;
                
                pickerManipulator.PickTriggerPosition = true;
                pickerManipulator.ActiveData = nodeData;
                
                if(pickerManipulator.ActiveData == null) return;
                
                pickerManipulator.OnPositionPicked = (pos) =>
                {
                    nodeData.Area = new Rect(pos.x,pos.y, nodeData.Area.width,nodeData.Area.height);
                    
                    // Refresh UI
                    _targetPosition.SetTarget(pos);
                };

            };
            #endregion
            
            _area = this.Q<RectField>("Area");
            _area.RegisterValueChangedCallback(evt => SetNodeDataArea(evt.newValue));

            _onEventCompleteVe = this.Q<VisualElement>("EventComplete");
            _gameObjectSelector = this.Q<LBSCustomObjectField>("GameObjectSelector");
            _gameObjectSelector.RegisterValueChangedCallback(evt =>
            {
                BaseQuestNodeData data = GetSelectedNodeData();
                if (data is null) return;
                data.Target = evt.newValue as GameObject;
                RefreshMethodList();
            });
            _gameObjectSelector.allowSceneObjects = true;
          
            _availableMethodsList = this.Q<ListView>("AvailableMethodsList");
            _selectedMethodsList = this.Q<ListView>("SelectedMethodsList");
            
            // No node when instanced
            _noNodeSelectedPanel.style.display = DisplayStyle.Flex;    
            
            RefreshMethodList();
            
            return this;
        }
        
        private void RefreshMethodList()
        {
            List<(GameObject, Component, MethodInfo)> selectedMethods = new();

            BaseQuestNodeData nodeData = GetSelectedNodeData();
            GameObject gameObject = nodeData?.Target;

            // reset on refresh
            _availableMethodsList.itemsSource = null;
            _selectedMethodsList.itemsSource = null;
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();
            
            if (!gameObject) return;
            
            #region Available Methods

            _availableMethods.Clear();

            foreach (MonoBehaviour comp in gameObject.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;

                foreach (MethodInfo method in comp.GetType().GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (method.ReturnType == typeof(void) && !method.GetParameters().Any())
                        _availableMethods.Add((gameObject, comp, method));
                }
            }

            _availableMethodsList.itemsSource = _availableMethods;
            _availableMethodsList.makeItem = () => new QuestMethodVisualElement();
            _availableMethodsList.bindItem = (element, i) =>
            {
                GetRegisteredMethods(nodeData, selectedMethods);

                if (i < 0 || i >= _availableMethods.Count)
                    return;

                var availableMethod = _availableMethods[i];
                QuestMethodVisualElement vm = (QuestMethodVisualElement)element;

                vm.SetEnabled(!selectedMethods.Contains(availableMethod));
                vm.AddListener(availableMethod, nodeData);
                vm.Q<Button>().clicked += () => RefreshMethodList();
            };

            #endregion
            
            #region Selected Methods

            selectedMethods.Clear();
            GetRegisteredMethods(nodeData, selectedMethods);

            _selectedMethodsList.itemsSource = selectedMethods;
            _selectedMethodsList.makeItem = () => new QuestMethodVisualElement();
            _selectedMethodsList.bindItem = (element, i) =>
            {
                if (i < 0 || i >= selectedMethods.Count)
                    return;

                QuestMethodVisualElement vm = (QuestMethodVisualElement)element;
                vm.RemoveListener(selectedMethods[i], nodeData);
                vm.Q<Button>().clicked += () => RefreshMethodList();
            };

            #endregion
            
            // rebuild both at the end
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();
        }


        private static void GetRegisteredMethods(BaseQuestNodeData nodeData, List<(GameObject, Component, MethodInfo)> selectedMethods)
        {
            selectedMethods.Clear();
            foreach (KeyValuePair<UnityActionStored, UnityAction> entry in nodeData.RegisteredListeners)
            {
                GameObject go = nodeData.Target;
                if(go is null || go.GetInstanceID() != entry.Key.gameObjectID)
                {
                    continue;
                }
                
                foreach (MonoBehaviour comp in go.GetComponents<MonoBehaviour>())
                {
                    if (comp == null || comp.GetType().Name != entry.Key.componentName)
                    {
                        continue;
                    }
                    MethodInfo method = comp.GetType().GetMethod(entry.Key.methodName);
                    selectedMethods.Add((go, comp, method));
                }
            }
        }

        private BaseQuestNodeData GetSelectedNodeData()
        {
            QuestNode node = _behaviour.Graph.SelectedGraphNode as QuestNode;
            BaseQuestNodeData nodeData = node?.NodeData;
            return nodeData;
        }


        private void SetNodeDataArea(Rect newValue)
        {
            BaseQuestNodeData nodeData = GetSelectedNodeData();
            if (nodeData is null) return;
            
            newValue.x = Mathf.Round(newValue.x);
            newValue.y = Mathf.Round(newValue.y);
            newValue.height = MathF.Abs(newValue.height);
            newValue.width = MathF.Abs(newValue.width);
            
            nodeData.Area = newValue;
            DrawManager.Instance.RedrawLayer(_behaviour.OwnerLayer);
        }

        /// <summary>
        /// By default the quest picker tool sets the Trigger Position of the quest node
        /// </summary>
        /// <param name="toolkit"></param>
        public void SetTools(ToolKit toolkit)
        { 
            QuestPicker questPicker = new();
            LBSTool toolPicker = new(questPicker);
            toolPicker.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            toolkit.ActivateTool(toolPicker,_behaviour?.OwnerLayer, target);
        }

        private void OnSelectNode(GraphNode graphNode)
        {
            QuestNode node = graphNode as QuestNode;
            bool validNode = node?.NodeData != null;
            
            _noNodeSelectedPanel.style.display = validNode ? DisplayStyle.None : DisplayStyle.Flex;  
            _nodePanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _actionPanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _targetPosition.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _selectedNodePanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _onEventCompleteVe.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            
            // on complete related
            _gameObjectSelector.value = GetSelectedNodeData()?.Target;
            RefreshMethodList();
                
            _instancedContent.Clear();
            
            if (!validNode )  return;
            
            _paramActionLabel.text = node.QuestAction;
            _nodeIDLabel.text = node.ID;

            Type dataType = node.NodeData.GetType();
            
            if (TypeToPanelMap.TryGetValue(dataType, out Type visualElementType))
            {
                if (visualElementType == null) return;
                if (Activator.CreateInstance(visualElementType) is not NodeEditor instance) return;
                
                _instancedContent.Add(instance);
                instance.SetNodeData(node.NodeData); // bindings per editor type
                SetBaseDataValues(node.NodeData); // for trigger position and size
            }
            
            // if not in the dictionary just set the default data: For example "GoTo" action
            else
            {
                SetBaseDataValues(node.NodeData);
            }
        }
        
        private void SetBaseDataValues(BaseQuestNodeData data)
        {
            Color backgroundColor = data.Color;
            backgroundColor.a = BackgroundOpacity;
            _actionColor.SetBackgroundColor(backgroundColor);
            
            _actionIcon.style.unityBackgroundImageTintColor = data.Color;
            _actionColor.SetBorder(data.Color,ActionBorderThickness);
            
            _area.value = data.Area;
            
        }
        

        #endregion
        
    }
}