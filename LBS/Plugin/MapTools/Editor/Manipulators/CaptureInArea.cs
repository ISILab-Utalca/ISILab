using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Extensions;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.MapTools.Editor.Manipulators
{

    public class CaptureInArea : ManipulateTeselation
    {

        #region FIELDS
        private List<LBSLayer> capturedBlueprintData = new();
        // overwrite the defalt feedback of teselation as behavior is inhertied to follow mouse position
        private static AreaFeedback areaFeedback;
        private bool CaptureStarted;
        private bool Capturing;
        private bool autoCapture = true;
        #endregion

        #region PROPERTIES
        public List<LBSLayer> CapturedBlueprintData => capturedBlueprintData;
        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

        public bool AutoCapture
        {
            get => autoCapture;
            set
            {
                string message = value == true ? "Auto Capture" : string.Empty;
                LBSMainWindow.WarningManipulator(message);
                autoCapture = value;
            }
        }

        public AreaFeedback AreaFeedback
        {
            get
            {
                if(areaFeedback == null)
                {
                    areaFeedback = new AreaFeedback();
                    areaFeedback.fixToTeselation = true;
                    areaFeedback.preview = true;
                    areaFeedback.SetColor(LBSSettings.Instance.view.warningColor);
                }
                return areaFeedback;
            }

            set => areaFeedback = value;
        }

        #endregion

        #region ACTIONS
        public Action<List<LBSLayer>, Texture2D, Vector2Int> CaptureComplete;
        public Action<KeyDownEvent> KeyDown;
        public Action<KeyUpEvent> KeyUp;
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
        }

        protected override void OnKeyDown(KeyDownEvent e)
        {
            base.OnKeyDown(e);
            KeyDown?.Invoke(e);
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);
            KeyUp?.Invoke(e);
        }
        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            if (e.button == 0) CaptureStarted = true;
            ClearArea();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            // basically removes the update positions
            if (e.button == 1) CaptureStarted = false;

            if(!CaptureStarted) return;

            AreaFeedback.UpdatePositions(StartPosition, EndPosition);
            MainView.Instance.AddElement(AreaFeedback);

            if (AutoCapture)
            {
                DoCapture();
            }
        
            else AreaFeedback.SetDisplay(true);
        }

        public void DoCapture()
        {
            if (Capturing) return;

            Capturing = true;
            CapturedBlueprintData.Clear();

            Vector2Int AreaStart = AreaFeedback.StartPosition.ToInt();
            Vector2Int AreaEnd = AreaFeedback.EndPosition.ToInt();

            var teselleationAreaStart = AreaStart;
            var tesellationAreaEnd = AreaEnd;

            tesellationAreaEnd.x -= 100;
            tesellationAreaEnd.y -= 100;

            capturedBlueprintData.Clear();
            // Should get all layers under the start and endposition 
            foreach (LBSLayer layer in LBSMainWindow.Instance.layerPanel.GetInverseOrderedLayers())
            {
                var layerClone = layer.GetAreaClone(teselleationAreaStart, tesellationAreaEnd);
                if (layerClone != null)
                {
                     capturedBlueprintData.Add(layerClone);
                }
               
            }
            
            Rect rect = Rect.MinMaxRect(
                AreaStart.x,
                AreaStart.y,
                AreaEnd.x,
                AreaEnd.y
            );

            AreaFeedback.SetDisplay(false);

            // Failed to find any storable objects
            if (!CapturedBlueprintData.Any())
            {
                Capturing = false;
                CaptureComplete?.Invoke(CapturedBlueprintData, null, rect.size.ToInt());
                return;
            }
            LBSVisualElementHelper.CaptureGraphView(
                LBSMainWindow.Instance,
                MainView.Instance,
                rect,
                tex =>
                {
                    AreaFeedback.SetDisplay(true);
                    CaptureComplete?.Invoke(CapturedBlueprintData, tex, rect.size.ToInt());
                    Capturing = false;
                }
            );
        }

        public void ClearArea() => 
            MainView.Instance.RemoveElement(AreaFeedback);

        #endregion

    }

}