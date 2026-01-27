using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.UI.Editor.ViewElements;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class RemoveNote : LBSManipulator
    {
        protected override string IconGuid => "a57f2767b5141524ea6c0ae9b682346e";

        public RemoveNote()
        {
            Name = "Remove Note";
            Description = "Remove a note from the map";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var target = element.panel.Pick(e.mousePosition);

            if (target is not LBSNoteView noteView)
                return;

            var note = noteView.Note;
            if (note == null)
                return;

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Note");

            var noteBehaviour = note.OwnerLayer.GetBehaviour<NoteBehaviour>();
            noteBehaviour?.RemoveNote(note);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}