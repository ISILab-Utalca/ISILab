using ISILab.LBS.Assistants;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class DWAssistant : LBSAssistant, IAssistantThreaded
{
    // Fields
    DrunkardWalkerGenerator _generator;
    Dictionary<int, Zone> _zoneDict = new();
    SchemaBehaviour _schemaBehaviour;

    private RectInt _area = new(0, 0, 60, 60);
    public int totalRooms = 6;
    public int walkDistanceBetweenRooms = 5;
    public Vector2Int minRoomSize = new Vector2Int(3, 3);
    public Vector2Int maxRoomSize = new Vector2Int(7, 7);

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
    public DrunkardWalkerGenerator Generator
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

    // Constructor
    public DWAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }

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
        int[,] mapData = Generator.Generate(Area.width, Area.height, totalRooms, walkDistanceBetweenRooms, minRoomSize, maxRoomSize);

        // Read map
        int w = mapData.GetLength(0);
        int h = mapData.GetLength(1);
        for(int i = 0; i < w; i++)
        {
            for(int j = 0; j < h; j++)
            {
                // Cancel Check
                if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                    return;

                // Ignore if 0
                int key = mapData[i, j];
                if (key < 1) continue;

                // Get or create zone
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
    }

    // Callback invoked when the task is cancelled.
    public void OnTaskCancelled() { }

    public override object Clone()
    {
        return new DWAssistant(IconGuid, Name, ColorTint);
    }

    public override void OnGUI() { }

}

public class DWAssistantManipulator : ManipulateTeselation
{
    private DWAssistant assistant;
    private Vector2Int _cornerStart;
    protected override string IconGuid => "ad8feef201665454ca79e31b7d798ac3";
    public event System.Action Execute;

    public DWAssistantManipulator() 
    {
        Feedback.fixToTeselation = true;
        Name = "Drunkard Walk Dungeon Generator";
        Description = "Select an area to generate a dungeon using the Drunkard Walk algorithm.";
    }

    public override void Init(LBSLayer layer, object owner)
    {
        base.Init(layer, owner);
        assistant = owner as DWAssistant;
    }
    protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
    {
        _cornerStart = position;
    }

    protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
    {
        base.OnMouseUp(element, endPosition, e);

        //If esc key was pressed, cancel the operation
        if (ForceCancel)
        {
            ForceCancel = false;
            return;
        }

        var corners = assistant.OwnerLayer.ToFixedPosition(_cornerStart, endPosition);
        var mapWidth = corners.max.x - corners.min.x;
        var mapHeight = corners.max.y - corners.min.y;
        assistant.Area = new RectInt(corners.min.x, corners.min.y, mapWidth, mapHeight);
        Execute?.Invoke();
    }
}
