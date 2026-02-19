using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace ISILab.LBS.Manipulators
{
    // Simple wrapper to access the area 
    public class BlueprintFeedback : AreaFeedback { }

    public class CaptureInArea : ManipulateTeselation
    {
        public object[] capturedObjects;

        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

        private BlueprintFeedback areaFeedback;

        public CaptureInArea():base(){}

        public override void Init(LBSLayer layer, object owner)
        {
            areaFeedback = new BlueprintFeedback();
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

            Rect rect = Rect.MinMaxRect(
                AreaStart.x,
                AreaStart.y,
                AreaEnd.x,
                AreaEnd.y
            );

            areaFeedback.SetDisplay(false);

            LBSVisualElementHelper.CaptureGraphView(
               LBSMainWindow.Instance,
               MainView.Instance,
               rect,
               OnGraphCaptured
            );

          
        }

        private void OnGraphCaptured(Texture2D tex)
        {
            BlueprintPanel.Instance.SetPreviewTexture(tex);
            areaFeedback.SetDisplay(true);
        }

        public void ClearArea() => MainView.Instance.RemoveElement(areaFeedback);
    }
}