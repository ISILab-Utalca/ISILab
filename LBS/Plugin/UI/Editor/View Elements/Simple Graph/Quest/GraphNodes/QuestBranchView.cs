using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestBranchView : QuestGraphNodeView
    {
        #region Static Assets
        private static VisualTreeAsset _rootAsset;
        #endregion

        #region UI Elements
        private readonly VisualElement _root;
        private VisualElement _or;
        private VisualElement _and;
        #endregion

        public QuestBranchView(BranchNode graphNode)
        {
            if (_rootAsset == null)
                _rootAsset = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestBranchView");

            _rootAsset.CloneTree(this);

            _root = this.Q<VisualElement>("Capsule");
            InvalidConnectionIcon = this.Q<VisualElement>("InvalidConnectionIcon");
            InvalidConnectionIcon.style.unityBackgroundImageTintColor = InvalidGrammarColor;

            VisualElement coloredVe = this.Q<VisualElement>("Capsule");
            coloredVe.style.backgroundColor = DefaultBackgroundColor;

            Node = graphNode ?? throw new ArgumentNullException(nameof(graphNode));

            _or = this.Q<VisualElement>("OrVe");
            _and = this.Q<VisualElement>("AndVe");

            if (graphNode.Kind == NodeKind.Or) _or.style.display = DisplayStyle.Flex;
            if (graphNode.Kind == NodeKind.And) _and.style.display = DisplayStyle.Flex;

            SetPosition(new Rect(Node.Area.position, Vector2.one));

            RegisterCallbacks();
            Refresh();
        }

        #region Callbacks
        private void RegisterCallbacks()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseUpEvent>(OnMouseUp);

            _root.RegisterCallback<MouseDownEvent>(OnMouseDownCapsule);
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
        }
        #endregion

        #region Update
        public override void Refresh()
        {
            UpdateGrammarState();               
            SetPosition(new Rect(GetPosition().position, new Vector2(_root.resolvedStyle.width, _root.resolvedStyle.height)));
            OnMoving?.Invoke(GetPosition());
        }

        protected override void UpdateGrammarState()
        {
            base.UpdateGrammarState();
            _root.SetBorder(!Node.ValidConnections ? InvalidGrammarColor : ValidGrammarColor, 1f);
            _or.SetBorder(!Node.ValidConnections ? InvalidGrammarColor : ValidGrammarColor, 1f);
            _and.SetBorder(!Node.ValidConnections ? InvalidGrammarColor : ValidGrammarColor, 1f);
        }
        
        protected override void OnMouseDown(MouseDownEvent evt)
        {
            base.OnMouseDown(evt);

            if (evt.button == 1)
            {
                MakeMenu(evt);
            }

            // Remove element
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

        private void MakeMenu(MouseDownEvent evt)
        {
            // Create the menu
            var menu = new GenericMenu();

            if (Node.Kind == NodeKind.Or)
            {
                menu.AddItem(new GUIContent("Set as 'Or branch' "), false, () =>
                {
                    Debug.Log("Set as Or branch");
                });
            }
    
            if (Node.Kind == NodeKind.And)
            {
                menu.AddItem(new GUIContent("Set as 'And branch' "), false, () =>
                {
                    Debug.Log("Set as And branch");
                });
            }

            menu.AddItem(new GUIContent("Delete node"), false, () =>
            {
                Debug.Log("Delete node");
            });
            
            menu.ShowAsContext();
            evt.StopPropagation();
        }

        public override VisualElement GetSelectVisualElement()
        {
            return this;
        }

        #endregion
    }
}
