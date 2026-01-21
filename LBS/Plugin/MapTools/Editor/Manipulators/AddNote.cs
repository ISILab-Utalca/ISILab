using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.ViewElements;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class AddNote : LBSManipulator
    {
        protected override string IconGuid => "c301732b5aed09a4e8b1a4f165dc3515";

        public AddNote()
        {
            Name = "Add Note";
            Description = "Add a note to the map";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var target = element.panel.Pick(e.mousePosition);
            if (target is LBSNoteView || target.parent is LBSNoteView) return;

            var note = new LBSNote(endPosition, "Double Click to Write your Comment", LBSMainWindow.Instance._selectedLayer);

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Note");

            var noteBehaviour = note.OwnerLayer.GetBehaviour<NoteBehaviour>();
            noteBehaviour?.AddNote(note);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}