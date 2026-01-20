using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
   
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class SelectManipulator : LBSManipulator
    {
        private LBSLocalCurrent _current;

        protected override string IconGuid => "77f81c1ea560ddf4c99e41c605166e3e";

        public SelectManipulator()
        {
            // Unset feedback
            Feedback = null;
            _current = LBSInspectorPanel.Instance.data;

            Name = "Select";
            Description = "Selection";
        }
        
        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            // Set provider reference
            _current = provider as LBSLocalCurrent;
        }

        protected override void UpdateView() { } // Do not redraw level

        protected override void OnMouseUp(VisualElement element, Vector2Int position, MouseUpEvent e)
        {
            LBSInspectorPanel.Instance.CallSelectableByPosition(LBSLayer, position);
        }
    }
}