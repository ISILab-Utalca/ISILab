using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.Components;
using LBS.VisualElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.VisualElements
{
    /// <summary>
    /// Represents a visual element on the quest graph used to indicate a trigger area or region.
    /// 
    /// This element is associated with a <see cref="QuestNodeData"/> and draws a visual box on the graph.
    /// 
    /// Supports interaction such as:
    /// - Dragging to reposition
    /// - Resizing via a handle
    /// - Updating the logical data when moved
    /// - An Icon that represents the node type for easier readability.
    /// 
    /// Also handles custom visual generation through <see cref="MeshGenerationContext"/> to draw lines between this element and its node origin.
    /// </summary>
    public sealed class TriggerElementArea : GraphElement
    {

        private QuestNodeData _nodeData;
        private readonly GrammarArea _areaField;
        private Color _currentColor;
        
        private string _activeHandle;
        private const string HandleBottomLeft = "bl";
        private const string HandleBottomRight = "br";
        private const string HandleTopLeft = "tl";
        private const string HandleTopRight = "tr";

        private const float GraphGridLength = 100;
        
        private bool _resizing;
        private readonly bool _isCenter;
        private bool _isDragging;
        
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartPosition;
        private Vector2 _resizeStartPosition;
        private Type _prevManipulatorType;
        private VisualElement triggerElementGizmo;
        private VisualElement targetIcon;
        private readonly VisualElement cornerTargetIcon;

        public TriggerElementArea(QuestNodeData nodeData, GrammarArea grammarArea, QuestNodeView nodeView,
            bool centerTarget = true)
        {

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TriggerElementArea");
            visualTree.CloneTree(this);

            triggerElementGizmo = this.Q<VisualElement>("TriggerElementSelector");
            targetIcon = this.Q<VisualElement>("TargetIcon");
            cornerTargetIcon = this.Q<VisualElement>("CornerTargetIcon");

            _isCenter = centerTarget;
            _areaField = grammarArea;
            _nodeData = nodeData;
            ActionExtensions.AddUnique(ref _areaField.data.OnDataChanged, UpdateData);

            var terminal = _nodeData.Terminal;

            if (terminal != null)
            {
                _currentColor = terminal.color;

                var icon = new StyleBackground(terminal.Icon);
                targetIcon.style.backgroundImage = icon;
                cornerTargetIcon.style.backgroundImage = icon;

                UpdateTargetIcon();
            }
            else
            {
                Debug.LogError($"[LBS] TriggerElementArea failed to load GrammarTerminal for ID: {_nodeData.ID}");
                _currentColor = LBSSettings.Instance.view.errorColor; // Error visibility
            }

            // Icons
            targetIcon.style.display = _isCenter ? DisplayStyle.Flex : DisplayStyle.None;
            cornerTargetIcon.style.display = _isCenter ? DisplayStyle.None : DisplayStyle.Flex;

            // Calculate initial visual position
            var area = _areaField.value;
            Vector2 position = _nodeData.OwnerLayer.FixedToPosition(
                new Vector2Int((int)area.x, (int)area.y), true);
            Rect drawArea = new(position, new Vector2(area.width * GraphGridLength, area.height * GraphGridLength));
            SetPosition(drawArea);

            // Styling
            Color backgroundColor = _currentColor;
            backgroundColor.a = 0.2f;
            triggerElementGizmo.style.backgroundColor = backgroundColor;
            triggerElementGizmo.style.unityBackgroundImageTintColor = backgroundColor;
            triggerElementGizmo.style.borderBottomColor = _currentColor;
            triggerElementGizmo.style.borderTopColor = _currentColor;
            triggerElementGizmo.style.borderRightColor = _currentColor;
            triggerElementGizmo.style.borderLeftColor = _currentColor;

            // Border setups
            SetupResizeHandle("Handle_bl", HandleBottomLeft, _isCenter);
            SetupResizeHandle("Handle_br", HandleBottomRight, _isCenter);
            SetupResizeHandle("Handle_tl", HandleTopLeft, _isCenter);
            SetupResizeHandle("Handle_tr", HandleTopRight, _isCenter);

            // Register mouse callbacks on the whole element
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            // callbacks
            nodeView.OnMoving += (_) => UpdateData(_nodeData);
            generateVisualContent -= OnGenerateVisualContent;
            generateVisualContent += OnGenerateVisualContent;

            //activeTriggerElementArea = this;

        }

        private void UpdateTargetIcon()
        {
            // target icon only for the default node area (main trigger area). data can have multiple area fields
            var displayTarget = _nodeData.Area == _areaField ? DisplayStyle.Flex : DisplayStyle.None;
            targetIcon.style.display = displayTarget;
        }

        private void OnMouseEnter(MouseEnterEvent evt) => ShelfManipulator();

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (!_isDragging && !_resizing)
                RestoreManipulator();
        }

        void SetupResizeHandle(string handleName, string handleCode, bool isCenter)
        {
            VisualElement handle = this.Q<VisualElement>(handleName);
            handle.style.display = isCenter ? DisplayStyle.Flex : DisplayStyle.None;
            VisualElement handleArea = handle.Q<VisualElement>("handleArea");
            
            // can only resize the main trigger area of a quest action node
            if (!isCenter) return;
            
            handle.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_isDragging || _resizing) return;

                RestoreManipulator();
                
                if(pickingMode == PickingMode.Ignore) return;
                _resizing = false;
                _activeHandle = null;
                handleArea.style.display = DisplayStyle.None;
            });
            
            handle.RegisterCallback<MouseEnterEvent>(_ =>
            {
                ShelfManipulator();
            });

            handle.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (pickingMode == PickingMode.Ignore) return;
                // only one resizer at a time
                if (_resizing) return;

                _resizeStartPosition = GetPosition().position;

                _resizing = true;
                _activeHandle = handleCode;
                handleArea.style.display = DisplayStyle.Flex;
                handle.CaptureMouse();
            });

            handle.RegisterCallback<MouseUpEvent>(_ =>
            {
                RestoreManipulator();
                
                if(pickingMode == PickingMode.Ignore) return;
                _resizing = false;
                handleArea.style.display = DisplayStyle.None;

                if (_nodeData.OwnerLayer is null) return;

                Rect currentRect = GetPosition();

                float deltaX = currentRect.x - _resizeStartPosition.x;
                float deltaY = currentRect.y - _resizeStartPosition.y;

                // Round position and size by 100
                float posX = Mathf.Round(_resizeStartPosition.x / GraphGridLength);
                float posY = -Mathf.Round(_resizeStartPosition.y / GraphGridLength);
                float width = Mathf.Round(currentRect.width / GraphGridLength);
                float height = Mathf.Round(currentRect.height / GraphGridLength);

                int deltaTileX = Mathf.RoundToInt(deltaX / GraphGridLength);
                int deltaTileY = Mathf.RoundToInt(deltaY / GraphGridLength);

                if (_activeHandle == HandleTopLeft)
                {
                    posX += deltaTileX;
                    posY -= deltaTileY;
                }
                else if (_activeHandle == HandleTopRight)
                {
                    posX -= deltaTileX;
                    posY -= deltaTileY;
                }
                else if (_activeHandle == HandleBottomLeft)
                {
                    posX += deltaTileX;
                    posY += deltaTileY;
                }
                // BottomRight does’t change origin

                // Update the logical area in tile space
                _areaField.SetValue(new Rect(posX, posY, width, height));
                _nodeData.Node.Select();

                handle.ReleaseMouse();
                _activeHandle = null;

                Vector2 position = _nodeData.OwnerLayer.FixedToPosition(new Vector2Int((int)_areaField.value.x, (int)_areaField.value.y), true);
                Rect drawArea = new(position, new Vector2(_areaField.value.width * GraphGridLength, _areaField.value.height * GraphGridLength));
                SetPosition(drawArea);

                //DrawManager.Instance.RedrawLayer(_data.Layer);
                DrawManager.Instance.DrawSingleComponent(this, _nodeData.OwnerLayer);
            });

            // Hide the areas by default(show when click on handle, hide on mouse up)
            handleArea.style.display = DisplayStyle.None;
            handle.RegisterCallback<MouseMoveEvent>(OnHandleRectMove);
        }

        /// <summary>
        /// Draws a dotted line from the NodeView to the Trigger center
        /// </summary>
        /// <param name="mgc"></param>
        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (!_isCenter) return;

            Painter2D painter = mgc.painter2D;
            painter.BeginPath(); 

            var nodeElements = MainView.Instance.GetElementsFromLayer(_nodeData.OwnerLayer, _nodeData.Node);
            GraphElement node = nodeElements?.FirstOrDefault();
            if (node == null) return;

            Vector2 center = new Vector2(GetPosition().width / 2f, GetPosition().height / 2f);

            Rect nodeRect = node.worldBound;
            Vector2 nodeWorldCenter = nodeRect.position + nodeRect.size / 2f;

            Vector2 to = this.WorldToLocal(nodeWorldCenter);

            painter.DrawDottedLine(center, to, _currentColor, 4f, 10f);
        }


        private void OnMouseDown(MouseDownEvent e)
        {
            // If resizing do NOT MOVE
            if(_resizing) return;
            
            if (e.button != 0) return;
            _isDragging = true;
            _dragStartMouse = e.mousePosition;
            this.CaptureMouse();
            
            
            Vector2Int tilePosition = new Vector2Int((int)_areaField.value.x, (int)_areaField.value.y);
            _dragStartPosition = _nodeData.Graph.OwnerLayer.FixedToPosition(tilePosition, true);

            DrawManager.Instance.PickingModeChangeAll(PickingMode.Ignore, new List<VisualElement> {this});
            
            e.StopPropagation();
        }

        private void ShelfManipulator()
        {
            if(ToolKit.Instance.GetActiveManipulatorInstance() is null) return;
            Type activeManipulator = ToolKit.Instance.GetActiveManipulatorInstance().GetType();
            bool usingAddNode = activeManipulator == typeof(AddGraphNode);
            bool usingRemoveNode = activeManipulator == typeof(RemoveGraphNode);
            
            // only set select if using addnode or remove node
            if (usingAddNode || usingRemoveNode)
            {
                if (_prevManipulatorType is null)
                {
                    Debug.Log("Stored");
                    _prevManipulatorType = activeManipulator;
                }
                
                ToolKit.Instance.SetActive(typeof(SelectManipulator));
            }
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            // If resizing do NOT MOVE
            if(_resizing) return;

            if (!_isDragging || e.pressedButtons != 1) return;
            
            if (!MainView.Instance.HasManipulator<SelectManipulator>()) return;

            Vector3 scale = MainView.Instance.viewTransform.scale;

            Vector2 delta = (e.mousePosition - _dragStartMouse) / scale;
            Vector2 newPos = _dragStartPosition + delta;

            Rect newRect = new(newPos, GetPosition().size);
            SetPosition(newRect);
            MarkDirtyRepaint();

            e.StopImmediatePropagation();
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            // If resizing do NOT MOVE
            if(_resizing) return;
            
            if (!_isDragging) return;
            _isDragging = false;
            this.ReleaseMouse();

            _areaField.SetValue(new Rect(
                Mathf.Round(GetPosition().x/GraphGridLength), 
                -Mathf.Round(GetPosition().y/GraphGridLength), 
                _areaField.value.width, 
                _areaField.value.height)
            );

            _nodeData.Node.Select();
            DrawManager.Instance.RedrawLayer(_nodeData.OwnerLayer);
            DrawManager.Instance.PickingModeRestoreAll();

            RestoreManipulator();
        }

        private void RestoreManipulator()
        {
            if (_prevManipulatorType is not null)
            {
                Debug.Log("Restored");
                ToolKit.Instance.SetActive(_prevManipulatorType);
                _prevManipulatorType = null;
            }
        }

        void OnHandleRectMove(MouseMoveEvent e)
        {
            if (!_resizing || string.IsNullOrEmpty(_activeHandle)) return;
            if (e.pressedButtons != 1 || e.button != 0) return;

            Vector3 scale = MainView.Instance.viewTransform.scale;
            Vector2 delta = e.mouseDelta / scale;
            Rect currentRect = GetPosition();

            float newX = currentRect.x;
            float newY = currentRect.y;
            float newWidth = currentRect.width;
            float newHeight = currentRect.height;

            if (_activeHandle.Contains("l"))
            {
                newX += delta.x;
                newWidth -= delta.x;
            }
            if (_activeHandle.Contains("r"))
            {
                newWidth += delta.x;
            }
            if (_activeHandle.Contains("t"))
            {
                newY += delta.y;
                newHeight -= delta.y;
            }
            if (_activeHandle.Contains("b"))
            {
                newHeight += delta.y;
            }

            // Clamp minimum size
            newWidth = Mathf.Max(newWidth, 20);
            newHeight = Mathf.Max(newHeight, 20);

            SetPosition(new Rect(newX, newY, newWidth, newHeight));

            e.StopPropagation();
        }

        public void UpdateData(QuestNodeData newData)
        {
            if (_nodeData != newData || _nodeData.Terminal == null) return;
            _currentColor = _nodeData.Terminal.color;

            // Update position
            Vector2 position = _nodeData.OwnerLayer.FixedToPosition(
                new Vector2Int((int)_areaField.value.x, (int)_areaField.value.y), true);

            Rect drawArea = new(
                position,
                new Vector2(_areaField.value.width * GraphGridLength, _areaField.value.height * GraphGridLength)
            );

            SetPosition(drawArea);

            // Update visuals (colors, icon, etc.)
            var triggerElementGizmo = this.Q<VisualElement>("TriggerElementSelector");

            Color bg = _currentColor;
            bg.a = 0.2f;

            triggerElementGizmo.style.backgroundColor = bg;
            triggerElementGizmo.style.borderBottomColor = _currentColor;
            triggerElementGizmo.style.borderTopColor = _currentColor;
            triggerElementGizmo.style.borderRightColor = _currentColor;
            triggerElementGizmo.style.borderLeftColor = _currentColor;

            var targetIcon = this.Q<VisualElement>("TargetIcon");
            targetIcon.style.backgroundImage = new StyleBackground(_nodeData.Terminal.Icon);

            UpdateTargetIcon();

            MarkDirtyRepaint();
        }

    }
}