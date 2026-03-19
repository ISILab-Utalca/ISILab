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
        private Vector2Int _first;
        private List<Vector2Int> Dirs => Commons.Directions.Bidimencional.Edges;

        private StairsMemoryLine feedbackStairsLine;
        private ConnectedMemoryLine feedbackMemoryLine;

        protected override string IconGuid => "b06c784e5d88d1547a40d4fc2f54b485";
        
        public string ToSet
        {
            get => _schema.conectionToSet;
            set => _schema.conectionToSet = value;
        }

        public AddSchemaTileConnection()
        {
            feedbackStairsLine = new StairsMemoryLine();
            feedbackStairsLine.fixToTeselation = true;
            feedbackMemoryLine = new ConnectedMemoryLine();
            feedbackMemoryLine.fixToTeselation = true;

            Feedback = feedbackStairsLine;
            OnManipulationNotification += () =>
            {
                if (ToSet == null) return;
                bool setStairs = ToSet.Contains("Stairs");
                if (setStairs)
                {
                    Feedback = feedbackStairsLine;
                }
                else
                {
                    Feedback = feedbackMemoryLine;
                }
            };

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

        protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
        {
            _first = _schema.OwnerLayer.ToFixedPosition(position);
            //*/
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int position, MouseUpEvent e)
        {
            base.OnMouseUp(element, position, e);
            ConnectedMemoryLine line = null;
            if(Feedback.GetType() == typeof(ConnectedMemoryLine) || Feedback.GetType() == typeof(StairsMemoryLine))
            {
                line = Feedback as ConnectedMemoryLine;
            }

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                ForceCancel = false;
                if (line != null) line.LineClear();
                return;
            }

            if (ToSet is null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Select a connection type in the LBS-inspector panel",LogType.Warning,4));
                return;
            }

            // Get second fixed position
            Vector2Int lastPos = _schema.OwnerLayer.ToFixedPosition(position);

            // Get vector direction
            int dx = lastPos.x - _first.x;
            int dy = lastPos.y - _first.y;
            
            float dLength = Mathf.Sqrt(dx * dx  +  dy * dy);
            if (line is null && dLength < 1) return;

            // Get index of directions
            int frontDirIndex = Dirs.FindIndex(d => d.Equals(new Vector2Int(Math.Sign(dx), Math.Sign(dy))));
            if (line is null && (frontDirIndex < 0 || frontDirIndex >= Dirs.Count)) return;
            int backDirIndex = Dirs.FindIndex(d => d.Equals(-new Vector2Int(Math.Sign(dx), Math.Sign(dy))));

            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Connection Between Zones");

            // Multi-connection mode
            bool requiresWall = dLength > 1;

            List<LBSTile> selectedTiles = new List<LBSTile>();
            if (line != null)
            {
                for (int i = 0; i < line.Line.Count; i++)
                {
                    var tile = _schema.GetTile(line.Line[i]);
                    selectedTiles.Add(tile);
                }
                line.LineClear();
            }
            else
            {
                int totalConnections = (int)Math.Floor(dLength);
                for (int i = 0; i <= totalConnections; i++)
                {
                    //Get the next tile 
                    selectedTiles.Add(GetTileInLine(_schema, i));
                }
            }

            for (int i = 1; i < selectedTiles.Count; i++)
            {
                LBSTile tile1 = selectedTiles[i - 1];
                LBSTile tile2 = selectedTiles[i];
                if (line != null)
                {
                    if (tile1 is null || tile2 is null) continue;

                    // Get vector direction
                    int bx = tile2.x - tile1.x;
                    int by = tile2.y - tile1.y;
                    
                    // Get index of directions
                    frontDirIndex = Dirs.FindIndex(d => d.Equals(new Vector2Int(Math.Sign(bx), Math.Sign(by))));
                    if (frontDirIndex < 0 || frontDirIndex >= Dirs.Count) continue; ;
                    backDirIndex = Dirs.FindIndex(d => d.Equals(-new Vector2Int(Math.Sign(bx), Math.Sign(by))));
                }


                bool setDoorOrWindow = ToSet.Equals("Door") || ToSet.Equals("Window");
                bool setStairs = ToSet.Contains("Stairs");
                if (requiresWall && setDoorOrWindow && !ValidWallReplace(_schema, tile1, tile2)) continue;
                if (requiresWall && setStairs && !ValidStairsPlacement(_schema, tile1, tile2)) continue;

                TrySetSingleConnection(_schema, tile1, tile2, frontDirIndex, backDirIndex);
                
                if (_schema.MultiLayerConnections && setDoorOrWindow)
                {
                    foreach (SchemaBehaviour other in _others)
                    {
                        LBSTile t1 = GetTileInLine(other, i - 1);
                        LBSTile t2 = GetTileInLine(other, i);
                        if (ValidWallReplace(other, t1, t2))
                        {
                            TrySetSingleConnection(other, t1, t2, frontDirIndex, backDirIndex);
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

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }

            /// END OF METHOD ///

            // Local functions
            LBSTile GetTileInLine(SchemaBehaviour schema, int i) => schema.GetTile(_first + new Vector2Int(Math.Sign(dx) * i, Math.Sign(dy) * i));

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

            bool ValidStairsPlacement(SchemaBehaviour schema, LBSTile tile1, LBSTile tile2)
            {
                bool tile1Exists = tile1 is not null;
                bool tile2Exists = tile2 is not null;

                // Identificar si es escalera que sube o baja
                bool up = ToSet.Contains("Up");

                // Si es escalera que sube, revisar la posición de tile1 en el floor siguiente (podría no existir)
                if (up)
                {
                    int nextFloorIndex = schema.OwnerLayer.ActiveFloor + 1;
                    bool floorExists = nextFloorIndex >= schema.OwnerLayer.FloorCount;
                    if (floorExists)
                    {
                        ConnectedTileMapModule tileConnections = 
                            schema.OwnerLayer.Modules(nextFloorIndex).FirstOrDefault(m => m.GetType() == typeof(ConnectedTileMapModule)) 
                            as ConnectedTileMapModule;

                        var nTile1 = tileConnections.GetPair(tile1);
                        //nTile1.
                    }
                }
                // También revisar que exista una zona en el la posición de tile2 en el siguiente floor
                // Si no existe, crear nueva zone con ese tile

                // Si es escalera que baja, revisar la posición de tile1 en el floor anterior (podría no existir)
                // También revisar que exista una zona en el la posición de tile2 en el anterior floor
                // Si no existe, crear nueva zone con ese tile
                
                return true;
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