using System.Collections.Generic;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Manipulators
{
    public class RemovePopulationTile : LBSManipulator
    {
        private PopulationBehaviour _population;
        
        private Feedback _previewFeedback;

        private readonly List<Feedback> previews = new();

        private bool LeftButtonPressed;
        
        protected override string IconGuid => "ce08b36a396edbf4394f7a4e641f253d";

        public RemovePopulationTile()
        {
            Feedback = new AreaFeedback();
            Feedback.fixToTeselation = true;
            Name = "Remove Tiles";
            Description = "Click on an item in the graph to remove it.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _previewFeedback = new DottedAreaFeedback();
            _previewFeedback.preview = true;
            _previewFeedback.fixToTeselation = true;
            
            _population = provider as PopulationBehaviour;
            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        protected override void OnMouseLeave(VisualElement element, MouseLeaveEvent e)
        {
            ForceCancel = true;
            MainView.Instance.RemoveElement(_previewFeedback);
            MainView.Instance.RemoveElement(Feedback);
            CleanPreviews();
        }
        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            LeftButtonPressed = false;

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                MainView.Instance.RemoveElement(_previewFeedback);
                ForceCancel = false;
                return;
            }

            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Remove element population");

            (Vector2Int min, Vector2Int max) corners = _population.OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            List<TileBundleGroup> removed = new();
            for (int i = corners.Item1.x; i <= corners.Item2.x; i++)
            {
                for (int j = corners.Item1.y; j <= corners.Item2.y; j++)
                {
                    Vector2Int position = new Vector2Int(i, j);
                    removed.Add(_population.RemoveTileGroup(position));
                }
            }

            LBSLayer OwnerLayer = _population.OwnerLayer;
            TileGroupBehavior poptb = OwnerLayer.GetBehaviour<TileGroupBehavior>();
            if(poptb is not null)
            {
                if (removed.Contains(poptb.SelectedTilemap)) 
                { 
                    poptb.SelectedTilemap = null; 
                }
            }
            LoadedLevel level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Element population");
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(x);
            }

            OwnerLayer.OnChangeUpdate();
            
            CleanPreviews();
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            base.OnMouseDown(element, startPosition, e);
            StartPosition = startPosition;

            LeftButtonPressed = e.button == 0;
            
            CleanPreviews();
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            CleanPreviews();

            if (!LeftButtonPressed || ForceCancel) return;
            
            List<TileBundleGroup> dottedGroups = new List<TileBundleGroup>();
            
            (Vector2Int min, Vector2Int max) corners = _population.OwnerLayer.ToFixedPosition(StartPosition, movePosition);
            for (int i = corners.Item1.x; i <= corners.Item2.x; i++)
            {
                for (int j = corners.Item1.y; j <= corners.Item2.y; j++)
                {
                    //Debug.Log("selected tile: " + i + " | " + j);
                    Vector2Int position = new(i, j);
                    
                    TileBundleGroup tileGroup = _population.GetTileGroup(position);
                    if (tileGroup is null) continue;
                    if(dottedGroups.Contains(tileGroup)) continue;
       
                    dottedGroups.Add(tileGroup);
                    
                    DottedAreaFeedback pf = new()
                    {
                        preview = true,
                        fixToTeselation = true
                    };
                    
                    Vector2Int topLeftCorner = tileGroup.AreaRect.position.ToInt();
                    Vector2Int bottomRightCorner = topLeftCorner;
                    
                    // Set corner by tile size
                    Vector2Int offset = Vector2Int.zero;
            
                    Bundle ToSet = tileGroup.BundleData.Bundle;
                    if(ToSet.TileSize.x > 1) offset.x += ToSet.TileSize.x - 1;
                    if(ToSet.TileSize.y > 1) offset.y -= ToSet.TileSize.y - 1;

                    // grid to local position
                    Vector2 firstPos = _population.OwnerLayer.FixedToPosition(topLeftCorner, true);
                    Vector2 lastPos = _population.OwnerLayer.FixedToPosition(bottomRightCorner + offset, true);
 
                    /* negative numbers in the FixedToPosition get clamped on negatives, jumping to the next lowest value.
                     example: coordinate -100 instead draws on -200
                     */
                    if (firstPos.x < 0) firstPos.x += 99;
                    if (lastPos.x < 0) lastPos.x += 99;
                    if (firstPos.y < 0) firstPos.y += 99;
                    if (lastPos.y < 0) lastPos.y += 99;
            
                    pf.ValidForInput(true);
                    pf.UpdatePositions(firstPos.ToInt(), lastPos.ToInt());
                    previews.Add(pf);
                
                }
            }

            foreach (Feedback feedback in previews)
            {
                MainView.Instance.AddElement(feedback);
            }

        }

        private void CleanPreviews()
        {
            foreach (Feedback feedback in previews)
            {
                if (feedback is null) continue;
                MainView.Instance.RemoveElement(feedback);
                feedback.visible = false;
            }

            previews.Clear();
        }
    }
}