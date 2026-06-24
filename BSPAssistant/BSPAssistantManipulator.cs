using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab
{
    public class BSPAssistantManipulator : ManipulateTeselation
    {
        private Vector2Int _cornerStart;

        private BSPAssistant _assistant;

        protected override string IconGuid => "08c60bd0a76e4bb4dad11ebf18bca46e";

        public BSPAssistantManipulator()
        {
            Feedback.fixToTeselation = true;
            Name = "BSP Dungeon Generator";
            Description = "Select an area to generate a dungeon usign the Binary Space Partition algorithm.";
        }

        public override void Init(LBSLayer layer, object owner)
        {
            base.Init(layer, owner);
            _assistant = owner as BSPAssistant;
        }
        protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
        {
            _cornerStart = position;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                ForceCancel = false;
                return;
            }

            var corners = _assistant.OwnerLayer.ToFixedPosition(_cornerStart, endPosition);
            _assistant.origin = new(
                Mathf.Min(_cornerStart.x, endPosition.x),
                Mathf.Min(_cornerStart.y, endPosition.y)
            );
            _assistant.mapWidth = Mathf.Abs(endPosition.x - _cornerStart.x);
            _assistant.mapHeight = Mathf.Abs(endPosition.y - _cornerStart.y );

        }
    }
}
