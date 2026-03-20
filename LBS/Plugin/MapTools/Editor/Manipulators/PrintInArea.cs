using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.UI.Editor;
using LBS.Components;
using System;
using UnityEngine;
using ISILab.LBS.VisualElements;
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
        private Vector2Int FeedbackPosition;
        internal static object AddingMode;
        #endregion

        #region PROPERTIES
        public Blueprint BlueprintToPrint
        {
            set
            {
                blueprintToPrint = value;
            }
        }
        protected override string IconGuid { get => "1900398b34c127b4bb80edadb9ef397b"; }

        #endregion


        #region ACTIONS
        public Action DoPrintBlueprint;
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
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);
            previewAreaFeedback.UpdatePositions(StartPosition, EndPosition);
           
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            base.OnMouseDown(element, startPosition, e);
            // Draw or create layers
            DoPrintBlueprint?.Invoke();

        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            ClearPreview();
            LBSLayer selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if (blueprintToPrint is null || selectedLayer is null) return;

            base.OnMouseMove(element, movePosition, e);

            //subtracting vector1 because default tile size is 100
            var size = blueprintToPrint.Size;
            FeedbackPosition = movePosition;
            previewAreaFeedback.UpdatePositions(FeedbackPosition, FeedbackPosition + size - new Vector2Int(100, 100));
            MainView.Instance.AddElement(previewAreaFeedback);

            Debug.Log($"[PrintInArea {GetHashCode()}] OnMouseMove");
        }

        public void ClearPreview() => MainView.Instance.RemoveElement(previewAreaFeedback);

        #endregion

    }

}