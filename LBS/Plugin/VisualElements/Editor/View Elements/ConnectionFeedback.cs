using ISILab.Extensions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.Commons.VisualElements
{
    public class ConnectionFeedback : GraphElement
    {
        private Color color = Color.white;
        private Vector2Int pos1 = new Vector2Int();
        private Vector2Int pos2 = new Vector2Int();
        private Vector2 offset = new Vector2();

        public ConnectionFeedback()
        {
            focusable = false;
            SetPosition(new Rect(Vector2.zero, new Vector2(10, 10)));
            generateVisualContent += OnGenerateVisualContent;
        }

        public void UpdatePositions(Color color, Vector2Int pos1, Vector2Int pos2)
        {
            this.color = color;
            this.pos1 = pos1;
            this.pos2 = pos2;
            MarkDirtyRepaint();
        }

        internal void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var fPos1 = pos1 + offset;
            var fPos2 = pos2 + offset;
            painter.DrawLine(fPos1, fPos2, color, 3);
            painter.DrawCircle(fPos1, 10f, color);
            painter.DrawCircle(fPos2, 10f, color);
            
        }
    }
}