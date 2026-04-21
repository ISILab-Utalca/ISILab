using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestBehaviour))]
    public class QuestBehaviorDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestBehaviour bh) return;

            // First time or forced redraw: Load everything
            if (!Loaded || FullRedrawRequested)
            {
                // Clear existing just in case
                HideVisuals(bh, view);

                LoadAllTiles(bh, view);
                Loaded = true;
                FullRedrawRequested = false;
            }

            Update(bh, view, tesselationSize);
        }

        public override void Update(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not QuestBehaviour bh) return;

            // 1. Remove what the behavior says is expired
            RemoveExpired(bh, view);

            // 2. Paint what the behavior says is new
            PaintNewTiles(bh, view);

            // 3. Refresh positions/data for everything currently in the graph
            UpdateLoadedTiles(bh, view);
        }

        private void RemoveExpired(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) return;

            // Use the base LBSBehaviour method to get objects marked for removal
            foreach (var expiredKey in bh.RetrieveExpiredTiles())
            {
                var existing = view.GetElementsFromLayer(bh.OwnerLayer, expiredKey);
                if (existing == null) continue;

                foreach (var element in existing.ToList()) // ToList to avoid modification errors
                {
                    view.Remove(element);
                }
            }
        }

        private void PaintNewTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) return;

            // Consume the new tile requests from the behavior
            foreach (object key in bh.RetrieveNewTiles())
            {
                VisualElement ve = null;

                // Handle Nodes
                if (key is GraphNode node)
                {
                    ve = node switch
                    {
                        QuestNode qn => CreateActionView(qn),
                        OrNode or AndNode => CreateBranchView(node),
                        _ => null
                    };
                }
                // Handle Edges (The key is a ValueTuple (QuestEdge, GraphNode))
                else if (key is ValueTuple<QuestEdge, GraphNode> edgeKey)
                {
                    var edge = edgeKey.Item1;
                    var from = edgeKey.Item2;

                    QuestGraphNodeView toView = view.GetElementsFromLayer(bh.OwnerLayer, edge.To).FirstOrDefault() as QuestGraphNodeView;
                    QuestGraphNodeView fromView = view.GetElementsFromLayer(bh.OwnerLayer, from).FirstOrDefault() as QuestGraphNodeView;

                    if (toView != null && fromView != null)
                    {
                        var edgeView = CreateEdgeView(graph, edge, fromView, toView);
                        edgeView.layer = fromView.layer + 1;
                        ve = edgeView;
                    }
                }

                if (ve != null)
                {
                    view.AddElementToLayerContainer(bh.OwnerLayer, key, ve as GraphElement);
                    ve.style.display = bh.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void UpdateLoadedTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) return;

            bool layerVisible = bh.OwnerLayer.IsVisible;

            // Refresh existing Nodes
            foreach (GraphNode node in graph.GraphNodes)
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, node);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el is not QuestGraphNodeView nodeView) continue;
                    nodeView.Refresh();
                    nodeView.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            // Refresh existing Edges
            foreach (QuestEdge edge in graph.GraphEdges)
            {
                foreach (GraphNode from in edge.From)
                {
                    var key = (edge, from);
                    var elements = view.GetElementsFromLayer(bh.OwnerLayer, key);
                    if (elements == null) continue;

                    foreach (var el in elements)
                    {
                        if (el is not LBSQuestEdgeView edgeView) continue;
                        edgeView.UpdatePositions();
                        edgeView.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
        }

        private void LoadAllTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) return;

            foreach (GraphNode node in graph.GraphNodes)
            {
                var existing = view.GetElementsFromLayer(graph.OwnerLayer, node);
                if (existing != null && existing.Count > 0)
                    continue;

                QuestGraphNodeView nodeView = node switch
                {
                    QuestNode qn => CreateActionView(qn),
                    OrNode or AndNode => CreateBranchView(node),
                    _ => null
                };

                if (nodeView == null) continue;

                view.AddElementToLayerContainer(graph.OwnerLayer, node, nodeView);
            }

            foreach (QuestEdge edge in graph.GraphEdges)
            {
                QuestGraphNodeView toView = view
                    .GetElementsFromLayer(graph.OwnerLayer, edge.To)
                    .FirstOrDefault() as QuestGraphNodeView;

                if (toView == null) continue;

                foreach (GraphNode from in edge.From)
                {
                    var key = (edge, from);

                    QuestGraphNodeView fromView = view
                        .GetElementsFromLayer(graph.OwnerLayer, from)
                        .FirstOrDefault() as QuestGraphNodeView;

                    if (fromView == null) continue;

                    var edgeView = CreateEdgeView(graph, edge, fromView, toView);

                    view.AddElementToLayerContainer(graph.OwnerLayer, key, edgeView);
                    edgeView.layer = fromView.layer + 1;
                }
            }
        }

        public override void HideVisuals(object target, MainView view)
          => ToggleVisuals(target, view, DisplayStyle.None);

        public override void ShowVisuals(object target, MainView view)
            => ToggleVisuals(target, view, DisplayStyle.Flex);

        private void ToggleVisuals(object target, MainView view, DisplayStyle style)
        {
            if (target is not QuestBehaviour bh || bh.Graph == null || bh.OwnerLayer == null)
                return;

            var graph = bh.Graph;
            var layer = bh.OwnerLayer;

            // 1. Toggle Edges
            foreach (QuestEdge edge in graph.GraphEdges)
            {
                foreach (GraphNode from in edge.From)
                {
                    var key = (edge, from);
                    SetKeyDisplayStyle(view, layer, key, style);
                }
            }

            // 2. Toggle Nodes
            foreach (var node in graph.GraphNodes)
            {
                SetKeyDisplayStyle(view, layer, node, style);
            }
        }

        private void SetKeyDisplayStyle(MainView view, LBSLayer layer, object key, DisplayStyle style)
        {
            var elements = view.GetElementsFromLayer(layer, key);
            var ve = elements?.FirstOrDefault();
            if (ve != null)
            {
                ve.style.display = style;
            }
        }

        private static LBSQuestEdgeView CreateEdgeView(
            QuestGraph graph,
            QuestEdge edge,
            QuestGraphNodeView n1,
            QuestGraphNodeView n2)
        {
            var edgeView = new LBSQuestEdgeView(graph, edge, n1, n2, 1.5f, 3.5f); 

            n1.Refresh();
            n2.Refresh();

            return edgeView;
        }

        private static QuestNodeView CreateActionView(QuestNode node) => new(node);
        private static QuestBranchView CreateBranchView(GraphNode node) => new(node);
    }
}