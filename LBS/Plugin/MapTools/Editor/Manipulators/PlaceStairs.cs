using ISILab.Commons;
using ISILab.LBS;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.Manipulators
{
    public class PlaceStairs : LBSManipulator
    {
        override protected string IconGuid => "103cf2403fa02574fb824cdb84514eb9";

        private SchemaBehaviour _schema;
        private ConnectedMemoryLine _line;
        private bool _downwards = false;

        public PlaceStairs()
        {
            _line = new ConnectedMemoryLine();
            Feedback = _line;
            Feedback.fixToTeselation = true;

            Name = "Place stairs";
            Description = "Hold ALT to place downwards stairs. Use Right click to remove stairs.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            _schema = provider as SchemaBehaviour;

            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            if (ForceCancel)
            {
                ForceCancel = false;
                _line.LineClear();
                return;
            }

            // Set Undo action
            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Place Stairs");

            List<Vector2Int> positions = _line.Positions;
            if (positions.Count < 2)
            {
                _line.LineClear();
                return;
            }

            // Validate stairs
            bool validated = false;
            validated = ValidatePositions(positions, Layer.ActiveFloor);
            int adyacentFloor = _downwards ? Layer.ActiveFloor - 1 : Layer.ActiveFloor + 1;
            if (validated) validated = ValidatePositions(positions, adyacentFloor);

            if (!validated)
            {
                _line.LineClear();
                return;
            }

            // Clear connections in between
            List<LBSTile> selectedTiles = new();
            List<LBSTile> adyacentSelectedTiles = new();
            var connectionMod = Layer.GetModule<ConnectedTileMapModule>();
            var adyacentConnectionMod = Layer.GetModule<ConnectedTileMapModule>("", adyacentFloor);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int position = positions[i];
                Vector2Int? prevDir = null;
                Vector2Int? nextDir = null;
                int? pDirIndex = null;
                int? nDirIndex = null;

                if (i > 0) prevDir = positions[i - 1] - position;
                if (i < positions.Count - 1) nextDir = positions[i + 1] - position;

                if (prevDir is not null)
                {
                    pDirIndex = Directions.Bidimencional.Edges.FindIndex(v => v == prevDir);
                }
                if (nextDir is not null)
                {
                    nDirIndex = Directions.Bidimencional.Edges.FindIndex(v => v == nextDir);
                }

                TileConnectionsPair pair = connectionMod.GetPair(position);
                if (pair != null)
                {
                    selectedTiles.Add(pair.Tile);
                    if (pDirIndex != null) pair.SetConnection(pDirIndex.Value, "Empty", true);
                    if (nDirIndex != null) pair.SetConnection(nDirIndex.Value, "Empty", true);
                }

                pair = adyacentConnectionMod.GetPair(position);
                if (pair != null)
                {
                    adyacentSelectedTiles.Add(pair.Tile);
                    if (pDirIndex != null) pair.SetConnection(pDirIndex.Value, "Empty", true);
                    if (nDirIndex != null) pair.SetConnection(nDirIndex.Value, "Empty", true);
                }
            }
            _schema.RecalculateWalls(selectedTiles.Where(t => t is not null).ToList());
            _schema.RecalculateWallsAtFloor(adyacentFloor, adyacentSelectedTiles.Where(t => t is not null).ToList());


            // Set stairs in both floors
            LBSStair upStairs, downStairs;
            if (_downwards)
            {
                upStairs = new LBSStair(positions, adyacentFloor, Layer.ActiveFloor, 1, StairShape.None);
                downStairs = new LBSStair(positions, adyacentFloor, Layer.ActiveFloor, -1, StairShape.None);

                _schema.PlaceStair(downStairs, Layer.ActiveFloor);
                _schema.PlaceStair(upStairs, adyacentFloor);
            }
            else
            {
                upStairs = new LBSStair(positions, adyacentFloor, Layer.ActiveFloor, 1, StairShape.None);
                downStairs = new LBSStair(positions, adyacentFloor, Layer.ActiveFloor, -1, StairShape.None);

                _schema.PlaceStair(downStairs, adyacentFloor);
                _schema.PlaceStair(upStairs, Layer.ActiveFloor);
            }

            // Get styles from zone if avaliable
            SectorizedTileMapModule sectorMod = _downwards ? 
                Layer.GetModule<SectorizedTileMapModule>("", adyacentFloor) : 
                Layer.GetModule<SectorizedTileMapModule>();

            if (sectorMod != null)
            {
                var firstTile = sectorMod.GetPairTile(positions[0]);
                if(firstTile != null && firstTile.Zone != null)
                {
                    upStairs.Styles = firstTile.Zone.InsideStyles;
                }
            }
            if(upStairs.Styles == null || upStairs.Styles.Count < 1)
            {
                upStairs.Styles = new List<string>() { _schema.PressetInsideStyle.Name };
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
            _line.LineClear();
        }

        private bool ValidatePositions(List<Vector2Int> positions, int floor = -1)
        {
            // Check if floor is out of limits
            if (floor >= Layer.FloorCount || floor < 0)
            {
                string direction = _downwards ? "downwards" : "backwards";
                new LBSLog($"[PlaceStairs]: Can't place {direction} stairs in floor " +
                    $"({Layer.ActiveFloor}) because it would be out of bounds.", LogType.Error, 8);
                return false;
            }

            // Find StairsModule
            var stairs = Layer.Modules(floor).FirstOrDefault(
                m => m.GetType() == typeof(StairsModule)) as StairsModule;
            if (stairs is null) return true;

            // Check if any position is occupied by another stair
            foreach (var pos in positions)
            {
                if (!stairs.IsPositionOccupied(pos)) continue;
                LBSMainWindow.MessageNotify(
                    new LBSLog("Can't place stairs in position " +
                    $"({pos.x},{pos.y}) because there's already a stair.", LogType.Error, 8));
                return false;
            }

            // Find ConnectedTileMapModule
            ConnectedTileMapModule connected = Layer.Modules(floor).FirstOrDefault(
                m => m.GetType() == typeof(ConnectedTileMapModule)) as ConnectedTileMapModule;
            if (connected is null) return true;
            return true;
        }


        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);
            if (e.ctrlKey)
            {
                _downwards = true;
                LBSMainWindow.WarningManipulator("(CTRL) Placing downwards stair");
            }
        }
        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);
            if (!e.ctrlKey)
            {
                _downwards = false;
                LBSMainWindow.WarningManipulator();
            }
        }

    }
}