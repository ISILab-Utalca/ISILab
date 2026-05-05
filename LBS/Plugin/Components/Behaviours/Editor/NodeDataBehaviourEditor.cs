using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using LBS;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    
    [LBSCustomEditor("NodeDataBehaviour", typeof(NodeDataBehaviour))]
    public class NodeDataBehaviourEditor : LBSCustomEditor, IToolProvider
    {
        #region CONSTS

        private static readonly Dictionary<Type, Type> FieldTypeToVisualElement = new()
        {

            { typeof(GrammarEventHook), typeof(FieldEventHook)},
            { typeof(GrammarArea), typeof(FieldArea)},
            { typeof(GrammarFloat), typeof(FieldFloat) },
            { typeof(GrammarString), typeof(FieldString) },
            { typeof(GrammarObject), typeof(FieldReferenceGraph) },
            { typeof(GrammarObjectType), typeof(FieldReferenceType) },
            { typeof(GrammarInt), typeof(FieldInt) }
        };

        #endregion

        #region FIELDS
        private NodeDataBehaviour behaviour;

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

        private static VisualTreeAsset visualTree;

        #endregion

        #region CONSTRUCTORS
        public NodeDataBehaviourEditor(object target) : base(target)
        {
            SetInfo(target);
            CreateVisualElement();
            behaviour.Graph.Reselect();
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as NodeDataBehaviour;
            if (behaviour == null) return;

            ActionExtensions.AddUnique(ref behaviour.OnNodeDataChanged, OnSelectNode);
            ActionExtensions.AddUnique(ref behaviour.OnNodeDataChangedBegin, DataChangeValueBegin);
            ActionExtensions.AddUnique(ref behaviour.OnNodeDataChangedEnd, DataChangeValueEnd);
            ActionExtensions.AddUnique(ref behaviour.Graph.OnNodeSelected, OnSelectNode);
        }

        private void DataChangeValueBegin(QuestNodeData data)
        {
            var x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Node Data Changed");
        }

        private void DataChangeValueEnd(QuestNodeData data)
        {
            var x = LBSController.CurrentLevel;
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(x);
            }
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("NodeDataBehaviour");
            visualTree.CloneTree(this);
            
            #region Get VisualElements from UXML
            _nodePanel = this.Q<VisualElement>("ID");
            _actionPanel = this.Q<VisualElement>("Action");
            _noNodeSelectedPanel = this.Q<VisualElement>("NoNodeSelectedPanel");
            
            fieldsVisualElements = this.Q<VisualElement>("InstancedContent");
            
            _actionColor = this.Q<VisualElement>("ActionColor");
            _actionIcon = this.Q<VisualElement>("ActionIcon");
            
            _paramActionLabel = this.Q<Label>("ParamAction");
            _nodeIDLabel = this.Q<Label>("ParamID");
            #endregion
            
            // No node when instanced
            _noNodeSelectedPanel.style.display = DisplayStyle.Flex;

            return this;
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
            toolkit.ActivateTool(toolPicker, behaviour?.OwnerLayer, target);

            // context exclusive from the Node Panel
            VisualElement toolButton = toolkit.GetToolButton(typeof(QuestPicker));
            toolButton.SetEnabled(false);
        }

        private void OnSelectNode(GraphNode graphNode)
        {
            DrawManager.Instance.UpdateSingleComponent(behaviour, behaviour.OwnerLayer);

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

            if (!validNode) return;

            SetNode(node);
        }

        private void SetFields(QuestNode node)
        {
            if(node?.Data?.Fields == null) return;

            fieldsVisualElements.Clear();

            foreach (var field in node.Data.Fields)
            {
                if (field.IsList)
                {
                    CreateFieldList(field);
                }
                else
                {
                    var ve = CreateField(field);
                    if (ve != null) fieldsVisualElements.Add(ve);
                }
            }
        }

        private VisualElement CreateField(GrammarField field)
        {
            if (field != null)
            {
                Type fieldType = field.GetType();

                if (FieldTypeToVisualElement.TryGetValue(fieldType, out Type veType))
                {
                    return (VisualElement)Activator.CreateInstance(veType, field);
                }
            }
            return null;
        }

        private void CreateFieldList(GrammarField listField)
        {
            LBSCustomFoldout foldout = new LBSCustomFoldout();
            foldout.text = listField.name;
            foldout.InitialValue = false;

            var listView = new ListView
            {
                itemsSource = listField.ItemsSource,
                reorderable = true,
                showAddRemoveFooter = true,
                showFoldoutHeader = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                headerTitle = ""
            };

            listView.itemsAdded += (indices) => {
                foreach (var i in indices)
                {
                    // Create force declare the grammar field list entry
                    var newItem = (GrammarField)Activator.CreateInstance(listField.PrimitiveType);
                    listField.ItemsSource[i] = newItem;
                }
            };

            listView.makeItem = () =>
            {
                var dummy = (GrammarField)Activator.CreateInstance(listField.PrimitiveType);
                return CreateField(dummy);
            };

            listView.bindItem = (element, index) =>
            {
                if (element is GrammarFieldEditor editor)
                {
                    editor.SetNewInfo((GrammarField)listView.itemsSource[index]);
                }
            };

            foldout.AddContent(listView);
            fieldsVisualElements.Add(foldout);
        }

        private void SetNode(QuestNode node)
        {
            if (node == null) return;
            var data = node.Data;
            if (data == null) return;

            _paramActionLabel.text = node.TerminalID;
            _nodeIDLabel.text = node.ID;

            Color terminalColor = data.Terminal.color;
            Color backgroundColor = terminalColor;
            backgroundColor.a = BackgroundOpacity;

            _actionIcon.style.unityBackgroundImageTintColor = terminalColor;

            _actionColor.SetBackgroundColor(backgroundColor);
            _actionColor.SetBorder(terminalColor, ActionBorderThickness);

            SetFields(node);
        }
        

        #endregion
        
    }
}