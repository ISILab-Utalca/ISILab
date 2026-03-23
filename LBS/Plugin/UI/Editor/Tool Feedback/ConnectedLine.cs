using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.VisualElements;
using System.Linq;

namespace ISILab.LBS.VisualElements
{
    public class ConnectedLine : Feedback
    {
        public ConnectedLine(Vector2Int p1, Vector2Int p2) : base(p1, p2) { }
        public ConnectedLine() : base()
        {
            this.StartOffset = (TeselationSize.Multiply(0.5f));
            this.EndOffset = (TeselationSize.Multiply(0.5f));
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var line = new List<Vector2>() { startPosition, endPosition };
            painter.DrawPolygon(line, new Color(0, 0, 0, 0), currentColor, 4, false);
            painter.DrawCircle(startPosition, 16, currentColor);
            painter.DrawCircle(endPosition, 16, currentColor);
        }

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            startPosition = p1;
            endPosition = p2;

            if (fixToTeselation)
            {
                startPosition = CalcFixTeselation(startPosition);
                endPosition = CalcFixTeselation(endPosition);


                startPosition = (startPosition * TeselationSize) + TeselationSize.Multiply(0.5f);
                endPosition = (endPosition * TeselationSize) + TeselationSize.Multiply(0.5f);
            }

            this.MarkDirtyRepaint();
        }
    }

    public class ConnectedCornerLine : Feedback
    {
        public bool LeftSide = false;

        public Vector2Int cornerPoint;

        public bool useVertices = false;

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            startPosition = p1;
            endPosition = p2;

            cornerPoint = LeftSide ?
                    new Vector2Int(startPosition.x, endPosition.y) :
                    new Vector2Int(endPosition.x, startPosition.y);

            if (fixToTeselation)
            {
                Vector2Int offsetValue = TeselationSize.Multiply(0.5f);
                offsetValue = new Vector2Int(offsetValue.x, -offsetValue.y);
                Vector2Int offset = useVertices ? TeselationSize.Multiply(0.5f) : Vector2Int.zero;
                startPosition = CalcFixTeselation(startPosition + offset);
                endPosition = CalcFixTeselation(endPosition + offset);
                cornerPoint = CalcFixTeselation(cornerPoint + offset);

                startPosition = (startPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
                endPosition = (endPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
                cornerPoint = (cornerPoint * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
            }

            this.MarkDirtyRepaint();
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            var line = new List<Vector2>() { startPosition, cornerPoint, endPosition };
            painter.DrawPolygon(line, new Color(0, 0, 0, 0), currentColor, 4, false);
            painter.DrawCircle(startPosition, 16, currentColor);
            painter.DrawCircle(endPosition, 16, currentColor);
        }
    }

    public class ConnectedMemoryLine : Feedback
    {
        public bool LeftSide = false;

        // grid position , fixed position
        private List<(Vector2Int, Vector2Int)> line = new();
        public List<Vector2Int> Line { get => GetListPositions(line, ListPositionType.Grid); }

        public bool useVertices = false;

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            startPosition = p1;
            endPosition = p2;

            if (line.Count < 1) return;

            if (line.Any(p => p.Item1 == (p2 / 100))) return;
            float d = Vector2Int.Distance(line[line.Count - 1].Item1, p2 / 100);
            if (d >= 2) return;

            // Diagonal threshold
            if (d > 1 && d < 1.5f)
            {
                var leftSideCorner = new Vector2Int(line[line.Count - 1].Item1.x, p2.y / 100);
                var rightSideCorner = new Vector2Int(p2.x / 100, line[line.Count - 1].Item1.y);
                bool leftSideValid = !line.Any(p => p.Item1 == leftSideCorner);
                bool rightSideValid = !line.Any(p => p.Item1 == rightSideCorner);

                if (!leftSideValid && !rightSideValid) return;

                if (LeftSide && leftSideValid)
                {
                    line.Insert(line.Count - 1, (leftSideCorner, leftSideCorner * 100));
                }
                else if (rightSideValid)
                {
                    line.Insert(line.Count - 1, (rightSideCorner, rightSideCorner * 100));
                }
                else
                {
                    line.Insert(line.Count - 1, (leftSideCorner, leftSideCorner * 100));
                }
            }
            line.Insert(line.Count - 1, (p2 / 100, p2));


            if (fixToTeselation)
            {
                Vector2Int offsetValue = TeselationSize.Multiply(0.5f) * Vector2Int.down;
                Vector2Int offset = useVertices ? TeselationSize.Multiply(0.5f) : Vector2Int.zero;

                startPosition = CalcFixTeselation(startPosition + offset);
                endPosition = CalcFixTeselation(endPosition + offset);

                startPosition = (startPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
                endPosition = (endPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;

                line[0] = (startPosition / 100, startPosition);
                for (int i = 1; i < line.Count - 1; i++)
                {
                    line[i] = (line[i].Item1, CalcFixTeselation(line[i].Item2 + offset));
                    line[i] = (line[i].Item1, (line[i].Item2 * TeselationSize) + TeselationSize.Multiply(0.5f) - offset);//*/
                }
                line[line.Count - 1] = (endPosition / 100, endPosition);
            }

            this.MarkDirtyRepaint();
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            if (line.Count < 1)
            {
                line = new() { (startPosition / 100, startPosition), (endPosition / 100, endPosition) };
            }
            //PrintLine();
            painter.DrawPolygon(VectorIntToNormal(GetListPositions(line, ListPositionType.Fixed)), new Color(0, 0, 0, 0), currentColor, 4, false);
            painter.DrawCircle(startPosition, 16, currentColor);
            painter.DrawCircle(endPosition, 16, currentColor);
        }

        private List<Vector2Int> GetListPositions(List<(Vector2Int, Vector2Int)> list, ListPositionType type)
        {
            List<Vector2Int> output = new();
            foreach (var item in list)
            {
                if(type == ListPositionType.Grid)
                {
                    output.Add(item.Item1);
                }
                else
                {
                    output.Add(item.Item2);
                }
            }
            return output;
        }

        private List<Vector2> VectorIntToNormal(List<Vector2Int> list)
        {
            List<Vector2> output = new();
            foreach (var item in list)
            {
                output.Add(item);
            }
            return output;
        }

        public void LineClear()
        {
            line.Clear();
        }

        private void PrintLine()
        {
            string s = "";
            foreach (var p in line)
            {
                s += p.Item2.x.ToString() + "-" + p.Item2.y.ToString();
                s += " : ";
            }
            Debug.Log(s);
        }

        private enum ListPositionType
        {
            Grid = 0,
            Fixed = 1
        }
    }
}
