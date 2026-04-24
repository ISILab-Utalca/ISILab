using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
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
        const float selectedOpacity = 1f;
        const float unselectedOpacity = 0.33f;

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestBehaviour bh) return;

            UpdateTiles(bh, view, tesselationSize);

            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(bh, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        public override void UpdateTiles(object target, MainView view, Vector2 teselationSize)
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

            foreach (var expiredKey in bh.RetrieveExpiredTiles())
            {
                view.ClearElementFromComponent(expiredKey, bh.OwnerLayer);
            }
        }
        private void PaintNewTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) return;

            foreach (object key in bh.RetrieveNewTiles())
            {

                var existing = view.GetElementsFromLayer(bh.OwnerLayer, key);
                if (existing != null && existing.Count > 0) 
                    continue;

                VisualElement ve = null;

                if (key is GraphNode node)
                {
                    ve = node switch
                    {
                        QuestNode qn => CreateActionView(qn),
                        OrNode or AndNode => CreateBranchView(node),
                        _ => null
                    };
                    if (ve is QuestGraphNodeView nodeView)
                    {
                        nodeView.SelectView(node.IsSelected());
                    }
                }
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

            bool isSelected = bh.OwnerLayer == LBSMainWindow.Instance._selectedLayer;
            bool layerVisible = bh.OwnerLayer.IsVisible;
            var pickMode = isSelected ? PickingMode.Position : PickingMode.Ignore;
            float opacity = isSelected ? selectedOpacity : unselectedOpacity;

            // Refresh existing Nodes
            foreach (GraphNode node in graph.GraphNodes)
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, node);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el is not QuestGraphNodeView nodeView) continue;
                    nodeView.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    nodeView.Refresh();
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
                        if (el is not QuestEdgeView edgeView) continue;
                        edgeView.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                        edgeView.UpdatePositions();
                    }
                }
            }


            var allElements = view.GetAllElementsInLayer(bh.OwnerLayer);
            foreach(var element in allElements)
            {
                //element.style.opacity = opacity;
                element.SetEnabled(isSelected);
                //DrawManager.Instance.ChangePickingMode(element, pickMode, new List<VisualElement>());
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

                // disable when loading levels
                nodeView.SetEnabled(false);

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

                    // disable when loading levels
                    edgeView.SetEnabled(false);
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
                if(ve is QuestGraphNodeView nodeView)
                    nodeView.SelectView(nodeView.Node.IsSelected());
            }
        }

        private static QuestEdgeView CreateEdgeView(
            QuestGraph graph,
            QuestEdge edge,
            QuestGraphNodeView n1,
            QuestGraphNodeView n2)
        {
            var edgeView = new QuestEdgeView(graph, edge, n1, n2, 1.5f, 3.5f); 

            n1.Refresh();
            n2.Refresh();
            edgeView.UpdatePositions();
            // Force an update after the next frame to ensure coordinates are settled
            edgeView.schedule.Execute(() => edgeView.UpdatePositions()).ExecuteLater(50);

            return edgeView;
        }

        private static QuestNodeView CreateActionView(QuestNode node) => new(node);
        private static QuestBranchView CreateBranchView(GraphNode node) => new(node);
    }
}