using ISILab.Extensions;
using ISILab.LBS.VisualElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class AreaFeedback : Feedback
    {
        #region Static fields
        private static float borderThickness = 1f;
        private static float fillOpacity = 0.33f;
        private static readonly Color Colordefault = new (0.5f, 0.7f, 0.98f, 1);
        private static readonly Color ColorPreview = new (0.5f, 0.7f, 0.98f, 1);
        private static readonly Color ColorDelete = Color.red;


        #endregion
        

        public Vector2 StartPosition { get => startPosition; }
        public Vector2 EndPosition { get => endPosition; }

        public AreaFeedback(Vector2Int p1, Vector2Int p2) : base(p1, p2) { }

        public AreaFeedback() : base()
        {
            EndOffset = TeselationSize;
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            
            var colorFill = delete ?  ColorDelete : Colordefault;
            if(preview) colorFill = ColorPreview;
            
            colorFill.a = fillOpacity;
            var fillColor = currentColor * colorFill;

            var points = new List<Vector2>()
        {
            new Vector2(startPosition.x, startPosition.y),
            new Vector2(startPosition.x, endPosition.y),
            new Vector2(endPosition.x, endPosition.y),
            new Vector2(endPosition.x, startPosition.y),
        };
            painter.DrawPolygon(points, fillColor, colorFill, borderThickness, true);
        }

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            if (fixToTeselation)
            {
                p1.x -= (p1.x >= 0 ? (Mathf.Abs(p1.x) % TeselationSize.x) : -(Mathf.Abs(p1.x) % TeselationSize.x) + TeselationSize.x);
                p1.y -= (p1.y >= 0 ? (Mathf.Abs(p1.y) % TeselationSize.y) : -(Mathf.Abs(p1.y) % TeselationSize.y) + TeselationSize.y);
                p2.x -= (p2.x >= 0 ? (Mathf.Abs(p2.x) % TeselationSize.x) : -(Mathf.Abs(p2.x) % TeselationSize.x) + TeselationSize.x);
                p2.y -= (p2.y >= 0 ? (Mathf.Abs(p2.y) % TeselationSize.y) : -(Mathf.Abs(p2.y) % TeselationSize.y) + TeselationSize.y);
            }

            if (fixAspect)
            {
                var delta = new Vector2Int(p2.x - p1.x, p2.y - p1.y);
                var minDelta = Mathf.Min(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                p2.x = p1.x + (minDelta * System.Math.Sign(p2.x - p1.x));
                p2.y = p1.y + (minDelta * System.Math.Sign(p2.y - p1.y));
            }
            startPosition = new Vector2Int(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y));
            endPosition = new Vector2Int(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));

            if(fixToTeselation)
            {
                startPosition += StartOffset;
                endPosition += TeselationSize;
            }            
            MarkDirtyRepaint();
        }
        
        public void SetColor(Color color)
        {
            currentColor = color; 
        }
    }
}