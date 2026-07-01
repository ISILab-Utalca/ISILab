using ISILab.LBS;
using ISILab.LBS.Assistants;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.UI.Image;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    public class BSPAssistant : LBSAssistant, IAssistantThreaded
    {
        #region FIELDS
        BSPDungeonGenerator _generator;
        Dictionary<int, Zone> _zoneDict = new();
        SchemaBehaviour _schemaBehaviour;

        private RectInt _area = new (0,0,50,50);
        public int minPartitionSize = 10;
        public int minRoomSize = 4;
        #endregion

        #region PROPERTIES
        public RectInt Area
        {
            set
            {
                if (value == _area) return;
                _area = value;
                changeCallback?.Invoke();
            }
            get { return _area; }
        }

        public BSPDungeonGenerator Generator
        {
            get { return _generator ??= new(); }
        }
        public Dictionary<int, Zone> ZoneDict
        {
            get { return _zoneDict ??= new(); }
        }
        public SchemaBehaviour Schema
        {
            get { return _schemaBehaviour ??= OwnerLayer.GetBehaviour<SchemaBehaviour>(); }
        }
        #endregion

        #region EVENTS
        public event Action changeCallback;
        #endregion

        // Constructor (MUST HAVE)
        public BSPAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }


        // Dungeon Generator Methods
        public void RunSynced()
        {
            // Init
            ZoneDict.Clear();
            int[,] mapData = Generator.Generate(Area.width, Area.height, minPartitionSize, minRoomSize);

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
                    LBSTile t = Schema.AddTile(new Vector2Int(Area.x + i, Area.y + j), value);
                    if (t != null) Schema.AddConnections(t, SchemaBehaviour.DefaultConnections,
                        new List<bool> { true, true, true, true });
                }
            }

            // Closing and drawing
            Schema.RecalculateWalls();
            DrawManager.Instance.RedrawLevel(LBS.loadedLevel.data);
            LBSMainWindow.Instance.layerPanel.SetSelectedLayer(Schema.OwnerLayer);
        }

        public void RunAsync(string insideStyle, string outsideStyle, Action<float> onProgress = null, CancellationToken token = default)
        {
            // Init
            ZoneDict.Clear();
            int[,] mapData = Generator.Generate(Area.width, Area.height, minPartitionSize, minRoomSize);

            // Read mapData
            int w = mapData.GetLength(0);
            int h = mapData.GetLength(1);
            for (int i = 0; i < mapData.GetLength(0); i++)
            {
                for (int j = 0; j < mapData.GetLength(1); j++)
                {
                    // Cancel Check
                    if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                        return;

                    // Ignore if 0
                    int key = mapData[i, j];
                    if (key < 1) continue;

                    // Get or create Zone
                    Zone value = ZoneDict.ContainsKey(key) ? ZoneDict[key] : ZoneDict[key] = Schema.AddZone(insideStyle, outsideStyle);

                    // Add new Tile and it's connections
                    LBSTile t = Schema.AddTile(new Vector2Int(Area.x + i, Area.y + j), value);
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

        // Inherited Methdos (MUST HAVE)
        public override object Clone()
        {
            return new BSPAssistant(IconGuid, Name, ColorTint);
        }

        public override void OnGUI() { }

        public void OnTaskCancelled()
        {
            throw new NotImplementedException();
        }
    }
}