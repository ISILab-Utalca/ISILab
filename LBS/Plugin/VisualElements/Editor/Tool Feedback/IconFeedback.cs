using ISILab.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class IconFeedback : Feedback
    {
        private const float Alpha = 0.25f;

        public VectorImage Icon { get; set; }

        public IconFeedback() : base()
        {
            EndOffset = TeselationSize;
        }

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            startPosition = new Vector2Int(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y));
            endPosition = new Vector2Int(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));

            if (fixToTeselation)
            {
                startPosition = CalcFixTeselation(startPosition);
                endPosition = CalcFixTeselation(endPosition);

                startPosition = startPosition * TeselationSize + StartOffset;
                endPosition = endPosition * TeselationSize + TeselationSize;
            }

            MarkDirtyRepaint();
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            //painter.DrawSquareGrid(new List<List<Vector2>>() { new List<Vector2>() { startPosition }, new List<Vector2>() { endPosition } }, Color.red, Color.white, 2, 20, 0);
            var points = new List<Vector2>()
            {
                new Vector2(startPosition.x, startPosition.y),
                new Vector2(startPosition.x, endPosition.y),
                new Vector2(endPosition.x, endPosition.y),
                new Vector2(endPosition.x, startPosition.y),
            };
            Vector2 center = new Vector2((startPosition.x + endPosition.x) / 2, (startPosition.y + endPosition.y) / 2);
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = Vector2.Lerp(points[i], center, 0.2f);
            }

            painter.DrawPolygon(points, new Color(currentColor.r, currentColor.g, currentColor.b, Alpha), new Color(1, 1, 1, 0.25f), 2.5f, true);
            if (Icon != null)
            {
                Vector2 offset = Vector2.Lerp(startPosition, center, 0.425f);
                mgc.DrawVectorImage(Icon, offset, Angle.Degrees(0), new Vector2(1.75f, 1.75f));

            }

        }

    }
}

