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
    /// <summary>
    /// Prints a blueprint (data, objects, layer, etc.) into the graph
    /// </summary>
    public class PrintInArea : ManipulateTeselation
    {

        #region FIELDS
        private Blueprint blueprintToPrint;
        private BlueprintFeedback previewAreaFeedback; 
        private List<Feedback> previews = new List<Feedback>();
        private Vector2Int FeedbackPosition;
        internal static object AddingMode;
        #endregion

        #region PROPERTIES
        public Blueprint BlueprintToPrint
        {
            set => blueprintToPrint = value;
        }
        protected override string IconGuid { get => "1900398b34c127b4bb80edadb9ef397b"; }

        #endregion


        #region CONSTRUCTORS
        public PrintInArea():base()
        {
            Name = "Add blueprint";
            Description = "Click in an area to instance the data of the current selected Blueprint.";
        }

        #endregion

        #region METHODS
        public override void Init(LBSLayer layer, object owner)
        {
            previewAreaFeedback = new BlueprintFeedback();
            previewAreaFeedback.fixToTeselation = true;
            previewAreaFeedback.preview = true;
            previewAreaFeedback.SetColor(Color.white);

            OnManipulationEnd += () =>
            {
                MainView.Instance.RemoveElement(previewAreaFeedback);
            };

            //OnManipulationEnd += ClearPreview;
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            // Draw or create layers
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
           // ClearPreview();
            LBSLayer selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if (blueprintToPrint is null || selectedLayer is null) 
            {
                ClearPreview();
                return; 
            }

            base.OnMouseMove(element, movePosition, e);


            //subtracting vector1 because default tile size is 1
            var size = blueprintToPrint.Size;
            FeedbackPosition = movePosition;
            var corners = selectedLayer.ToFixedPosition(FeedbackPosition, FeedbackPosition - size);
            
            previewAreaFeedback.UpdatePositions(FeedbackPosition, FeedbackPosition + size-new Vector2Int(100,100));
            MainView.Instance.AddElement(previewAreaFeedback);

        }

        public void ClearPreview()
        {
            MainView.Instance.RemoveElement(previewAreaFeedback);
        }

        internal Vector2 GetRectPosition()
        {
            return previewAreaFeedback.StartPosition;
        }

        #endregion

    }

}