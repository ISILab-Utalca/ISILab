
using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using LBS;
using LBS.VisualElements;
using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("QuestBehaviour", typeof(QuestBehaviour))]
    public class QuestBehaviourEditor : LBSCustomEditor, IToolProvider
    {

        #region FIELDS
        private QuestBehaviour _behaviour;

        private const string actionIconGuid = "aa4c8898bd338cb4b91b6516e6d4e0c9";
        private const string orIconGuid = "e06ff34bd346d754eb0a4b12ef3dbe56";
        private const string andIconGuid = "5aed77f9dd05be64b9e99eb9d43d8ce6";

        private AddGraphNode _addNode;
        private RemoveGraphNode _removeNode;
        private ConnectQuestNodes _connectNodes;
        private RemoveQuestConnection _removeConnection;
        #endregion


        #region VIEW ELEMENTS
        private ObjectField _grammarReference;

        private SimplePallete _actionsPallete;
        private SimplePallete _conditionsPallete;
        #endregion


        #region PROPERTIES
        VectorImage QuestActionIcon
        {
            get => LBSAssetMacro.LoadAssetByGuid<VectorImage>(actionIconGuid);
        }
        VectorImage OrIcon
        {
            get => LBSAssetMacro.LoadAssetByGuid<VectorImage>(orIconGuid);
        }
        VectorImage AndIcon
        {
            get => LBSAssetMacro.LoadAssetByGuid<VectorImage>(andIconGuid);
        }

        #endregion

        public QuestBehaviourEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);     
        }

        public sealed override void SetInfo(object paramTarget)
        {
            if (_behaviour != null) return;
            _behaviour = target as QuestBehaviour;

            // this should only happen on object creation
            QuestGraph questGraph = _behaviour?.OwnerLayer.GetModule<QuestGraph>();
            if (questGraph is null) return;
            
            questGraph.LoadGrammar();
            
            // Manually set both
            _grammarReference.value = questGraph.Grammar;
            ChangeGrammar(questGraph.Grammar);

            questGraph.RedrawGraph += () => DrawManager.Instance.RedrawLayer(questGraph.OwnerLayer);
        }

        public void SetTools(ToolKit toolkit)
        {
            _behaviour = target as QuestBehaviour;
            
            _addNode = new AddGraphNode();
            var t1 = new LBSTool(_addNode);
            t1.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            _removeNode = new RemoveGraphNode();
            var t2 = new LBSTool(_removeNode);
            t2.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;

            _addNode.SetRemover(_removeNode);
            
            _connectNodes = new ConnectQuestNodes();
            var t3 = new LBSTool(_connectNodes);
            t3.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            _removeConnection = new RemoveQuestConnection();
            var t4 = new LBSTool(_removeConnection);
            t4.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;

            _connectNodes.SetRemover(_removeConnection);

            toolkit.ActivateTool(t1,_behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t2,_behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t3,_behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t4,_behaviour?.OwnerLayer, target);
            
            _addNode.OnManipulationEnd += RefreshHistoryPanel;
            _removeNode.OnManipulationEnd += RefreshHistoryPanel;
            _connectNodes.OnManipulationEnd += RefreshHistoryPanel;
            _removeConnection.OnManipulationEnd += RefreshHistoryPanel;

            _behaviour ??= _behaviour; // if null, assign
            _behaviour!.Graph.GoToNodeInGraph = null;
            _behaviour!.Graph.GoToNodeInGraph += GoToQuestNode;
        }

        private static void GoToQuestNode(Vector2Int nodePos)
        {
            Vector3 scale = MainView.Instance.viewTransform.scale;
            Rect viewport = MainView.Instance.viewport.layout;
            
            // the x offset depends on the size of the inspector window
            // TODO get the difference between the main view and the inspector window to set the center correctly
            var xOffset = (viewport.width * 0.35f) / scale.x; 
            var yOffset = (viewport.height * 0.5f) / scale.y;
            
            var x = nodePos.x - xOffset;
            var y = nodePos.y - yOffset;

            var position = new Vector3(-x * scale.x, -y * scale.y, 0);
            
            MainView.Instance.UpdateViewTransform(position, scale);
        }

        private void RefreshHistoryPanel()
        {
            SetInfo(target);
            
        }
        
        protected sealed override VisualElement CreateVisualElement()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBehaviourEditor");
            visualTree.CloneTree(this);

            _grammarReference = this.Q<ObjectField>(name: "Grammar");
            _grammarReference.objectType = typeof(LBSGrammar);
            _grammarReference.RegisterValueChangedCallback(evt => ChangeGrammar(evt.newValue as LBSGrammar));

            _actionsPallete = this.Q<SimplePallete>("ActionsPallete");
            _actionsPallete.DisplayAddElement = false;
         

            _conditionsPallete = this.Q<SimplePallete>("ConditionsPallete");
            _conditionsPallete.DisplayAddElement = false;
          
            
            return this;
        }

        private void SetActionPallete()
        { 
            _actionsPallete.NameLabel = "Action nodes";
            _actionsPallete.ShowGroups = false;
            _actionsPallete.ShowAddButton = false;
            _actionsPallete.ShowRemoveButton = false;
            _actionsPallete.ShowNoElement = false;

         //   _actionsPallete.Clear();
            _actionsPallete.DisplayContent(false);
            QuestGraph quest = _behaviour?.OwnerLayer.GetModule<QuestGraph>();
            if (quest == null) return;
            if (quest.Grammar == null || !quest.Grammar.TerminalActions.Any()) return;

            string[] terminals = quest.Grammar.TerminalActions.ToArray();

            _actionsPallete.DisplayContent(true);

            object[] options = new object[terminals.Length];
            for (int i = 0; i < terminals.Length; i++)
            {
                options[i] = terminals[i];
            }

            // Init options
            _actionsPallete.SetOptions(options, (optionView, option) =>
            {
                string terminalAction = (string)option;
                optionView.Label = terminalAction;
                //optionView.FrameColor = bundle.Color;
                optionView.Icon = QuestActionIcon;
            });

            _actionsPallete.OnSelectOption += (selected) =>
            {
                ToolKit.Instance.SetActive(typeof(AddGraphNode));
                _behaviour.activeGraphNodeType = typeof(QuestNode);
                _behaviour.ActionToSet = (string)selected;
            };


            _actionsPallete.Repaint();

        }

        private void SetConditionalPallete()
        {
            _conditionsPallete.NameLabel = "Connection nodes";
            _conditionsPallete.ShowGroups = false;
            _conditionsPallete.ShowAddButton = false;
            _conditionsPallete.ShowRemoveButton = false;
            _conditionsPallete.ShowNoElement = false;

          //  _conditionsPallete.Clear();
            _conditionsPallete.DisplayContent(false);

            QuestGraph quest = _behaviour?.OwnerLayer.GetModule<QuestGraph>();
            if (quest == null) return;
            if (quest.Grammar == null || !quest.Grammar.TerminalActions.Any()) return;

            _conditionsPallete.DisplayContent(true);

            string[] conditionals = {GraphNode.Or, GraphNode.And};

            object[] options = new object[conditionals.Length];
            for (int i = 0; i < conditionals.Length; i++)
            {
                options[i] = conditionals[i];
            }

            // Init options
            _conditionsPallete.SetOptions(options, (optionView, option) =>
            {
                string conditional = (string)option;

                optionView.Label = conditional;
                //optionView.FrameColor = bundle.Color;
                if (conditional == GraphNode.Or)optionView.Icon = OrIcon;
                if (conditional == GraphNode.And) optionView.Icon = AndIcon;
            });

            _conditionsPallete.OnSelectOption += (selected) =>
            {
                string conditional = (string)selected;
                ToolKit.Instance.SetActive(typeof(AddGraphNode));
                if (conditional == GraphNode.Or) _behaviour.activeGraphNodeType = typeof(OrNode);
                if (conditional == GraphNode.And) _behaviour.activeGraphNodeType = typeof(AndNode);
    
                // no action, only node type
                _behaviour.ActionToSet = string.Empty;

            };


            _conditionsPallete.Repaint();

        }


        private void ChangeGrammar(LBSGrammar grammar)
        {
            if (grammar == null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("LBS Quest: Must assign a valid grammar in the Quest Behaviour Editor",LogType.Error,5));
                _grammarReference.value = null;
            }
            else
            {
                QuestGraph quest = _behaviour.OwnerLayer.GetModule<QuestGraph>();
                if (quest == null)  throw new Exception("No Module");
            
                // Check if the new grammar is different at all
                if(quest.Grammar != grammar) quest.Grammar = grammar;
            }

            SetActionPallete();
            SetConditionalPallete();

        }
    }
}