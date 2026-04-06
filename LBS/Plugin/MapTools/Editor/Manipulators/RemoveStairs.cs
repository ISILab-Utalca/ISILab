using ISILab.Commons;
using ISILab.LBS;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.Manipulators
{
    public class RemoveStairs : LBSManipulator
    {
        override protected string IconGuid => "286d1f7a07ec7924297cfd915095e8e1";

        private SchemaBehaviour _schema;
        private List<SchemaBehaviour> _others = new();

        public RemoveStairs()
        {
            Feedback = new AreaFeedback();
            Feedback.fixToTeselation = true;

            Name = "Remove stairs";
            Description = "Click on a stair to remove it.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            _schema = provider as SchemaBehaviour;

            // Searches for other layers to remove stairs from
            // Pretty much usable, but should require its own toggle instead of _schema.MultiLayerConnections
            /*if (_schema.MultiLayerConnections)
                _others = LBSController.CurrentLevel.data.Layers
                    .Select(l => l.GetBehaviour<SchemaBehaviour>())
                    .Where(b => b is not null && b != _schema)
                    .ToList();*/

            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);
            LBSMainWindow.WarningManipulator();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            if (ForceCancel)
            {
                ForceCancel = false;
                return;
            }

            // Set Undo action
            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Stairs");

            List<SchemaBehaviour> allBehaviours = new();
            HashSet<StairsModule> toUpdate = new();
            allBehaviours.Add(_schema);

            // Search for stair modules in other layers
            if (_schema.MultiLayerConnections)
            {
                foreach (var o in _others)
                {
                    allBehaviours.Add(o);
                }
            }

            // Remove stairs
            var corners = _schema.OwnerLayer.ToFixedPosition(StartPosition, EndPosition);
            foreach (var schema in allBehaviours)
            {
                foreach (var stair in schema.Stairs)
                {
                    foreach (var pos in stair.Positions)
                    {
                        if (corners.min.x <= pos.x && pos.x <= corners.max.x &&
                            corners.min.y <= pos.y && pos.y <= corners.max.y)
                        {
                            schema.RemoveStair(stair);
                            break;
                        }
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}