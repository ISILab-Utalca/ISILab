using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.VisualElements;
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
        private static VisualTreeAsset asset;

        // Dragging and Double Click stuff
        private bool isDragging = false;
        private Vector2 dragOffset;
        private Vector2 dragStartPos;
        private float lastClickTime = 0f;
        private const float doubleClickThreshold = 0.3f;

        private bool isEditing = false;

        #endregion

        private string contentText;

        [UxmlAttribute]
        public string ContentText
        {
            get => contentText;
            set
            {
                contentText = value;
                if (label != null)
                {
                    label.text = value;
                }
            }
        }

        #region CONSTRUCTORS

        public LBSNoteView()
        {
            AddToClassList("lbs-post-it");

            asset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSNoteView");
            asset.CloneTree(this);

            label = this.Q<Label>("Label");

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallbackOnce<GeometryChangedEvent>(evt => { SetPosition(new Rect(Note.Position, layout.size)); });
        }

        public LBSNoteView(LBSNote note, float width = 100, float height = 100) : this()
        {
            Note = note;
            label.text = note.Message;
            style.width = label.style.width = width;
            style.height = label.style.height = height;
            style.overflow = Overflow.Hidden;
        }

        #endregion

        #region MOUSE EVENTS

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;
            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                var time = (float)UnityEditor.EditorApplication.timeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold && !isEditing)
                {
                    StartEdition();
                }
                else
                {
                    if (isEditing) return;
                    isDragging = true;
                    dragStartPos = Note.Position;
                    dragOffset = MainView.Instance.contentViewContainer.WorldToLocal(evt.originalMousePosition);
                    this.CaptureMouse();
                }
                lastClickTime = time;
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                if (ToolKit.Instance.GetActiveManipulator() is null) return;

                if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
                {
                    if (isEditing) return;

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

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
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
            //tempEditor.style.height = StyleKeyword.Auto;
            tempEditor.style.whiteSpace = WhiteSpace.Normal;
            tempEditor.multiline = true;
            tempEditor.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            Add(tempEditor);
            tempEditor.Focus();

            tempEditor.RegisterCallback<FocusOutEvent>(evt =>
            { 
                Note.Message = tempEditor.value;
                label.text = Note.Message;
                label.style.display = DisplayStyle.Flex;
                Remove(tempEditor);
                isEditing = false;
                evt.StopPropagation();
            });
        }
    }
}
