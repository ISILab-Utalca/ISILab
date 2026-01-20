using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class RotateSchemaZone : LBSManipulator
    {
        private SchemaBehaviour _schema;
        protected override string IconGuid => "485afea6f40f10e41a28c3d016a9250b";

        public RotateSchemaZone()
        {
            Name = "Rotate Zone";
            Description = "Click on a zone to rotate it around its center.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            _schema = provider as SchemaBehaviour;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var pos = _schema.OwnerLayer.ToFixedPosition(endPosition);
            var tile = _schema.GetTile(pos);
            if (tile == null) return;

            var zone = _schema.GetZone(tile);
            if (zone == null) return;

            int direction = 0;
            if (e.button == 0) direction = -1; // Left click: rotate counter-clockwise
            else if (e.button == 1) direction = 1; // Right click: rotate clockwise

            if (direction != 0)
            {
                EditorGUI.BeginChangeCheck();

                Undo.RegisterCompleteObjectUndo(LBSController.CurrentLevel, "Rotate Zone");

                bool success = _schema.RotateZone(zone, direction);

                if (!success) Debug.Log("Cannot rotate zone: Collision detected.");

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(LBSController.CurrentLevel);
            }
        }

        //protected override void OnKeyDown(KeyDownEvent e) { }
    }
}