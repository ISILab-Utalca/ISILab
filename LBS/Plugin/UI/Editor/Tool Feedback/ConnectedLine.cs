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
        protected List<(Vector2Int, Vector2Int)> line = new();
        public List<Vector2Int> Line { get => GetListPositions(line, ListPositionType.Grid); }

        public bool useVertices = false;

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            // Save start and end positions
            startPosition = p1;
            endPosition = p2;

            // Return if line wasn't initiated or if new position is already in line 
            if (line.Count < 1) return;
            var p2Grid = VectorFixedToGrid(p2);
            var p2fixed = VectorGridToFixed(p2Grid);

            if (line.Any(p => p.Item1 == p2Grid))
            {
                return;
            }

            // Return if new position is too far from last position
            float d = Vector2Int.Distance(line[line.Count - 1].Item1, p2Grid);
            Debug.Log(line[line.Count - 1].Item1.x + " " + line[line.Count - 1].Item1.y + " : " + p2Grid.x + " " + p2Grid.y);
            //Debug.Log(d);
            if (d >= 2) return;

            // Diagonal threshold: automatically adds new corner position if avaliable, return if not
            if (d > 1 && d < 1.5f)
            {
                var leftSideCorner = new Vector2Int(line[line.Count - 1].Item1.x, p2Grid.y);
                var rightSideCorner = new Vector2Int(p2Grid.x, line[line.Count - 1].Item1.y);
                bool leftSideValid = !line.Any(p => p.Item1 == leftSideCorner);
                bool rightSideValid = !line.Any(p => p.Item1 == rightSideCorner);

                if (!leftSideValid && !rightSideValid) return;

                if (LeftSide && leftSideValid)
                {
                    line.Add((leftSideCorner, VectorGridToFixed(leftSideCorner)));
                    Debug.Log("Se añadió: " + leftSideCorner.x + " " + leftSideCorner.y + " : " + VectorGridToFixed(leftSideCorner).x + " " + VectorGridToFixed(leftSideCorner).y);
                }
                else if (rightSideValid)
                {
                    line.Add((rightSideCorner, VectorGridToFixed(rightSideCorner)));
                    Debug.Log("Se añadió: " + rightSideCorner.x + " " + rightSideCorner.y + " : " + VectorGridToFixed(rightSideCorner).x + " " + VectorGridToFixed(rightSideCorner).y);
                }
                else
                {
                    line.Add((leftSideCorner, VectorGridToFixed(leftSideCorner)));
                    Debug.Log("Se añadió: " + leftSideCorner.x + " " + leftSideCorner.y + " : " + VectorGridToFixed(leftSideCorner).x + " " + VectorGridToFixed(leftSideCorner).y);
                }
            }

            // Adds new position to line
            line.Add((p2Grid, p2));
            Debug.Log("Se añadió: " + line[line.Count - 1].Item1.x + " " + line[line.Count - 1].Item1.y + " : " + p2.x + " " + p2.y);

            // Calculate fixed position for every position in line
            if (fixToTeselation)
            {
                Vector2Int offsetValue = TeselationSize.Multiply(0.5f) * Vector2Int.down;
                Vector2Int offset = useVertices ? TeselationSize.Multiply(0.5f) : Vector2Int.zero;

                startPosition = CalcFixTeselation(startPosition + offset);
                startPosition = (startPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
                line[0] = (VectorFixedToGrid(startPosition), startPosition);

                for (int i = 1; i < line.Count - 1; i++)
                {
                    line[i] = (line[i].Item1, CalcFixTeselation(line[i].Item2 + offset));
                    line[i] = (line[i].Item1, (line[i].Item2 * TeselationSize) + TeselationSize.Multiply(0.5f) - offset);//*/
                }

                endPosition = CalcFixTeselation(endPosition + offset);
                endPosition = (endPosition * TeselationSize) + TeselationSize.Multiply(0.5f) - offset;
                line[line.Count - 1] = (VectorFixedToGrid(endPosition), endPosition);
            }

            this.MarkDirtyRepaint();
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            if (line.Count < 1)
            {
                line = new() { (VectorFixedToGrid(startPosition), startPosition)};
                Debug.Log("Se añadió: " + VectorFixedToGrid(startPosition).x + " " + VectorFixedToGrid(startPosition).y + " : " + startPosition.x + " " + startPosition.y);
            }

            painter.DrawPolygon(VectorIntToNormal(GetListPositions(line, ListPositionType.Fixed)), new Color(0, 0, 0, 0), currentColor, 4, false);
            painter.DrawCircle(startPosition, 16, currentColor);
            painter.DrawCircle(endPosition, 16, currentColor);
        }

        protected List<Vector2Int> GetListPositions(List<(Vector2Int, Vector2Int)> list, ListPositionType type)
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

        protected List<Vector2> VectorIntToNormal(List<Vector2Int> list)
        {
            List<Vector2> output = new();
            foreach (var item in list)
            {
                output.Add(item);
            }
            return output;
        }
        protected Vector2Int VectorFixedToGrid(Vector2Int item)
        {
            var v = item * new Vector2Int(1,-1);
            if (item.x < 0) v += Vector2Int.left * 100;
            if (item.y < 0) v += Vector2Int.up * 100;
            return v / 100;
        }
        protected Vector2Int VectorGridToFixed(Vector2Int item)
        {
            var v = item * 100;
            if (v.x < 0) v += Vector2Int.right * 50;
            if (v.y > 0) v += Vector2Int.down * 50;
            return v * new Vector2Int(1, -1);
        }

        public virtual void LineClear()
        {
            line.Clear();
        }

        protected enum ListPositionType
        {
            Grid = 0,
            Fixed = 1
        }
    }

    public class StairsMemoryLine : ConnectedMemoryLine
    {
        int _limit = 6;
        bool _canContinue = true;
        bool _isValid = false;
        public bool IsValid { get => _isValid; }

        public override void UpdatePositions(Vector2Int p1, Vector2Int p2)
        {
            if (!_canContinue || line.Count >= _limit) return;
            base.UpdatePositions(p1, p2);

            int linePositions = line.Count < _limit ? line.Count : _limit;
            if (linePositions < 3) return;

            List<Vector2Int> directions = new ();
            for (int i = 1; i < linePositions; i++)
            {
                directions.Add(line[i].Item1 - line[i - 1].Item1);
            }
            for (int i = 1; i < linePositions - 1; i++)
            {
                switch (i)
                {
                    case 1:
                        if (directions[i] != directions[i - 1])
                        {
                            currentColor = Color.red;
                            _canContinue = false;
                        }
                        break;
                    case 2:
                        if (directions[i] == directions[i - 1])
                        {
                            currentColor = Color.green;
                            _canContinue = false;
                            _isValid = true;
                        }
                        else if (!IsRotation(directions[i], directions[i - 1]))
                        {
                            currentColor = Color.red;
                            _canContinue = false;
                        }
                        break;
                    case 3:
                        if (directions[i] == directions[i - 1])
                        {
                            currentColor = Color.green;
                            _canContinue = false;
                            _isValid = true;
                        }
                        else if (!IsRotation(directions[i], directions[i - 1]))
                        {
                            currentColor = Color.red;
                            _canContinue = false;
                        }
                        break;
                    case 4:
                        if (directions[i] == directions[i - 1])
                        {
                            currentColor = Color.green;
                            _canContinue = false; 
                            _isValid = true;
                        }
                        else
                        {
                            currentColor = Color.red;
                            _canContinue = false;
                        }
                        break;
                    default:
                        currentColor = Color.red;
                        _canContinue = false;
                        break;
                }

                if (!_canContinue) break;
            }


            bool IsRotation(Vector2Int a, Vector2Int b)
            {
                if (a == new Vector2Int(b.y, b.x) || a == new Vector2Int(-b.y, b.x) ||
                    a == new Vector2Int(b.y, -b.x) || a == new Vector2Int(-b.y, -b.x))
                {
                    return true;
                }
                return false;
            }
        }
        protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            if (line.Count < 1)
            {
                line = new() { (VectorFixedToGrid(startPosition), startPosition) };
                currentColor = Color.white;
            }

            painter.DrawPolygon(VectorIntToNormal(GetListPositions(line, ListPositionType.Fixed)), new Color(0, 0, 0, 0), currentColor, 4, false);
            painter.DrawCircle(startPosition, 16, currentColor);
            painter.DrawCircle(endPosition, 16, currentColor);
        }

        override public void LineClear()
        {
            _canContinue = true;
            _isValid = false;
            line.Clear();
        }
        public enum StairShapes
        {
            None,
            Straight,
            Corner,

        }
    }
}
