using ISILab.LBS.Manipulators;
using ISILab.LBS;
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

public class PlaceStairs : LBSManipulator
{
    override protected string IconGuid => "103cf2403fa02574fb824cdb84514eb9";

    private SchemaBehaviour _schema;
    private StairsMemoryLine _line;
    private bool _downwards = false;

    public PlaceStairs() 
    {
        _line = new StairsMemoryLine();
        Feedback = _line;
        Feedback.fixToTeselation = true;

        Name = "Place stairs";
        Description =
            "Supported shapes: straight line, corner, U-shape and S-shape. " + 
            "Hold ALT to place downwards stairs.";
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
        if (_line.IsValid)
        {

            // Set Undo action
            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Place Stairs");

            List<Vector2Int> positions = _line.Positions;

            // Validate stairs
            bool validated = false;
            validated = ValidatePositions(positions, Layer.ActiveFloor);
            int adyacent = _downwards ? Layer.ActiveFloor - 1 : Layer.ActiveFloor + 1;
            if (validated) validated = ValidatePositions(positions, adyacent);

            if (!validated)
            {
                _line.LineClear(); 
                return;
            }

            // Set stairs in both floors
            LBSStair upStairs, downStairs;
            if (_downwards)
            {
                upStairs = new LBSStair(positions, adyacent, Layer.ActiveFloor, 1, _line.Shape);
                downStairs = new LBSStair(positions, adyacent, Layer.ActiveFloor, -1, _line.Shape);

                _schema.PlaceStair(downStairs, Layer.ActiveFloor);
                _schema.PlaceStair(upStairs, adyacent);
            }
            else
            {
                upStairs = new LBSStair(positions, Layer.ActiveFloor, adyacent, 1, _line.Shape);
                downStairs = new LBSStair(positions, Layer.ActiveFloor, adyacent, -1, _line.Shape);

                _schema.PlaceStair(downStairs, adyacent);
                _schema.PlaceStair(upStairs, Layer.ActiveFloor);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
        _line.LineClear();
    }

    private bool ValidatePositions(List<Vector2Int> positions, int floor = -1)
    {
        // Check if floor is out of limits
        if (floor >= Layer.FloorCount || floor < 0)
        {
            string direction = _downwards ? "downwards" : "backwards";
            Debug.LogError($"[PlaceStairs]: Can't place {direction} stairs in floor " +
                $"({Layer.ActiveFloor}) because it would be out of bounds.");
            return false;
        }

        // Find StairsModule
        var stairs = Layer.Modules().FirstOrDefault(
            m => m.GetType() == typeof(StairsModule)) as StairsModule;
        if (stairs is null) return true;

        // Check if any position is occupied by another stair
        foreach (var pos in positions)
        {
            if (!stairs.IsPositionOccupied(pos)) continue;
            Debug.LogError("[PlaceStairs]: Can't place stairs in position " +
                $"({pos.x},{pos.y}) because there's already a stair.");
            return false;
        }

        // Find ConnectedTileMapModule
        ConnectedTileMapModule connected = Layer.Modules().FirstOrDefault(
            m => m.GetType() == typeof(ConnectedTileMapModule)) as ConnectedTileMapModule;
        if (connected is null) return true;

        // Check if there are connections between positions
        var dirs = ISILab.Commons.Directions.Bidimencional.Edges;
        for (int i = 1; i < positions.Count; i++)
        {
            var t1 = connected.GetPair(positions[i-1]);
            var t2 = connected.GetPair(positions[i]);
            if (t1 is null || t2 is null) continue;

            int dir = dirs.FindIndex(d => d.Equals(positions[i] - positions[i - 1]));
            int invDir = dirs.FindIndex(d => d.Equals(positions[i - 1] - positions[i]));
            if (t1.Connections[dir] != "Empty" || t2.Connections[invDir] != "Empty")
            {
                Debug.LogError("[PlaceStairs]: Can't place stairs between positions " +
                    $"({t1.Tile.x},{t1.Tile.y}) and ({t2.Tile.x},{t2.Tile.y}) " +
                    "because there's a connection in the middle.");
                return false;
            }

        }
        return true;
    }


    protected override void OnKeyDown(KeyDownEvent e)
    {
        base.OnKeyDown(e);
        if (e.keyCode == KeyCode.LeftAlt)
        {
            _downwards = true;
        }
    }
    protected override void OnKeyUp(KeyUpEvent e)
    {
        base.OnKeyUp(e);
        if (e.keyCode == KeyCode.LeftAlt)
        {
            _downwards = false;
        }
    }

}
