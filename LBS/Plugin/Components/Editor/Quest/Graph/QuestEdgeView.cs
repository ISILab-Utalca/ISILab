using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using UnityEditor;

namespace ISILab.LBS.VisualElements
{
    public class QuestEdgeView : GraphElement
    {
        private const float curveBendStrength = 0.4f;
        
        private Vector2 _startPos, _endPos; // Use Vector2 for precise positioning
        private readonly float _lineWidth;
        private readonly float _stroke;
        private readonly QuestEdge _edge;
        private readonly QuestGraph _graph;
        private readonly VisualElement _connectionView;
        private readonly QuestGraphNodeView _node1;
        private readonly QuestGraphNodeView _node2;

        public QuestEdgeView(QuestGraph questGraph, QuestEdge edge, QuestGraphNodeView node1, QuestGraphNodeView node2, float lineWidth = 5f, float stroke = 3f)
        {
            _graph = questGraph ?? throw new ArgumentNullException(nameof(questGraph));
            _edge = edge ?? throw new ArgumentNullException(nameof(edge));
            _node1 = node1 ?? throw new ArgumentNullException(nameof(node1));
            _node2 = node2 ?? throw new ArgumentNullException(nameof(node2));
            _lineWidth = lineWidth;
            _stroke = stroke;

            // Grab the arrow view
            _connectionView = this.Q<VisualElement>("View");

            // Handle movement of first node
            ActionExtensions.AddUnique(ref node1.OnMoving, UpdatePositionFromNode1);

            // Handle movement of second node
            ActionExtensions.AddUnique(ref node2.OnMoving, UpdatePositionFromNode2);

            // Initialize positions
            UpdatePositions();

            // Draw the dotted line
            generateVisualContent += DrawLine;

            // Register right-click menu for edge type change
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            MarkDirtyRepaint();
        }

        private void UpdatePositionFromNode1(Rect node1Rect)
        {
            UpdatePositions();
        }

        private void UpdatePositionFromNode2(Rect node2Rect)
        {
            UpdatePositions();
        }

        internal void UpdatePositions()
        {
            var worldPos1 = _node1.GetSelectVisualElement().worldBound.center;
            var worldPos2 = _node2.GetSelectVisualElement().worldBound.center;
            var dir = (worldPos2 - worldPos1).normalized;
            
            var edge1 = GetRectEdgePoint(_node1.GetSelectVisualElement().worldBound, dir, 10f);   // circle offset
            var edge2 = GetRectEdgePoint(_node2.GetSelectVisualElement().worldBound, -dir, 10f);  // arrow offset

            _startPos = this.WorldToLocal(edge1);
            _endPos   = this.WorldToLocal(edge2);

            MarkDirtyRepaint();
        }

        private Vector2 GetRectEdgePoint(Rect rect, Vector2 direction, float extraOffset = 0f)
        {
            Vector2 center = rect.center;
            if (direction == Vector2.zero)
                return center;

            direction.Normalize();

            float tx = direction.x > 0
                ? (rect.xMax - center.x) / direction.x
                : (rect.xMin - center.x) / direction.x;

            float ty = direction.y > 0
                ? (rect.yMax - center.y) / direction.y
                : (rect.yMin - center.y) / direction.y;

            float t = Mathf.Min(tx, ty);

            return center + direction * (t + extraOffset);
        }


        private void DrawLine(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.strokeColor = Color.white;
            painter.lineWidth = _stroke;

            Vector2 dir = (_endPos - _startPos).normalized;
            float distance = Vector2.Distance(_startPos, _endPos);
            float bend = distance * curveBendStrength;

            // Perpendicular vectors for the bend
            Vector2 perp1 = new Vector2(-dir.y, dir.x); // Fixed: should be (-y, x) for true perpendicular
            Vector2 perp2 = new Vector2(dir.y, -dir.x);

            Vector2 controlPoint1 = _startPos + dir * (distance * 0.4f) + perp1 * bend;
            Vector2 controlPoint2 = _endPos - dir * (distance * 0.4f) + perp2 * bend;

            // 1. Draw the actual curve
            Vector2 endDir = painter.DrawBezierLine(_startPos, controlPoint1, controlPoint2, _endPos,
                painter.strokeColor, painter.strokeColor);

            // 2. Calculate the midpoint (t = 0.5)
            Vector2 midPoint = GetBezierPoint(0.5f, _startPos, controlPoint1, controlPoint2, _endPos);

            // 3. Draw the middle circle
            painter.BeginPath();
            painter.Arc(midPoint, 6f, 0, 360); // 6f is the radius, adjust as needed
            painter.Fill(); // Or painter.stroke() if you want an outline

            // Handle direction logic for the arrow
            if (Mathf.Abs(_startPos.y - _endPos.y) <= _node1.worldBound.height)
            {
                endDir = dir;
            }

            // 4. Draw decorations
            painter.DrawArrow(_endPos, endDir, 16, 3f, painter.strokeColor);
            painter.DrawCircle(_startPos, 10, painter.strokeColor);
        }

        /// <summary>
        /// Calculates a point along a cubic Bezier curve at time t (0 to 1)
        /// </summary>
        private Vector2 GetBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0; // (1-t)^3 * P0
            p += 3 * uu * t * p1; // 3 * (1-t)^2 * t * P1
            p += 3 * u * tt * p2; // 3 * (1-t) * t^2 * P2
            p += ttt * p3;         // t^3 * P3

            return p;
        }



        private void OnMouseDown(MouseDownEvent evt)
        {
            // Only right-click
            if (evt.button != (int)MouseButton.RightMouse) return;
            
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Set Type/Direct"), false, () => _graph.ChangeConnection(_edge, typeof(QuestNode)));
            menu.AddItem(new GUIContent("Set Type/OR"), false, () => _graph.ChangeConnection(_edge, typeof(OrNode)));
            menu.AddItem(new GUIContent("Set Type/AND"), false, () => _graph.ChangeConnection(_edge, typeof(AndNode)));
            menu.AddSeparator("");

            menu.ShowAsContext();
            evt.StopPropagation();
        }
    }
}