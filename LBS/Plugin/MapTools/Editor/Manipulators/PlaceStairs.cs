using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaceStairs : LBSManipulator
{
    override protected string IconGuid => "103cf2403fa02574fb824cdb84514eb9";

    private StairsMemoryLine line;
    private SchemaBehaviour _schema;

    public PlaceStairs() 
    {
        line = new StairsMemoryLine();
        Feedback = line;
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
            line.LineClear();
            return;
        }
        if (line.IsValid)
        {
            List<Vector2Int> positions = line.Positions;
            if (!ValidatePositions(positions))
            {
                line.LineClear(); 
                return;
            }
                

        }
        line.LineClear();
    }

    private bool ValidatePositions(List<Vector2Int> positions, int floor = -1)
    {
        string[] empty = { "Empty" };

        // Find ConnectedTileMapModule in floor
        ConnectedTileMapModule connectedModule = null;
        var fModules = Layer.Modules(floor);
        foreach (var module in fModules)
        {
            if (module.GetType() != typeof(ConnectedTileMapModule)) continue;
            connectedModule = (ConnectedTileMapModule) module;
        }
        if (connectedModule is null) return true;

        // Check if there are connections between positions
        var dirs = ISILab.Commons.Directions.Bidimencional.Edges;
        for (int i = 1; i < positions.Count; i++)
        {
            var t1 = connectedModule.GetPair(positions[i-1]);
            var t2 = connectedModule.GetPair(positions[i]);
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

}
