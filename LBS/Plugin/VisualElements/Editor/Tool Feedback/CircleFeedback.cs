using ISILab.Extensions;
using ISILab.LBS.VisualElements;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class CircleFeedback : Feedback
    {
        
        private static float fillOpacity = 0.33f;
        private static readonly Color Colordefault = Color.white;

        private float radiusPixels;
        private Vector2 offset = Vector2.zero;

        public CircleFeedback(Vector2Int center, float radiusPx)
        {
            startPosition = center;
            radiusPixels = radiusPx;
        }

        public CircleFeedback() : base() { }

        public override void UpdatePositions(Vector2Int center, Vector2Int radiusPos)
        {
            
            radiusPixels = Vector2.Distance(center, radiusPos);
            startPosition = center;
         

            MarkDirtyRepaint();
        }

        public void SetColor(Color c)
        {
            currentColor = c;
        }


        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var finalColor = currentColor;
            finalColor.a = fillOpacity;

            painter.DrawCircle(startPosition + offset, radiusPixels, finalColor);
        }

        internal void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }
    }
}
