using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
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
        private List<BlueprintStorable> capturedBlueprintData = new();
        private BlueprintFeedback areaFeedback;

        #endregion

        #region PROPERTIES
        public List<BlueprintStorable> CapturedBlueprintData => capturedBlueprintData;
        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

      

        #endregion



        #region ACTIONS
        public Action<List<BlueprintStorable>, Texture2D, Vector2Int> CaptureComplete;
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
            CapturedBlueprintData.Clear();
            areaFeedback.UpdatePositions(StartPosition, EndPosition);

            Vector2Int AreaStart = areaFeedback.StartPosition.ToInt();
            Vector2Int AreaEnd = areaFeedback.EndPosition.ToInt();
            /** Tesselation clamping adds bordering tiles, subtracting 1 will keep it within 
             * a single tile size*/
            var teselleationAreaStart = AreaStart;
            var tesellationAreaEnd = AreaEnd;

            tesellationAreaEnd.x -= 100;
            tesellationAreaEnd.y -= 100;

            Debug.Log("Start: " + AreaStart);
            Debug.Log("End: " + AreaEnd);

            CloneRefs.Start();

            // Should get all layers under the start and endposition 
            foreach (LBSLayer layer in LBSMainWindow.Instance.GetLayers())
            {

                BlueprintData[] layerObjs = layer.GetObjects(teselleationAreaStart, tesellationAreaEnd);
                if (!layerObjs.Any()) continue;
                
                BlueprintStorable data = new BlueprintStorable(layer.Name, layer.ID, layerObjs);
                CapturedBlueprintData.Add(data);

            }

            CloneRefs.End();

            MainView.Instance.AddElement(areaFeedback);

            Rect rect = Rect.MinMaxRect(
                AreaStart.x,
                AreaStart.y,
                AreaEnd.x,
                AreaEnd.y
            );

            areaFeedback.SetDisplay(false);

            // Failed to find any storable objects
            if (!CapturedBlueprintData.Any()) return false;

            // ensure are is within graph
         //   if (!graphRect.Contains(rect.min) || !graphRect.Contains(rect.max)) 
          //      return false;

            LBSVisualElementHelper.CaptureGraphView(
                LBSMainWindow.Instance,
                MainView.Instance,
                rect,
                tex =>
                {
                    areaFeedback.SetDisplay(true);
                    CaptureComplete?.Invoke(CapturedBlueprintData, tex, rect.size.ToInt());
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