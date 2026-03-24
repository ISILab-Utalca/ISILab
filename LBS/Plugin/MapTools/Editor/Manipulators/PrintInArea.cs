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
        private Vector2Int FeedbackPosition;
        internal static object AddingMode;
        // overwrite the defalt feedback of teselation as behavior is inhertied to follow mouse position
        internal static AreaFeedback areaFeedback;
        #endregion

        #region PROPERTIES
        public Blueprint BlueprintToPrint
        {
            set
            {
                blueprintToPrint = value;
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
                    areaFeedback.SetColor(Color.white);
                    Feedback.style.visibility = Visibility.Hidden;
                }
                return areaFeedback;
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
            Feedback.style.visibility = Visibility.Hidden;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);
            AreaFeedback.UpdatePositions(StartPosition, EndPosition);
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            base.OnMouseDown(element, startPosition, e);
            // Draw or create layers
            DoPrintBlueprint?.Invoke();
        }

        protected override void OnMouseMove(VisualElement element, Vector2Int movePosition, MouseMoveEvent e)
        {
            Feedback.style.display = DisplayStyle.None;
            ClearPreview();
            LBSLayer selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if (blueprintToPrint == null || selectedLayer == null) return;

            base.OnMouseMove(element, movePosition, e);

            //subtracting vector because default tile size is 100
            var size = blueprintToPrint.Size;
            FeedbackPosition = movePosition;
            AreaFeedback.UpdatePositions(FeedbackPosition, FeedbackPosition + size - new Vector2Int(100, 100));
            MainView.Instance.AddElement(AreaFeedback);

        }

        public void ClearPreview() => MainView.Instance.RemoveElement(AreaFeedback);

        #endregion

    }

}