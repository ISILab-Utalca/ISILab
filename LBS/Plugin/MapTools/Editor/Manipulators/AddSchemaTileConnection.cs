using ISILab.LBS.Editor.Windows;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Modules;

namespace ISILab.LBS.Manipulators
{
    public class AddSchemaTileConnection : LBSManipulator
    {
        private SchemaBehaviour _schema;
        private List<SchemaBehaviour> _others;

        private List<Vector2Int> Dirs => Commons.Directions.Bidimencional.Edges;
        private ConnectedMemoryLine _line;

        protected override string IconGuid => "b06c784e5d88d1547a40d4fc2f54b485";
        
        public string ToSet
        {
            get => _schema.conectionToSet;
            set => _schema.conectionToSet = value;
        }

        public AddSchemaTileConnection()
        {
            _line = new ConnectedMemoryLine();
            _line.fixToTeselation = true;
            Feedback = _line;

            Name = "Set Manual Connection";
            Description = "Draw across a zone's border to generate a connection.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            //Debug.Log("Tile connection Init");
            base.Init(layer, provider);
            
            _schema = provider as SchemaBehaviour;
            if(_schema.MultiLayerConnections) 
                MultiLayerSetup();

            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        public void MultiLayerSetup()
        {
            _others = LBSController.CurrentLevel.data.Layers
                .Select(l => l.GetBehaviour<SchemaBehaviour>())
                .Where(b => b is not null && b != _schema)
                .ToList();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int position, MouseUpEvent e)
        {
            base.OnMouseUp(element, position, e);

            // Cancel the operation
            // If esc key was pressed OR a connection wasn't selected
            if (ForceCancel)
            {
                ForceCancel = false;
                _line.LineClear();
                return;
            }
            if (ToSet is null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Select a connection type in the LBS-inspector panel",LogType.Warning,4));
                _line.LineClear();
                return;
            }

            // Get selected tiles
            List<LBSTile> selectedTiles = new List<LBSTile>();
            for (int i = 0; i < _line.Positions.Count; i++)
            {
                var tile = _schema.GetTile(_line.Positions[i]);
                selectedTiles.Add(tile);
            }
            bool requiresWall = _line.Positions.Count > 1;

            // Set Undo action
            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Connection Between Zones");

            // Filter connection behaviour
            bool setDoorOrWindow = ToSet.Equals("Door") || ToSet.Equals("Window");

            // Set tile connections
            int frontDirIndex, backDirIndex;
            for (int i = 1; i < selectedTiles.Count; i++)
            {
                LBSTile t1 = selectedTiles[i - 1];
                LBSTile t2 = selectedTiles[i];

                // Get direction vector
                int dx = _line.Positions[i].x - _line.Positions[i - 1].x;
                int dy = _line.Positions[i].y - _line.Positions[i - 1].y;

                // Get index of directions
                frontDirIndex = Dirs.FindIndex(d => d.Equals(new Vector2Int(Math.Sign(dx), Math.Sign(dy))));
                if (frontDirIndex < 0 || frontDirIndex >= Dirs.Count) continue; ;
                backDirIndex = Dirs.FindIndex(d => d.Equals(-new Vector2Int(Math.Sign(dx), Math.Sign(dy))));

                // Validate position
                if (requiresWall && setDoorOrWindow && !ValidWallReplace(_schema, t1, t2)) continue;
                TrySetSingleConnection(_schema, t1, t2, frontDirIndex, backDirIndex);
                
                // Spread change to other layers
                if (_schema.MultiLayerConnections && setDoorOrWindow)
                {
                    foreach (SchemaBehaviour other in _others)
                    {
                        LBSTile t3 = other.GetTile(t1.Position);
                        LBSTile t4 = other.GetTile(t2.Position);
                        if (ValidWallReplace(other, t3, t4))
                        {
                            TrySetSingleConnection(other, t3, t4, frontDirIndex, backDirIndex);
                            Action redrawCallback = null;
                            redrawCallback = () =>
                            {
                                DrawManager.Instance.RedrawLayer(other.OwnerLayer);
                                OnManipulationEnd -= redrawCallback;
                            };
                            OnManipulationEnd += redrawCallback;
                            break; // The multilayer connections feature was planned for adjacent rooms of two different layers. It is not expected to set the same connection in more than 2 layers.
                        }
                    }
                }
            }
            _line.LineClear();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }

            /// END OF METHOD ///
            bool ValidWallReplace(SchemaBehaviour schema, LBSTile tile1, LBSTile tile2)
            {
                bool tile1Exists = tile1 is not null;
                bool tile2Exists = tile2 is not null;
                string conn1 = tile1Exists ? schema.GetConnections(tile1)[frontDirIndex] : "Empty";
                string conn2 = tile2Exists ? schema.GetConnections(tile2)[backDirIndex] : "Empty";
                bool firstHasWall = !conn1.Equals("Empty");
                bool secondHasWall = !conn2.Equals("Empty");
                return (firstHasWall || secondHasWall) && (tile1Exists || secondHasWall) && (tile2Exists || firstHasWall);
            }
        }

        private void TrySetSingleConnection(
            SchemaBehaviour schema,
            LBSTile firstTile,
            LBSTile secondTile,
            int frontDirIndex,
            int backDirIndex
            )
        {

            if (firstTile is null && secondTile is null) return;
            if (firstTile is not null)
            {
                if (firstTile.Equals(secondTile))
                {
                    Debug.Log("Not Valid Tile - Same Tile with lenght 0");
                    return;
                }
                schema.SetConnection(firstTile, frontDirIndex, ToSet, false);
            }
            if (secondTile is not null)
            {
                schema.SetConnection(secondTile, backDirIndex, ToSet, false);
            }
            return;
        }
    }
}