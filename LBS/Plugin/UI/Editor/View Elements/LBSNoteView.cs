using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.VisualElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.ViewElements
{
    [UxmlElement]
    public partial class LBSNoteView : GraphElement
    {
        // Define colors for the note, both when unselected (kinda transparent) and selected (when clicked and focused, more solid)
    
        #region FIELDS

        public LBSNote Note;

        private TextField tempEditor;
        private Label label;
        private VisualElement root;
        private Button deleteBtn;
        private Button collapseBtn;
        private VisualElement handler;
        private static VisualTreeAsset asset;

        // Dragging and Double Click stuff
        private bool isDragging = false;
        private Vector2 dragOffset;
        private Vector2 dragStartPos;
        private float lastClickTime = 0f;
        private const float doubleClickThreshold = 0.3f;

        private const int initialWidth = 100;
        private const int initialHeight = 100;
        private const int baseLabelWidthOffset = 10;
        private const int baseLabelHeightOffset = 50;

        private bool isEditing = false;
        private bool isResizing = false;
        private bool isCollapsed = true;
        private int currentWidth = 200;
        private int currentHeight = 200;

        private string collapseIconGUID = "727cee87f3a4e4a438e17e246bf311fb";
        private string expandIconGUID = "99db0707a9fc8fa48b566aefae9023b7";


        #endregion

        #region CONSTRUCTORS

        public LBSNoteView()
        {
            AddToClassList("lbs-post-it");

            asset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSNoteView");
            asset.CloneTree(this);

            label = this.Q<Label>("Label");
            root = this.Q<VisualElement>("Root");
            deleteBtn = this.Q<Button>("DeleteButton");
            collapseBtn = this.Q<Button>("CollapseButton");
            handler = this.Q<VisualElement>("Handler");


            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallbackOnce<GeometryChangedEvent>(evt => { SetPosition(new Rect(Note.Position, layout.size)); });

            deleteBtn.RegisterCallback<ClickEvent>(evt =>
            {
                var level = LBSController.CurrentLevel;
                EditorGUI.BeginChangeCheck();
                Undo.RegisterCompleteObjectUndo(level, "Remove Note");

                var noteBehaviour = Note.OwnerLayer.GetBehaviour<NoteBehaviour>();
                noteBehaviour?.RemoveNote(Note);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(level);
                }

                DrawManager.Instance.RedrawLayer(Note.OwnerLayer);
            });

            collapseBtn.RegisterCallback<ClickEvent>(evt =>
            {
                if (isCollapsed)
                {
                    collapseBtn.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(collapseIconGUID));
                    isCollapsed = false;
                    ResizeNote(false, currentWidth, currentHeight);
                }
                else
                {
                    collapseBtn.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(expandIconGUID));
                    isCollapsed = true;
                    ResizeNote(true);
                }
            });

            handler.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;

                if (ToolKit.Instance.GetActiveManipulator() is null) return;

                if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                    ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
                {
                    isResizing = true;
                    isCollapsed = false;
                    collapseBtn.style.backgroundImage = new StyleBackground(AssetMacro.LoadAssetByGuid<VectorImage>(collapseIconGUID));
                    handler.CaptureMouse();
                    evt.StopPropagation();
                }
            });

            handler.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (ToolKit.Instance.GetActiveManipulator() is null) return;

                if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                    ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
                {
                    if (isResizing)
                    {
                        Vector2 mouseCanvas = MainView.Instance.contentViewContainer.WorldToLocal(evt.originalMousePosition);
                        Vector2 delta = mouseCanvas - Note.Position;
                        int newWidth = Mathf.Max(initialWidth, (int)delta.x);
                        int newHeight = Mathf.Max(initialHeight, (int)delta.y);
                        ResizeNote(false, newWidth, newHeight);
                    }
                }
            });

            handler.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    if (ToolKit.Instance.GetActiveManipulator() is null) return;

                    if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                        ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
                    {
                        if (isResizing)
                        {
                            isResizing = false;
                            handler.ReleaseMouse();
                        }
                    }
                }
            });
        }

        public LBSNoteView(LBSNote note) : this()
        {
            Note = note;

            label.text = note.Message;
            label.style.width = initialWidth - baseLabelWidthOffset;
            label.style.height = initialHeight - baseLabelHeightOffset;
            label.style.overflow = Overflow.Hidden;
            
            style.width = initialWidth;
            style.height = initialHeight;
            //style.flexGrow = 0;
        }

        #endregion

        #region MOUSE EVENTS

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
            {
                var time = (float)UnityEditor.EditorApplication.timeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold && !isEditing)
                {
                    StartEdition();
                }
                else
                {
                    if (isEditing || isResizing) return;
                    isDragging = true;
                    dragStartPos = Note.Position;
                    dragOffset = MainView.Instance.contentViewContainer.WorldToLocal(evt.originalMousePosition);
                    this.CaptureMouse();
                }
                lastClickTime = time;
                evt.StopPropagation(); // To stop the creation of a note if using AddNote
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                if (ToolKit.Instance.GetActiveManipulator() is null) return;

                if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                    ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
                {
                    if (isEditing || isResizing) return;

                    if (isDragging)
                    {
                        isDragging = false;
                        this.ReleaseMouse();
                    }
                }
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (evt.button != 0) return;
            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator) ||
                ToolKit.Instance.GetActiveManipulator().GetType() == typeof(AddNote))
            {
                if (isDragging)
                {
                    if (parent != null)
                    {
                        Vector2 mouseCanvas = MainView.Instance.contentViewContainer.WorldToLocal(evt.originalMousePosition);
                        Vector2 delta = mouseCanvas - dragOffset;
                        Vector2 newPos = dragStartPos + delta;
                        Note.Position = newPos;
                        SetPosition(new Rect(Note.Position, layout.size));
                    }
                }
            }
        }

        // Check what happens if the mouse leaves the editor window while dragging

        #endregion

        private void StartEdition()
        {
            CreateEditor();
            label.style.display = DisplayStyle.None;
            isEditing = true;
            isDragging = false;
        }

        private void CreateEditor()
        {
            tempEditor = new TextField();
            tempEditor.value = Note.Message;
            tempEditor.style.width = label.style.width;
            tempEditor.style.height = label.style.height;
            tempEditor.style.whiteSpace = WhiteSpace.Normal;
            tempEditor.style.flexGrow = 0;
            tempEditor.style.flexShrink = 0;
            tempEditor.style.overflow = Overflow.Hidden;
            tempEditor.multiline = true;
            tempEditor.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            tempEditor.pickingMode = PickingMode.Ignore;
            root.Add(tempEditor);

            tempEditor.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            {
                tempEditor.Focus();
            });

            tempEditor.RegisterCallback<FocusOutEvent>(evt =>
            {
                Note.Message = tempEditor.value;
                label.text = Note.Message;
                label.style.display = DisplayStyle.Flex;
                root.Remove(tempEditor);
                isEditing = false;
                evt.StopPropagation();
            });
        }

        private void ResizeNote(bool initial, int _width = initialWidth, int _height = initialHeight)
        {
            int width = initial ? initialWidth : currentWidth = _width;
            int height = initial ? initialHeight : currentHeight = _height;

            label.style.width = width - baseLabelWidthOffset;
            label.style.height = height - baseLabelHeightOffset;
            root.style.width = width;
            root.style.height = height;
            style.width = width;
            style.height = height;
            SetPosition(new Rect(layout.x, layout.y, width, height));
            MarkDirtyRepaint();
        }
    }
}
