using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestEdgeView : GraphElement
    {
        private const float curveBendStrength = 0.5f;
        private const float minCurveValue = 10f;

        private static VisualTreeAsset visualTree;


        private Vector2 _startPos, _endPos;
        private readonly float _lineWidth;
        private readonly float _stroke;
        private readonly Modules.Edge _edge;
        // meant to be used to access the USS color hehe
        private readonly VisualElement _viewData;
        private readonly Graph _graph;
        private readonly VisualElement _connectionView;
        private readonly QuestGraphNodeView _node1;
        private readonly QuestGraphNodeView _node2;

        public QuestEdgeView(Graph graph, Modules.Edge edge, QuestGraphNodeView node1, QuestGraphNodeView node2, float lineWidth = 5f, float stroke = 3f)
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestEdgeView");
            visualTree.CloneTree(this);

            _viewData = this.Q<VisualElement>("View");

            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _edge = edge ?? throw new ArgumentNullException(nameof(edge));
            _node1 = node1 ?? throw new ArgumentNullException(nameof(node1));
            _node2 = node2 ?? throw new ArgumentNullException(nameof(node2));
            _lineWidth = lineWidth;
            _stroke = stroke;

            ActionExtensions.AddUnique(ref node1.OnMoving, UpdatePositionFromNode1);
            ActionExtensions.AddUnique(ref node2.OnMoving, UpdatePositionFromNode2);

            UpdatePositions();
            generateVisualContent += DrawLine;
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            MarkDirtyRepaint();
        }

        private void UpdatePositionFromNode1(Rect node1Rect) => UpdatePositions();
        private void UpdatePositionFromNode2(Rect node2Rect) => UpdatePositions();

        internal void UpdatePositions()
        {
            // Fetch clean bounds
            Rect rect1 = _node1.GetSelectVisualElement().worldBound;
            Rect rect2 = _node2.GetSelectVisualElement().worldBound;

            // FIX THE JITTER: Round the rect center to avoid float precision sub-pixel jitter 
            // caused by selection borders changing margin sizes dynamically
            Vector2 pos1 = new Vector2(Mathf.Round(rect1.center.x), Mathf.Round(rect1.center.y));
            Vector2 pos2 = new Vector2(Mathf.Round(rect2.center.x), Mathf.Round(rect2.center.y));

            Vector2 dir = (pos2 - pos1).normalized;

            // Pass the clean rounded directions into your working raycast function
            var edge1 = GetRectEdgePoint(rect1, dir, 2f);
            var edge2 = GetRectEdgePoint(rect2, -dir, 10f);

            _startPos = this.WorldToLocal(edge1);
            _endPos = this.WorldToLocal(edge2);

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
            painter.strokeColor = _viewData.resolvedStyle.unityBackgroundImageTintColor;
            painter.lineWidth = _stroke;

            float deltaX = _endPos.x - _startPos.x;
            float deltaY = _endPos.y - _startPos.y;

            float handleLength = 0f;
            float directionSign = Mathf.Sign(deltaX);

            if (!Mathf.Approximately(deltaX, 0f) && !Mathf.Approximately(deltaY, 0f))
            {
                handleLength = Mathf.Max(Mathf.Abs(deltaX) * curveBendStrength, minCurveValue);
            }

            Vector2 controlPoint1 = _startPos + new Vector2(handleLength * directionSign, 0f);
            Vector2 controlPoint2 = _endPos - new Vector2(handleLength * directionSign, 0f);

            painter.DrawBezierLine(_startPos, controlPoint1, controlPoint2, _endPos,
                painter.strokeColor, painter.strokeColor);

            Vector2 midPoint = GetBezierPoint(0.5f, _startPos, controlPoint1, controlPoint2, _endPos);

            painter.BeginPath();
            painter.Arc(midPoint, 6f, 0, 360);
            painter.Fill();

            Vector2 preEndPoint = GetBezierPoint(0.9f, _startPos, controlPoint1, controlPoint2, _endPos);
            Vector2 arrowDirection = (_endPos - preEndPoint).normalized;

            if (arrowDirection == Vector2.zero)
            {
                arrowDirection = (_endPos - _startPos).normalized;
            }

            painter.DrawArrow(_endPos, arrowDirection, 12, 4f, painter.strokeColor);
            painter.DrawCircle(_startPos, 10, painter.strokeColor);

            _viewData.SetDisplay(false);
        }

        private Vector2 GetBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Set Type/Direct"), false, () => _graph.ChangeConnection(_edge, NodeKind.Terminal));
            menu.AddItem(new GUIContent("Set Type/OR"), false, () => _graph.ChangeConnection(_edge, NodeKind.Or));
            menu.AddItem(new GUIContent("Set Type/AND"), false, () => _graph.ChangeConnection(_edge, NodeKind.And));
            menu.AddSeparator("");

            menu.ShowAsContext();
            evt.StopPropagation();
        }
    }
}