using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.VisualElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using ISILab.LBS.Behaviours;
using ISILab.Extensions;
using ISILab.LBS.Modules;

namespace ISILab.LBS.VisualElements 
{
    public abstract class QuestGraphNodeView : GraphElement
    {
        #region Static Colors

        protected static readonly Color InvalidGrammarColor     = LBSSettings.Instance.view.errorColor;
        protected static readonly Color DefaultBackgroundColor = LBSSettings.Instance.view.toolkitNormalDark;
        protected static readonly Color ValidGrammarColor   = LBSSettings.Instance.view.successColor;

        #endregion

        #region Fields

        private GraphNode node;

        public GraphNode Node
        {
            get => node;
            set
            {
                node = value;
                ActionExtensions.AddUnique(ref node.OnSelect, OnSelect);
                ActionExtensions.AddUnique(ref node.Graph.OnUpdateGraph, Refresh);
                ActionExtensions.AddUnique(ref node.OnDeselect, OnDeselect);
                ActionExtensions.AddUnique(ref OnMoving, UpdateNodePosition);
            }
        }

        private void OnSelect()
        {
            SelectView(true);

        }

        private void OnDeselect()
        {
            SelectView(false);
        }

        protected VisualElement InvalidConnectionIcon;

        private static Type prevManipulator;

        private const float Alpha = 0.33f;

        protected bool _isDragging = false;

        #endregion

        #region Events

        public Action<Rect> OnMoving;
        
        #endregion

        #region Grammar State

        protected virtual void UpdateGrammarState()
        {
            InvalidConnectionIcon.style.display = Node.ValidConnections ? DisplayStyle.None : DisplayStyle.Flex;
        }
        
        #endregion

        #region Mouse Events
        protected virtual void OnMouseDown(MouseDownEvent evt)
        {
            if (Node == null) return;
            if (Node.Graph == null) return;
            if (LBSMainWindow.Instance._selectedLayer == null) return;

            if (LBSMainWindow.Instance._selectedLayer != Node.Graph.OwnerLayer) return;
          

            if (evt.button == 0 && ToolKit.Instance.GetActiveManipulatorInstance() is SelectManipulator)
            {
                LBSInspectorPanel.ActivateBehaviourTab();

                Node.Select();
                _isDragging = true;
                this.CaptureMouse();
            }
            
            DrawManager.Instance.PickingModeChangeAll(PickingMode.Ignore, new List<VisualElement> {this});
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (!Equals(LBSMainWindow.Instance._selectedLayer, Node.Graph.OwnerLayer)) return;
            // only move the selected node
            if (Node == null || !Node.IsSelected()) return;
            if (e.pressedButtons != 1) return; // only while dragging
            if (!MainView.Instance.HasManipulator<SelectManipulator>()) return;

            var grabPosition = GetPosition().position + e.mouseDelta / MainView.Instance.viewTransform.scale;
            grabPosition *= MainView.Instance.viewport.transform.scale;

            var newPos = new Rect(grabPosition.x, grabPosition.y, resolvedStyle.width, resolvedStyle.height);
            SetPosition(newPos);

            OnMoving?.Invoke(newPos);
            MarkDirtyRepaint();
        }

        protected virtual void OnMouseLeave(MouseLeaveEvent e) 
        {
            if (!Equals(LBSMainWindow.Instance._selectedLayer, Node.Graph.OwnerLayer)) return;
            if (Node == null) return;
            if (_isDragging) return;

            RestoreManipulator();
            //OnMouseMove(MouseMoveEvent.GetPooled(e.mousePosition, e.button, e.clickCount, e.mouseDelta));
            DrawManager.Instance.PickingModeRestoreAll();
        }
        
        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (!Equals(LBSMainWindow.Instance._selectedLayer, Node.Graph.OwnerLayer)) return;

            RestoreManipulator();
            DrawManager.Instance.PickingModeRestoreAll();
            DrawManager.Instance.RedrawLayer(Node.Graph.OwnerLayer);

            _isDragging = false;
            this.ReleaseMouse();

            /// avoid recall on assistant
            Type activeManipulator = ToolKit.Instance.GetActiveManipulatorInstance().GetType();
            bool usingSelect = activeManipulator == typeof(SelectManipulator);
            bool usingAdd = activeManipulator == typeof(AddGraphNode);
            if (usingAdd || usingSelect) evt.StopImmediatePropagation();
        }

        protected virtual void OnMouseEnter(MouseEnterEvent evt)
        {
            if (!Equals(LBSMainWindow.Instance._selectedLayer, Node.Graph.OwnerLayer)) return;
            ShelfManipulator();
        }
        
        private void ShelfManipulator()
        {
            if(ToolKit.Instance.GetActiveManipulatorInstance() is null) return;
            Type ActiveManipulator = ToolKit.Instance.GetActiveManipulatorInstance().GetType();
            bool usingAddNode = ActiveManipulator == typeof(AddGraphNode);
            
            // only set select if using addnode
            if (usingAddNode)
            {
                if (prevManipulator is null)
                {
                    prevManipulator = ActiveManipulator;
                }
                
                ToolKit.Instance.SetActive(typeof(SelectManipulator));
            }
            
        }

        public abstract void Refresh();

        private void RestoreManipulator()
        {
            if (prevManipulator is not null)
            {
                ToolKit.Instance.SetActive(prevManipulator);
                prevManipulator = null;
            }
        }
        #endregion

        private void UpdateNodePosition(Rect rect)
        {
            if (Node == null) return;
            Node.NodePosition = rect;
        }

        #region Selection

        public abstract VisualElement GetSelectVisualElement();

        public void SelectView(bool select)
        {
            var color = DefaultBackgroundColor;
            if (select)
            {
                color = Node.IsValid() ? ValidGrammarColor : InvalidGrammarColor;
        
                // Blend color to simulate the alpha effect
                float r = color.r * Alpha; 
                float g = color.g * Alpha;
                float b = color.b * Alpha;
                color = new Color(r, g, b, 1f); 
            }

            VisualElement coloredVe = this.Q<VisualElement>("Capsule");
            coloredVe.style.backgroundColor = new StyleColor(color);
        }
        #endregion
    }
}
