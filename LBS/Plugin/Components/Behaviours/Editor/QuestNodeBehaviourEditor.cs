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
using ISILab.LBS.Plugin.Components.Data;
using LBS;
using LBS.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    
    [LBSCustomEditor("QuestFlowBehaviour", typeof(QuestNodeBehaviour))]
    public class QuestNodeBehaviourEditor : LBSCustomEditor, IToolProvider
    {
        #region FIELDS
        private QuestNodeBehaviour nodeBehavior;

        private const float ActionBorderThickness = 1f;
        private const float BackgroundOpacity = 0.25f;
        
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
        private VisualElement fieldsVisualElements;
        
        private VisualElement _actionColor;
        private VisualElement _actionIcon;
        
        private PickerVector2Int _targetPosition;
        private RectField _area;
        private VisualElement _selectedNodePanel;

        private VisualElement _onEventCompleteVe;
        private LBSCustomEventHooker _hooker;
 
        #endregion
        
        #region CONSTRUCTORS
        public QuestNodeBehaviourEditor(object target) : base(target)
        {
            SetInfo(target);
            CreateVisualElement();
            nodeBehavior.Graph.Reselect();
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            nodeBehavior = paramTarget as QuestNodeBehaviour;
            if (nodeBehavior == null) return;

            ActionExtensions.AddUnique(ref nodeBehavior.OnNodeDataChanged, OnSelectNode);
            ActionExtensions.AddUnique(ref nodeBehavior.Graph.OnNodeSelected, OnSelectNode);
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
            
            fieldsVisualElements = this.Q<VisualElement>("InstancedContent");
            
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
                
                QuestNodeData actionData = nodeBehavior.SelectedNodeData;
                if (actionData is null) return;
                
                pickerManipulator.PickTriggerPosition = true;
                pickerManipulator.ActiveData = actionData;
                
                if(pickerManipulator.ActiveData == null) return;
                
                pickerManipulator.OnPositionPicked = (pos) =>
                {
                    actionData.Area = new Rect(pos.x,pos.y, actionData.Area.width,actionData.Area.height);
                    
                    // Refresh UI
                    _targetPosition.SetTarget(pos);
                };

            };
            #endregion
            
            _area = this.Q<RectField>("Area");
            _area.RegisterValueChangedCallback(evt => SetNodeDataArea(evt.newValue));

            _onEventCompleteVe = this.Q<VisualElement>("EventComplete");
            _hooker = this.Q<LBSCustomEventHooker>("EventHooker");
            _hooker.EventType = LBSEventType.Complete;
            _hooker.AllowChangeTriggerEnable = false;
            // cant change complete mode
            _hooker.Selector.RegisterValueChangedCallback(evt =>
            {
                QuestNodeData data = nodeBehavior.SelectedNodeData;
                if (data is null) return;
                _hooker.Hooker = data.EventHooker;
            });
            _hooker.Selector.allowSceneObjects = true;

                     
            // No node when instanced
            _noNodeSelectedPanel.style.display = DisplayStyle.Flex;

            _hooker.RefreshMethodList();
            
            return this;
        }
        

        private void SetNodeDataArea(Rect newValue)
        {
            QuestNodeData nodeData = nodeBehavior.SelectedNodeData;
            if (nodeData is null) return;
            
            newValue.x = Mathf.Round(newValue.x);
            newValue.y = Mathf.Round(newValue.y);
            newValue.height = MathF.Abs(newValue.height);
            newValue.width = MathF.Abs(newValue.width);
            
            nodeData.Area = newValue;
            DrawManager.Instance.RedrawLayer(nodeBehavior.OwnerLayer);
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
            toolkit.ActivateTool(toolPicker, nodeBehavior?.OwnerLayer, target);

            // context exclusive from the Node Panel
            VisualElement toolButton = toolkit.GetToolButton(typeof(QuestPicker));
            toolButton.SetEnabled(false);

          
        }

        private void OnSelectNode(GraphNode graphNode)
        {
            QuestNode node = graphNode as QuestNode;
            bool validNode = node != null;

            if (!validNode)
            {
                style.display = DisplayStyle.None;
                return;
            }
            else
            {
                style.display = DisplayStyle.Flex;
            }


            fieldsVisualElements.Clear();

            _noNodeSelectedPanel.style.display = validNode ? DisplayStyle.None : DisplayStyle.Flex;  
            _nodePanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _actionPanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _targetPosition.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _selectedNodePanel.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;
            _onEventCompleteVe.style.display = validNode ? DisplayStyle.Flex : DisplayStyle.None;

            if (!validNode) return;

            // on complete display
            _hooker.Hooker = (nodeBehavior.SelectedNodeData?.EventHooker);

            // set default data display
            _paramActionLabel.text = node.TerminalID;
            _nodeIDLabel.text = node.ID;
            SetBaseNodeData(node.Data);

            // specific visual elements display per fields
            foreach (var terminalField in node.Data.Fields)
            {
                var terminalID = terminalField.name;
                // find a visualelement class with the terminalID as attribute and instance it
               // VisualElement ve = Activator.CreateInstance() as VisualElement;
                VisualElement ve = new VisualElement();
                fieldsVisualElements.Add(ve);
            }

        }

        /// <summary>
        /// Sets the trigger position and size (Rect) of the node data.
        /// Adds fields by type
        /// </summary>
        /// <param name="data"></param>
        private void SetBaseNodeData(QuestNodeData data)
        {
            Color terminalColor = data.Terminal.color;
            Color backgroundColor = terminalColor;
            backgroundColor.a = BackgroundOpacity;
            _actionColor.SetBackgroundColor(backgroundColor);
            
            _actionIcon.style.unityBackgroundImageTintColor = terminalColor;
            _actionColor.SetBorder(terminalColor, ActionBorderThickness);
            
            _area.value = data.Area;
            
        }
        

        #endregion
        
    }
}