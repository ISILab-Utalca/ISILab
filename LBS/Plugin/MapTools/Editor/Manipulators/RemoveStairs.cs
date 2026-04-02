using ISILab.Commons;
using ISILab.LBS;
using ISILab.LBS.Manipulators;
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

public class RemoveStairs : LBSManipulator
{
    override protected string IconGuid => "ce08b36a396edbf4394f7a4e641f253d";

    private SchemaBehaviour _schema;
    private List<SchemaBehaviour> _others = new ();
    private Vector2Int _first;

    public RemoveStairs() 
    {
        Feedback = new AreaFeedback();
        Feedback.fixToTeselation = true;

        Name = "Remove stairs";
        Description =
            "Click on a stair to remove it.";
    }

    public override void Init(LBSLayer layer, object provider = null)
    {
        base.Init(layer, provider);
        _schema = provider as SchemaBehaviour;
        if (_schema.MultiLayerConnections)
            _others = LBSController.CurrentLevel.data.Layers
                .Select(l => l.GetBehaviour<SchemaBehaviour>())
                .Where(b => b is not null && b != _schema)
                .ToList();

        Feedback.TeselationSize = layer.TileSize;
        layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
    }

    protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
    {
        _first = _schema.OwnerLayer.ToFixedPosition(position);
    }

    protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
    {
        base.OnMouseUp(element, endPosition, e); 

        if (ForceCancel)
        {
            ForceCancel = false;
            return;
        }

        // Set Undo action
        LoadedLevel level = LBSController.CurrentLevel;
        EditorGUI.BeginChangeCheck();
        Undo.RegisterCompleteObjectUndo(level, "Remove Stairs");

        List<LBSStair> toRemove = new();
        toRemove.AddRange(_schema.OwnerLayer.GetModule<StairsModule>().Stairs);
        foreach(var o in _others)
        {
            toRemove.AddRange(o.OwnerLayer.GetModule<StairsModule>().Stairs);
        }


        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(level);
        }
    }
}
