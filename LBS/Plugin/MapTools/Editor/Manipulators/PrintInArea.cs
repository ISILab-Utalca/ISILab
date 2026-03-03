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
        #endregion

        #region PROPERTIES
        public Blueprint BlueprintToPrint
        {
            set => blueprintToPrint = value;
        }
        protected override string IconGuid { get => "1900398b34c127b4bb80edadb9ef397b"; }

        #endregion


        #region CONSTRUCTORS
        public PrintInArea():base(){}

        #endregion

        #region METHODS
        public override void Init(LBSLayer layer, object owner)
        {
            previewAreaFeedback = new BlueprintFeedback();
            previewAreaFeedback.fixToTeselation = true;
            previewAreaFeedback.preview = true;
            previewAreaFeedback.SetColor(LBSSettings.Instance.view.colorListen);

            OnManipulationEnd += ClearPreview;
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            // Draw or create layers
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            ClearPreview();
            LBSLayer selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if (blueprintToPrint is null || selectedLayer is null) return;

            base.OnMouseMove(element, movePosition, e);

            var startPos = movePosition;
            var corners = selectedLayer.ToFixedPosition(startPos, movePosition);

            //subtracting vector1 because default tile size is 1
            var size = blueprintToPrint.GetSize() - Vector2Int.one;
            var endPos = startPos + selectedLayer.FixedToPosition(size).ToInt();
    
            previewAreaFeedback.UpdatePositions(startPos, endPos);
            MainView.Instance.AddElement(previewAreaFeedback);
            Debug.Log(
                $"Blueprint Preview → Start: {startPos} | Size: {size} | End: {endPos}"
            );

        }

        public void ClearPreview()
        {
            MainView.Instance.RemoveElement(previewAreaFeedback);
        }

        #endregion

    }

}