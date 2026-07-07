using ISILab.LBS.Assistants;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BSPAssistant : LBSAssistant, IAssistantThreaded
{
    // Example of public member that can be configured through the Editor (UI) class.
    public string exampleMember;

    // Fields
    BSPDungeonGenerator _generator;
    Dictionary<int, Zone> _zoneDict = new();
    SchemaBehaviour _schemaBehaviour;

    private RectInt _area = new(0, 0, 50, 50);
    public int minPartitionSize = 10;
    public int minRoomSize = 4;

    // Properties
    public RectInt Area
    {
        set
        {
            if (value == _area) return;
            _area = value;
            AreaChanged?.Invoke();
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

    // Events
    public event Action AreaChanged;

    public BSPAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
    {

    }

    /// <summary>
    /// Main method that runs the assistant's work asyncronically, and reports the progress.
    /// </summary>
    /// <param name="onProgress">Callback to inform progress (range 0..1).</param>
    /// <param name="token">Cancellation token provided by the Editor.</param>
    /// <remarks>
    /// - This method is typicaly executed in a Thread. Avoid using UnityEngine APIs from threads since some of them are not supported.
    /// - It's recommended to periodically call ´CheckPendingCancel´ to respect the user's cancel request.
    /// - It's recommended to periodically call ´onProfress´ to show the assistant's progress on the UI.
    /// </remarks>
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
                onProgress?.Invoke((float)((i * w) + j) / (w * h));
            }
        }
        // Update progress bar
        onProgress?.Invoke(1);
        Thread.Sleep(1);
    }

    // Callback invoked when the task is cancelled.
    public void OnTaskCancelled() { }

    public override object Clone()
    {
        return new BSPAssistant(IconGuid, Name, ColorTint);
    }

    public override void OnGUI() { }

}
