using ISILab.LBS.Assistants;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using System.Collections.Generic;
using UnityEngine;


public class BSPDungeonAssistant : LBSAssistant
{
    #region FIELDS
    BSPDungeonGenerator _generator;
    Dictionary<int, Zone> _zoneDict = new();
    SectorizedTileMapModule _sectorizedModule;
    SchemaBehaviour _schemaBehaviour;

    [Header("BSP Settings")]
    int mapWidth = 50;
    public int mapHeight = 50;
    public int minPartitionSize = 10;
    public int minRoomSize = 4;
    #endregion

    #region PROPERTIES
    public BSPDungeonGenerator Generator
    {
        get { return _generator ??= new(); }
    }
    public Dictionary<int, Zone> ZoneDict
    {
        get { return _zoneDict ??= new(); }
    }
    public SectorizedTileMapModule Sectorized
    {
        get { return _sectorizedModule ??= OwnerLayer.GetModule<SectorizedTileMapModule>(); }
    }
    public SchemaBehaviour Schema
    {
        get { return _schemaBehaviour ??= OwnerLayer.GetBehaviour<SchemaBehaviour>(); }
    }
    #endregion


    public BSPDungeonAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }

    public void Run()
    {
        ZoneDict.Clear();
        List<LBSTile> _newTiles = new();
        int[,] mapData = Generator.Generate(mapWidth, mapHeight, minPartitionSize, minRoomSize);

        for(int i = 0; i < mapData.GetLength(0); i++)
        {
            for(int j = 0; j < mapData.GetLength(1); j++)
            {
                int key = mapData[i, j];
                if (key < 1) continue;

                Zone value;

                if (!ZoneDict.ContainsKey(key))
                {
                    value = Schema.AddZone();
                    ZoneDict[key] = value;
                }
                else
                {
                    value = ZoneDict[key];
                }

                LBSTile t = Schema.AddTile(new Vector2Int(i, j), value);
                if (t != null) Schema.AddConnections(t, SchemaBehaviour.DefaultConnections, 
                    new List<bool> { true, true, true, true });
                _newTiles.Add(t);
            }
        }
        Schema.RecalculateWalls();
        //Schema.RequestFullRepaint(new(), _newTiles);
        Schema.OwnerLayer.Reload();
    }


    public override object Clone()
    {
        return new BSPDungeonAssistant(IconGuid, Name, ColorTint);
    }

    public override void OnGUI() { Debug.Log("[BSPDungeonAssistant]: OnGUI"); }
}