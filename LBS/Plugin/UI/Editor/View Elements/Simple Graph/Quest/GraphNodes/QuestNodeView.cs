using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using LBS.VisualElements;
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestNodeView : QuestGraphNodeView
    {
        private const float IconSize = 24f;
        private const float Padding = 40f;
        private const float MinWidth = 160f;

        private static VisualTreeAsset _asset;

        private readonly VisualElement _root;
        private readonly VisualElement _start;
        private readonly VisualElement _goal;
        private readonly VisualElement _scrollIcon;
        private readonly VisualElement _iconGrammarInvalid;
        private readonly VisualElement _iconNodeDataInvalid;
        private readonly VisualElement _capsule;
        private readonly ToolbarMenu _toolbar;
        private readonly Label _label;
        private readonly QuestActionDetailsView _questActionDetails;
        private readonly QuestNode _questNode;

        private bool _isBound;

        public QuestNodeView(QuestNode graphNode)
        {
            _questNode = graphNode ?? throw new ArgumentNullException(nameof(graphNode));
            Node = graphNode;

            _asset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestNodeView");
            _asset.CloneTree(this);

            _label = this.Q<Label>("Title");
            _root = this.Q<VisualElement>("Root");
            _start = this.Q<VisualElement>("StartVe");
            _goal = this.Q<VisualElement>("GoalVe");
            _scrollIcon = this.Q<VisualElement>("ScrollIcon");
            InvalidConnectionIcon = this.Q<VisualElement>("InvalidConnectionIcon");
            _iconNodeDataInvalid = this.Q<VisualElement>("InvalidDataIcon");
            _iconGrammarInvalid = this.Q<VisualElement>("InvalidGrammarIcon");
            _toolbar = this.Q<ToolbarMenu>("ToolBar");
            _questActionDetails = this.Q<QuestActionDetailsView>("TooltipWindow");
            _capsule = this.Q<VisualElement>("Capsule");

            DefaultBackgroundColor = _capsule.resolvedStyle.backgroundColor;

            InvalidConnectionIcon.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _iconNodeDataInvalid.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _iconGrammarInvalid.style.unityBackgroundImageTintColor = InvalidGrammarColor;
            _questActionDetails.style.display = DisplayStyle.None;

            _questActionDetails.Node = _questNode;

            SetPosition(new Rect(Node.Area.position, Vector2.one));

            SetupToolbar();
            SetupCallbacks();
            SetLabelID();

            Refresh();
        }

        private void SetupToolbar()
        {
            _toolbar.style.display = DisplayStyle.None;
            _toolbar.menu.AppendAction("Set as Start Node", MakeRoot);
        }

        private void SetupCallbacks()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());

            _capsule.RegisterCallback<MouseDownEvent>(OnMouseDownCapsule);
            _questNode.Graph.OnForceUpdate += Refresh;
        }

        private void OnMouseDownCapsule(MouseDownEvent evt)
        {
            if (ToolKit.Instance.GetActiveManipulatorInstance() is null) 
                return;

            var activeManipulator = ToolKit.Instance.GetActiveManipulatorInstance();
            if (activeManipulator is null)
                return;

            var rgn = activeManipulator as RemoveGraphNode;
            if (rgn is null)
                return;

            rgn.Delete(Node);
        }

        public override void Refresh()
        {
            if (Node == null) throw new ArgumentNullException(nameof(Node), "Underlying Node reference is null");

            UpdateNodeType();
            UpdateRefresh();
            UpdateGrammarState();
            UpdatePosition();
        }

        private void UpdateNodeType()
        {
            _start.style.display = _questNode.NodeType == GraphNodeType.Start ? DisplayStyle.Flex : DisplayStyle.None;
            _goal.style.display = _questNode.NodeType == GraphNodeType.Goal ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateRefresh()
        {
            if (_isBound) return;

            foreach (var field in _questNode.Data.Fields)
            {
                ActionExtensions.RemoveMethod(ref field.Refresh, nameof(NotifyValidData));
                ActionExtensions.AddUnique(ref field.Refresh, NotifyValidData);
            }

            _isBound = true;
        }

        public void UpdatePosition()
        {
            UpdateWidth();
            SetPosition(new Rect(GetPosition().position, new Vector2(_root.resolvedStyle.width, _root.resolvedStyle.height)));
            OnMoving?.Invoke(GetPosition());
        }

        private void UpdateWidth()
        {
            if (string.IsNullOrEmpty(_label.text)) return;

            var textSize = _label.MeasureTextSize(_label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            float iconTotal = IconSize;

            if (!_questNode.Data.IsValid()) iconTotal += IconSize;
            if (!_questNode.ValidGrammar) iconTotal += IconSize;
            if (!Node.ValidConnections) iconTotal += IconSize;
            if (_questNode.NodeType != GraphNodeType.Middle) iconTotal += IconSize;

            _root.style.width = Mathf.Max(MinWidth, textSize.x + iconTotal + Padding);
        }

        protected sealed override void UpdateGrammarState()
        {
            base.UpdateGrammarState();

            _iconNodeDataInvalid.style.display = _questNode.Data.IsValid() ? DisplayStyle.None : DisplayStyle.Flex;
            _iconGrammarInvalid.style.display = _questNode.ValidGrammar ? DisplayStyle.None : DisplayStyle.Flex;
            _capsule.SetBorder(Node.IsValid() ? ValidGrammarColor : InvalidGrammarColor, 1f);
        }

        private void MakeRoot(DropdownMenuAction _)
        {
            Node.Graph.SetRoot(_questNode);
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

        protected override void OnMouseDown(MouseDownEvent evt)
        {
            base.OnMouseDown(evt);
            if (evt.button == 1 && !_isDragging)
            {
                _toolbar.style.display = DisplayStyle.Flex;
                _toolbar.ShowMenu();
            }
        }

        private void NotifyValidData(GrammarField field)
        {
            if (field.IsList)
            {
                foreach (var item in field.ItemsSource)
                {
                    if (item is GrammarField gf && !gf.IsValid())
                        LBSMainWindow.MessageNotify(gf.GetValidStateLog());
                }
            }
            else if (!field.IsValid())
            {
                LBSMainWindow.MessageNotify(field.GetValidStateLog());
            }

            UpdateGrammarState();
        }

        protected override void OnMouseEnter(MouseEnterEvent evt)
        {
            if (Node == null || !enabledSelf) return;
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

        private void SetLabelID()
        {
            var rawText = Node.ID?.TrimStart();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                _label.text = string.Empty;
                return;
            }

            _label.text = $"{char.ToUpper(rawText[0])}{rawText[1..]}";
        }

        public override VisualElement GetSelectVisualElement() => this;
    }
}