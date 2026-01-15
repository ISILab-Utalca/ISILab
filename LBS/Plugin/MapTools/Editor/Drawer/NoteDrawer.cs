using System;
using System.Linq;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Drawers;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.ViewElements;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(NoteBehaviour))]
    public class NoteDrawer : Drawer
    {
        private Color color;

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not NoteBehaviour nb) return;

            foreach (var note in nb.Notes)
            {
                var existing = view.GetElementsFromLayer(nb.OwnerLayer, note)?.FirstOrDefault();
                if (existing != null) continue;

                var noteView = new LBSNoteView(note);

                var canvasPos = view.FixPos(note.Position);
                noteView.SetPosition(new Rect(canvasPos, noteView.layout.size));

                view.AddElementToLayerContainer(nb.OwnerLayer, note, noteView);
            }
        }

        public override void HideVisuals(object target, MainView view) { }

        public override void ShowVisuals(object target, MainView view) { }
    }
}