using ISILab.LBS;
using ISILab.LBS.Assistants;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    public class BSPDungeonAssistant : LBSAssistant
    {
        #region FIELDS
        BSPDungeonGenerator _generator;
        Dictionary<int, Zone> _zoneDict = new();
        SectorizedTileMapModule _sectorizedModule;
        SchemaBehaviour _schemaBehaviour;

        [Header("BSP Settings")]
        public int mapWidth = 50;
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

        #region EVENTS
        public event Action changeCallback;
        #endregion


        public BSPDungeonAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }

        public void RunSynced()
        {
            // Init
            ZoneDict.Clear();
            int[,] mapData = Generator.Generate(mapWidth, mapHeight, minPartitionSize, minRoomSize);

            // Read mapData
            for (int i = 0; i < mapData.GetLength(0); i++)
            {
                for (int j = 0; j < mapData.GetLength(1); j++)
                {
                    // Ignore if 0
                    int key = mapData[i, j];
                    if (key < 1) continue;

                    // Get or create Zone
                    Zone value = ZoneDict.ContainsKey(key) ? ZoneDict[key] : ZoneDict[key] = Schema.AddZone();

                    // Add new Tile and it's connections
                    LBSTile t = Schema.AddTile(new Vector2Int(i, j), value);
                    if (t != null) Schema.AddConnections(t, SchemaBehaviour.DefaultConnections,
                        new List<bool> { true, true, true, true });
                }
            }

            // Closing and drawing
            Schema.RecalculateWalls();
            DrawManager.Instance.RedrawLevel(LBS.loadedLevel.data);
            LBSMainWindow.Instance.layerPanel.SetSelectedLayer(Schema.OwnerLayer);
        }

        public void RunAsync(Action<float> onProgress = null, CancellationToken token = default)
        {
            // Init
            ZoneDict.Clear();
            int[,] mapData = Generator.Generate(mapWidth, mapHeight, minPartitionSize, minRoomSize, true);

            // Read mapData
            int w = mapData.GetLength(0);
            int h = mapData.GetLength(1);
            for (int i = 0; i < mapData.GetLength(0); i++)
            {
                for (int j = 0; j < mapData.GetLength(1); j++)
                {
                    // Ignore if 0
                    int key = mapData[i, j];
                    if (key < 1) continue;

                    // Get or create Zone
                    Zone value = ZoneDict.ContainsKey(key) ? ZoneDict[key] : ZoneDict[key] = Schema.AddZone(true);

                    // Add new Tile and it's connections
                    LBSTile t = Schema.AddTile(new Vector2Int(i, j), value);
                    if (t != null) Schema.AddConnections(t, SchemaBehaviour.DefaultConnections,
                        new List<bool> { true, true, true, true });

                    // Update progress bar
                    onProgress?.Invoke((float) ((i * w) + j) / (w * h));
                }
            }
            // Update progress bar
            onProgress?.Invoke(1);
            Thread.Sleep(1);
        }

        public override object Clone()
        {
            return new BSPDungeonAssistant(IconGuid, Name, ColorTint);
        }

        public override void OnGUI() { }

    }
}