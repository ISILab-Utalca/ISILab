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

        const float DEFAULT_WIDTH = 200;
        const float DEFAULT_HEIGHT = 200;
            
        
        
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
            this.style.width = DEFAULT_WIDTH;
            this.style.height = DEFAULT_HEIGHT;
            AddToClassList("lbs-post-it");
        }

        public LBSNoteView(LBSNote note, float width = 100, float height = 100) : this()
        {
            asset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSNoteView");
            asset.CloneTree(this);

            textField = this.Q<TextField>("TextField");
            label = this.Q<Label>("Label");

            textField.Children().First().style.display = DisplayStyle.None;

            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    if (evt.shiftKey)
                    {
                        // Insert line break
                        int cursorIndex = textField.cursorIndex;
                        string value = textField.value;
                        textField.value = value.Insert(cursorIndex, "\n");
                        textField.cursorIndex = cursorIndex + 1;
                        evt.StopPropagation();
                    }
                    else
                    {
                        textField.Blur();
                        evt.StopPropagation();
                    }
                }
            });

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                Note.Message = textField.value;
                label.text = Note.Message;
                label.style.display = DisplayStyle.Flex;
                textField.style.display = DisplayStyle.None;
            });

            this.RegisterCallback<MouseDownEvent>(OnMouseDown);
            label.RegisterCallback<MouseUpEvent>(OnMouseUp);
            label.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<PointerEnterEvent>(evt => {Debug.Log("EnterFrame");});
            

            RegisterCallbackOnce<GeometryChangedEvent>(SetPos);
        }

        public LBSNoteView(LBSNote note, float width = 100, float height = 100) : this()
        {
            Note = note;

            this.style.minWidth = width;
            this.style.minHeight = height;
            this.style.flexGrow = 1;

            textField.style.display = DisplayStyle.Flex;
            label.style.display = DisplayStyle.None;
            textField.value = Note.Message;
            textField.Focus();
            isDragging = false;
        }

        private void SetPos(GeometryChangedEvent evt)
        {
            SetPosition(new Rect(MainView.Instance.FixPos(Note.Position), layout.size));
        }

        #endregion

        #region MOUSE EVENTS

        private void OnMouseDown(MouseDownEvent evt)
        {
            Debug.Log("Hola");

            if (evt.button != 0) return; // Only left mouse button, maybe with left click you could delete the note
            if (ToolKit.Instance.GetActiveManipulator() is null) return;

            if (ToolKit.Instance.GetActiveManipulator().GetType() == typeof(SelectManipulator))
            {
                float time = Time.realtimeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold)
                {
                    // Double click: Edit note
                    textField.style.display = DisplayStyle.Flex;
                    label.style.display = DisplayStyle.None;
                    textField.value = Note.Message;
                    textField.Focus();
                    isDragging = false;
                }
                else
                {
                    // Single click: Start dragging
                    if (ToolKit.Instance.GetActiveManipulatorInstance() is SelectManipulator)
                    {
                        isDragging = true;
                        dragOffset = evt.localMousePosition;
                        this.CaptureMouse();
                    }
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
                        style.left = newPos.x;
                        style.top = newPos.y;
                        Note.Position = new Vector2Int(Mathf.RoundToInt(newPos.x), Mathf.RoundToInt(newPos.y));
                    }
                }
            }
        }

        // Check what happens if the mouse leaves the editor window while dragging

        #endregion
    }
}
