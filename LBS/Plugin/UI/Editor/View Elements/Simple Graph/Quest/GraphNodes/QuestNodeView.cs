using ISILab.Commons.Utility.Editor;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.Components;
using UnityEditor.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestNodeView : QuestGraphNodeView
    {
        #region CONSTS
        const float iconSize = 24f;
        const float padding = 40f;
        const float minWidth = 160;
        #endregion

        #region FIELDS

        private static VisualTreeAsset _asset;

        #region VIEWS
        private readonly VisualElement _root;
        
        private readonly VisualElement _start;
        private readonly VisualElement _goal;
        private readonly VisualElement _scrollIcon;
        
        private readonly VisualElement _iconGrammarInvalid;
        private readonly VisualElement _iconNodeDataInvalid;
        
        private readonly ToolbarMenu _toolbar;
        private readonly Label _label;
        private readonly QuestActionDetailsView _questActionDetails;

        #endregion
        
        #endregion

        public QuestNodeView(QuestNode graphNode)
        {
            if (_asset == null)
                _asset = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestNodeView");

            _asset.CloneTree(this);

            _label             = this.Q<Label>("Title");
            _root              = this.Q<VisualElement>("Root");
            _start          = this.Q<VisualElement>("StartVe");
            _goal          = this.Q<VisualElement>("GoalVe");
            _scrollIcon = this.Q<VisualElement>("ScrollIcon");
            InvalidConnectionIcon = this.Q<VisualElement>("InvalidConnectionIcon");
            _iconNodeDataInvalid = this.Q<VisualElement>("InvalidDataIcon");
            _iconGrammarInvalid = this.Q<VisualElement>("InvalidGrammarIcon");
            _toolbar           = this.Q<ToolbarMenu>("ToolBar");
            _questActionDetails = this.Q<QuestActionDetailsView>("TooltipWindow");
            
            
            VisualElement coloredVe = this.Q<VisualElement>("Capsule");
            coloredVe.style.backgroundColor = DefaultBackgroundColor;
            
            SetupToolbar();
            SetupCallbacks();

            style.marginBottom = style.marginLeft = style.marginRight = style.marginTop = 0;

            InvalidConnectionIcon.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _iconNodeDataInvalid.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _iconGrammarInvalid.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _questActionDetails.style.display = DisplayStyle.None;
            _questActionDetails.Node = graphNode as QuestNode;

            Node = graphNode;
            SetPosition(new Rect(Node.NodePosition.position, Vector2.one));

            Refresh();
        }


        #region Setup
        private void SetupToolbar()
        {
            _toolbar.style.display = DisplayStyle.None;
            _toolbar.menu.AppendAction("Set as Start Node", MakeRoot);
        }

        public override void Refresh()
        {
            if (Node == null) throw new ArgumentNullException("null node");

            UpdateNodeID();

            UpdateNodeType();

            UpdateGrammarState();

            UpdatePosition();

        }

        private void UpdateNodeType()
        {
            _start.style.display = DisplayStyle.None;
            _goal.style.display = DisplayStyle.None;

            if (Node is QuestNode qn)
            {
                switch (qn.NodeType)
                {
                    case QuestNode.ENodeType.Start:
                        _start.style.display = DisplayStyle.Flex;
                        break;
                    case QuestNode.ENodeType.Goal:
                        _goal.style.display = DisplayStyle.Flex;
                        break;
                }
            }
        }

        private void SetupCallbacks()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());
        }

        #endregion

        #region Updates
        public void UpdatePosition()
        {
            UpdateWidth();

            SetPosition(new Rect(GetPosition().position,
                new Vector2(                
                _root.resolvedStyle.width ,
                _root.resolvedStyle.height))
            );

            OnMoving?.Invoke(GetPosition());
        }

        private void UpdateWidth()
        {
            // Use the Font and Style of the label to measure text without waiting for layout
            var textSize = _label.MeasureTextSize(
                _label.text, 
                0,
                MeasureMode.Undefined, 
                0,
                MeasureMode.Undefined);

            if (string.IsNullOrEmpty(_label.text)) 
                return;

            float iconTotal = iconSize;
            if (Node is QuestNode qn)
            {
                // add icon spaces
                if (!qn.Data.IsValid()) 
                    iconTotal += iconSize;
                if (!Node.ValidGrammar) 
                    iconTotal += iconSize;
                if (!Node.ValidConnections) 
                    iconTotal += iconSize;
                if (qn.NodeType != QuestNode.ENodeType.Middle)
                    iconTotal += iconSize;
            }

            float calculatedWidth = textSize.x + iconTotal + padding;
            float finalWidth = Mathf.Max(minWidth, calculatedWidth);

            _root.style.width = finalWidth;
        }
        #endregion

        #region Grammar State
        protected sealed override void UpdateGrammarState()
        {
            if(Node is not QuestNode qn) return;
         
            base.UpdateGrammarState();

            _iconNodeDataInvalid.style.display = qn.Data.IsValid() ? DisplayStyle.None : DisplayStyle.Flex;
            _iconGrammarInvalid.style.display = Node.ValidGrammar ? DisplayStyle.None : DisplayStyle.Flex;
            this.Q<VisualElement>("Capsule").SetBorder(Node.IsValid() ? ValidGrammarColor : InvalidGrammarColor, 1f);
        }
        #endregion

        #region Toolbar Actions
        private void MakeRoot(DropdownMenuAction _)
        {
            Node.Graph.SetRoot(Node as QuestNode);
            _toolbar.menu.ClearItems();
            _toolbar.menu.AppendAction("Remove Start Node assignation", RemoveRoot);
            UpdateWidth();
        }

        private void RemoveRoot(DropdownMenuAction _)
        {
            Node.Graph.SetRoot(null);
            _toolbar.menu.ClearItems();
            _toolbar.menu.AppendAction("Set as Start Node", MakeRoot);
            UpdateWidth();
        }
        #endregion

        #region Mouse Events
        protected override void OnMouseDown(MouseDownEvent evt)
        {
            base.OnMouseDown(evt);
            if (evt.button == 1 && !_isDragging)
            {
                _toolbar.style.display = DisplayStyle.Flex;
                _toolbar.ShowMenu();
            }
        }

        protected override void OnMouseEnter(MouseEnterEvent evt)
        {
            if (Node == null) return;
            if (!this.enabledSelf) return;
            base.OnMouseEnter(evt);

            if (!_isDragging)
                _questActionDetails.SetDisplays(InvalidConnectionIcon, _iconGrammarInvalid, _iconNodeDataInvalid);
        }

        protected override void OnMouseLeave(MouseLeaveEvent evt)
        {
            _questActionDetails.style.display = DisplayStyle.None;
            base.OnMouseLeave(evt);
        }

        protected override void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
                _questActionDetails.style.display = DisplayStyle.None;

            base.OnMouseMove(evt);
        }
        
        #endregion

        #region Helpers

        private float GetElementWidthIfVisible(VisualElement element, float fallback)
        {
            if (element.style.display != DisplayStyle.Flex) return 0f;
            var width = element.resolvedStyle.width;
            return (float.IsNaN(width) || width == 0) ? fallback : width;
        }

        private void UpdateNodeID()
        {
            var text = Node.ID;
            if (!string.IsNullOrWhiteSpace(text))
                text = char.ToUpper(text.TrimStart()[0]) + text.TrimStart().Substring(1);

            _label.text = text;
        }

        public override VisualElement GetSelectVisualElement()
        {
            return this;
        }
        #endregion
    }
}
