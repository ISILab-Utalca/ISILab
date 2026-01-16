using System;
using System.Linq;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Drawers;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.ViewElements;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;

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

                view.AddElementToLayerContainer(nb.OwnerLayer, note, noteView);
            }
        }

        public override void HideVisuals(object target, MainView view) 
        {
            if (target is not NoteBehaviour nb) return;

            foreach (LBSNote note in nb.Keys)
            {
                if (note == null) continue;

                var elements = view.GetElementsFromLayer(nb.OwnerLayer, note);
                foreach (var graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            if (target is not NoteBehaviour nb) return;

            foreach (LBSNote note in nb.Keys)
            {
                foreach (var graphElement in view.GetElementsFromLayer(nb.OwnerLayer, note).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}