using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class NoteManipulator : LBSManipulator
    {
        protected override string IconGuid => "1280a33d1238d2b4d8e037976f245072";

        public NoteManipulator()
        {
            Name = "Note";
            Description = "Add a note to the map";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var note = new LBSNote(endPosition, "Write your comment");

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Note");

            var noteBehaviour = LBSMainWindow.Instance._selectedLayer.GetBehaviour<NoteBehaviour>();
            noteBehaviour?.AddNote(note);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}