
using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
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
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.Progress;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("QuestBehaviour", typeof(QuestBehaviour))]
    public class QuestBehaviourEditor : LBSCustomEditor, IToolProvider
    {

        #region FIELDS
        private static QuestBehaviour behaviour;

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
            target = paramTarget;
            behaviour = target as QuestBehaviour;

            ChangeGrammar(behaviour.Grammar);

            ActionExtensions.AddUnique(ref behaviour.Graph.OnForceUpdate, Redraw);
            ActionExtensions.AddUnique(ref behaviour.Graph.OnSelect, Redraw);
            ActionExtensions.AddUnique(ref behaviour.Graph.GraphPosition, GoToQuestNode);
        }

        private void Redraw(object node)
        {
            if(LBSMainWindow.Instance.SelectedLayer == behaviour.OwnerLayer)
                DrawManager.Instance.UpdateSingleComponent(behaviour, behaviour.OwnerLayer);
        }

        private void Redraw()
        {
            if (LBSMainWindow.Instance.SelectedLayer == behaviour.OwnerLayer)
                DrawManager.Instance.UpdateSingleComponent(behaviour, behaviour.OwnerLayer);
        }

        public void SetTools(ToolKit toolkit)
        {
            behaviour = target as QuestBehaviour;
            
            _addNode = new AddGraphNode();
            var t1 = new LBSTool(_addNode);
            t1.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            _removeNode = new RemoveGraphNode();
            var t2 = new LBSTool(_removeNode);
            t2.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;

            //_addNode.SetRemover(_removeNode);
            
            _connectNodes = new ConnectQuestNodes();
            var t3 = new LBSTool(_connectNodes);
            t3.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            
            _removeConnection = new RemoveQuestConnection();
            var t4 = new LBSTool(_removeConnection);
            t4.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;

            _connectNodes.SetRemover(_removeConnection);

            toolkit.ActivateTool(t1,behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t2,behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t3,behaviour?.OwnerLayer, target);
            toolkit.ActivateTool(t4,behaviour?.OwnerLayer, target);

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

        
        protected sealed override VisualElement CreateVisualElement()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBehaviourEditor");
            visualTree.CloneTree(this);

            _grammarReference = this.Q<ObjectField>(name: "Grammar");
            _grammarReference.objectType = typeof(LBSGrammar);
            _grammarReference.RegisterValueChangedCallback(evt => ChangeGrammar(evt.newValue as LBSGrammar));

            _actionsPallete = this.Q<SimplePallete>("ActionsPallete");
            _actionsPallete.DisplayContent(false);

            _conditionsPallete = this.Q<SimplePallete>("ConditionsPallete");
            _conditionsPallete.DisplayContent(false);

            var or = Components.NodeKind.Or.ToString();
            var and = Components.NodeKind.And.ToString();

            // Init options
            string[] conditionals = {or, and};

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
                if (conditional == or) optionView.Icon = OrIcon;
                if (conditional == and) optionView.Icon = AndIcon;
            });

            _conditionsPallete.OnSelectOption += (selected) =>
            {
                string conditional = (string)selected;
                ToolKit.Instance.SetActive(typeof(AddGraphNode));

                // no action, only node type
                behaviour.ActiveTerminal = string.Empty;
                if (conditional == or)
                    behaviour.ActiveNodeKind = Components.NodeKind.Or;
                else if (conditional == and)
                    behaviour.ActiveNodeKind = Components.NodeKind.And;
            };

            _actionsPallete.OnSelectOption += (selected) =>
            {
                ToolKit.Instance.SetActive(typeof(AddGraphNode));
                behaviour.ActiveNodeKind = NodeKind.Terminal;
                behaviour.ActiveTerminal = (string)selected;
            };

            return this;
        }

        private void SetActionPallete()
        { 

            _actionsPallete.DisplayContent(false);

            if (behaviour.Grammar == null || !behaviour.Grammar.TerminalActions.Any()) 
                return;

            string[] terminals = behaviour.Grammar.TerminalActions.ToArray();

            _actionsPallete.DisplayContent(true);

            object[] options = new object[terminals.Length];
            for (int i = 0; i < terminals.Length; i++)
            {
                options[i] = terminals[i];
            }

            _actionsPallete.SetOptions(options, (optionView, option) =>
            {
                string terminalAction = (string)option;

                optionView.Label = terminalAction;
                //optionView.Icon = QuestActionIcon;

                var terminal = behaviour.Grammar.GetTerminal(terminalAction);
                if (terminal != null)
                {
                    optionView.FrameColor = terminal.color;
                    optionView.Icon = terminal.Icon;
                }

  
            });

            _actionsPallete.Repaint();

        }

        private void SetConditionalPallete()
        {
            _conditionsPallete.DisplayContent(false);

            if (behaviour.Grammar == null || !behaviour.Grammar.TerminalActions.Any()) 
                return;

            _conditionsPallete.DisplayContent(true);

            _conditionsPallete.Repaint();

        }


        private void ChangeGrammar(LBSGrammar newGrammar)
        {
            if (newGrammar == null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("LBS Quest: Must assign a valid grammar in the Quest Behaviour Editor",LogType.Error,5));
                behaviour.Grammar = null;
            }
            else
            {
                // Check if the new grammar is different at all
                if(behaviour.Grammar != newGrammar) 
                    behaviour.Grammar = newGrammar;
            }

            _grammarReference.value = newGrammar;
            SetActionPallete();
            SetConditionalPallete();

        }
    }
}