using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    // Simple wrapper to access the area 
    public class BlueprintFeedback : AreaFeedback { }

    public class CaptureInArea : ManipulateTeselation
    {
        #region CONSTS
        public const bool AutoCapture = true;
        #endregion

        #region FIELDS
        private List<LBSLayer> capturedBlueprintData = new();
        private BlueprintFeedback areaFeedback;
        private bool Capturing = false;
        #endregion

        #region PROPERTIES
        public List<LBSLayer> CapturedBlueprintData => capturedBlueprintData;
        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

      

        #endregion



        #region ACTIONS
        public Action<List<LBSLayer>, Texture2D, Vector2Int> CaptureComplete;
        #endregion

        #region CONSTRUCTORS
        public CaptureInArea():base()
        {
            Name = "Create blueprint";
            Description = "Select an area in the graph to store its data as a Blueprint.";
        }

        #endregion

        #region METHODS
        public override void Init(LBSLayer layer, object owner)
        {
            areaFeedback = new BlueprintFeedback();
            areaFeedback.fixToTeselation = true;
            areaFeedback.preview = true;
            areaFeedback.SetColor(LBSSettings.Instance.view.warningColor); 
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            ClearArea();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            if (AutoCapture) DoCapture();
   
        }

        public bool DoCapture()
        {
            if (Capturing) return false;

            Capturing = true;
            CapturedBlueprintData.Clear();
            areaFeedback.UpdatePositions(StartPosition, EndPosition);

            Vector2Int AreaStart = areaFeedback.StartPosition.ToInt();
            Vector2Int AreaEnd = areaFeedback.EndPosition.ToInt();
            
            /** Tesselation clamping adds bordering tiles, subtracting tilesize keeps the correct bounds
             */
            var teselleationAreaStart = AreaStart;
            var tesellationAreaEnd = AreaEnd;

            tesellationAreaEnd.x -= 100;
            tesellationAreaEnd.y -= 100;

            capturedBlueprintData.Clear();
            // Should get all layers under the start and endposition 
            foreach (LBSLayer layer in LBSMainWindow.Instance.GetLayers())
            {
                var layerClone = layer.GetAreaClone(teselleationAreaStart, tesellationAreaEnd);
                if (layerClone != null)
                {
                     capturedBlueprintData.Add(layerClone);
                }
               
            }

            MainView.Instance.AddElement(areaFeedback);
            Rect rect = Rect.MinMaxRect(
                AreaStart.x,
                AreaStart.y,
                AreaEnd.x,
                AreaEnd.y
            );

            areaFeedback.SetDisplay(false);

            // Failed to find any storable objects
            if (!CapturedBlueprintData.Any())
            {
                Capturing = false;
                return false;
            }
            LBSVisualElementHelper.CaptureGraphView(
                LBSMainWindow.Instance,
                MainView.Instance,
                rect,
                tex =>
                {
                    areaFeedback.SetDisplay(true);
                    CaptureComplete?.Invoke(CapturedBlueprintData, tex, rect.size.ToInt());
                    Capturing = false;
                }
            );
            return true;
        }

        public void ClearArea()
        {
            MainView.Instance.RemoveElement(areaFeedback);
        }

        #endregion

    }

}