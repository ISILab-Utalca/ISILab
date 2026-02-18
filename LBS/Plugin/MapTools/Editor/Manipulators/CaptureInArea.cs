using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class CaptureInArea : ManipulateTeselation
    {

        public object[] capturedObjects;

        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

        private AreaFeedback areaFeedback;

        public CaptureInArea():base(){}

        public override void Init(LBSLayer layer, object owner)
        {
            areaFeedback = new AreaFeedback();
            areaFeedback.fixToTeselation = true;
            areaFeedback.preview = true;
            areaFeedback.SetColor(LBSSettings.Instance.view.warningColor); 
        //    OnManipulationEnd += ClearArea;
          //  base.Init(layer, owner);
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            ClearArea();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            List<object> areaObjs = new();

            capturedObjects = areaObjs.ToArray();
            areaFeedback.UpdatePositions(StartPosition, EndPosition);

            Vector2Int AreaStart = areaFeedback.StartPosition.ToInt();
            Vector2Int AreaEnd = areaFeedback.EndPosition.ToInt();

            // Should get all layers under the start and endposition 
            foreach (LBSLayer layer in LBSMainWindow.Instance.GetLayers())
            {
                areaObjs.AddRange(layer.GetObjects(AreaStart, AreaEnd));
            }


            MainView.Instance.AddElement(areaFeedback);
            // Use the objects to save them and create a blueprint

            Rect rect = new Rect(AreaStart, AreaEnd);

            var graph = MainView.Instance; // your Graph root VisualElement

            BlueprintPanel.CaptureElement(graph, rect, tex =>
            {
                BlueprintPanel.Instance.SetPreviewTexture(tex);
            });

        }


        public void ClearArea()
        {
            MainView.Instance.RemoveElement(areaFeedback);
        }
    }
}