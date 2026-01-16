using System;
using System.Linq;
using ISILab.Commons.Utility.Editor;
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

        /*
            Falta que la nota no est� gigante
            Ver lo del zoom
            Que no se borren las notas tras recargar
            Ese peque�o detalle para que no se mueva el texto hacia arriba tras un salto de l�nea
            Cambiar (si se puede) la forma de crear un salto de l�nea
        */
    
        #region FIELDS

        public LBSNote Note;

        private TextField textField;
        private Label label;
        private static VisualTreeAsset asset;

        // Dragging and Double Click stuff
        private bool isDragging = false;
        private Vector2 dragOffset;
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

            textField = this.Q<TextField>("TextField");
            label = this.Q<Label>("Label");

            //textField.Children().First().style.display = DisplayStyle.None;

            //textField.RegisterCallback<KeyDownEvent>(evt =>
            //{
            //    if (evt.keyCode == KeyCode.Return)
            //    {
            //        if (evt.shiftKey)
            //        {
            //            // Insert line break
            //            int cursorIndex = textField.cursorIndex;
            //            string value = textField.value;
            //            textField.value = value.Insert(cursorIndex, "\n");
            //            textField.cursorIndex = cursorIndex + 1;
            //            evt.StopPropagation();
            //        }
            //        else
            //        {
            //            textField.Blur();
            //            evt.StopPropagation();
            //        }
            //    }
            //});

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                Note.Message = textField.value;
                label.text = Note.Message;
                label.style.display = DisplayStyle.Flex;
                textField.style.display = DisplayStyle.None;
                isEditing = false;
            });

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            //RegisterCallback<PointerEnterEvent>(evt => {Debug.Log("EnterFrame");});

            RegisterCallbackOnce<GeometryChangedEvent>(SetPos);
        }

        public LBSNoteView(LBSNote note, float width = 10, float height = 100) : this()
        {
            Note = note;

            style.width = width;
            style.height = height;
            style.flexGrow = 1;
        }

        private void SetPos(GeometryChangedEvent evt)
        {
            SetPosition(new Rect(MainView.Instance.FixPos(Note.Position), layout.size));

            textField.style.display = DisplayStyle.Flex;
            label.style.display = DisplayStyle.None;
            textField.value = Note.Message;
            textField.Focus();
            isEditing = true;
            isDragging = false;
        }

        #endregion

        #region MOUSE EVENTS

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return; // Only left mouse button, maybe with left click you could delete the note
            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                var time = (float)UnityEditor.EditorApplication.timeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold)
                {
                    // Double click: Edit note
                    
                    Debug.Log("Edit");

                    textField.style.display = DisplayStyle.Flex;
                    label.style.display = DisplayStyle.None;
                    textField.value = Note.Message;
                    textField.Focus();
                    isEditing = true;
                    isDragging = false;
                }
                else
                {
                    isDragging = true;
                    dragOffset = evt.localMousePosition;
                    this.CaptureMouse();
                }
                lastClickTime = time;
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0) return; // Only left mouse button, maybe with left click you could delete the note
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

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (evt.button != 0) return; // Only left mouse button, maybe with left click you could delete the note
            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                if (isDragging)
                {
                    if (parent != null)
                    {
                        Vector2 newPos = evt.mousePosition - dragOffset;
                        Note.Position = new Vector2(newPos.x ,newPos.y);
                        SetPosition(new Rect(MainView.Instance.FixPos(Note.Position), layout.size));

                        //style.left = newPos.x;
                        //style.top = newPos.y;
                    }
                }
            }
        }

        // Check what happens if the mouse leaves the editor window while dragging

        #endregion
    }
}
