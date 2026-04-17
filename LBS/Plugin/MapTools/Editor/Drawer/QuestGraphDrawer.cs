using System.Linq;
using UnityEngine;
using ISILab.LBS.VisualElements;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;


namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestGraph))]
    public class QuestGraphDrawer : Drawer
    {

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            var graph = (QuestGraph)target;
            if (graph == null) return;

            Update(graph,view,tesselationSize);
            
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(graph, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        public override void Update(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not QuestGraph graph) return; 
            PaintNewTiles(graph, view); 
            UpdateLoadedTiles(graph, view);
        }

        private void PaintNewTiles(QuestGraph graph, MainView view)
        {

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
                nodeView.style.display = graph.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            foreach (QuestEdge edge in graph.GraphEdges)
            {
                var existing = view.GetElementsFromLayer(graph.OwnerLayer, edge);

                if (existing != null && existing.Count > 0)
                    continue; // already exists

                QuestGraphNodeView toView = view
                    .GetElementsFromLayer(graph.OwnerLayer, edge.To)
                    .FirstOrDefault() as QuestGraphNodeView;

                if (toView == null) continue;

                foreach (GraphNode from in edge.From)
                {
                    QuestGraphNodeView fromView = view
                        .GetElementsFromLayer(graph.OwnerLayer, from)
                        .FirstOrDefault() as QuestGraphNodeView;

                    if (fromView == null) continue;

                    var edgeView = CreateEdgeView(graph, edge, fromView, toView);

                    view.AddElementToLayerContainer(graph.OwnerLayer, edge, edgeView);
                    edgeView.layer = fromView.layer + 1;
                    edgeView.style.display = graph.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void UpdateLoadedTiles(QuestGraph graph, MainView view)
        {
            foreach (GraphNode node in graph.GraphNodes)
            {
                var elements = view.GetElementsFromLayer(graph.OwnerLayer, node);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el is not QuestGraphNodeView nodeView) continue;

                    if (!nodeView.visible) continue;

                    nodeView.SelectView(nodeView.Node == graph.SelectedGraphNode);
                    (nodeView as QuestNodeView)?.SetupNode(node as QuestNode);
                    nodeView.style.display = graph.OwnerLayer.IsVisible
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }

            }

            foreach (QuestEdge edge in graph.GraphEdges)
            {
                var elements = view.GetElementsFromLayer(graph.OwnerLayer, edge);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el is not LBSQuestEdgeView edgeView) continue;

                    if (!edgeView.visible) continue;

                    edgeView.UpdatePositions();

                    edgeView.style.display = graph.OwnerLayer.IsVisible
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            }
        }

        private void LoadAllTiles(QuestGraph graph, MainView view)
        {
            foreach (GraphNode node in graph.GraphNodes)
            {
                QuestGraphNodeView nodeView = node switch
                {
                    // make a quest action visual element
                    QuestNode qn => CreateActionView(qn),
                    // make a branch visual element
                    OrNode or AndNode => CreateBranchView(node),
                    _ => null
                };

                view.AddElementToLayerContainer(graph.OwnerLayer, node, nodeView);
            }

            foreach (QuestEdge edge in graph.GraphEdges)
            {
                QuestGraphNodeView toView = view.GetElementsFromLayer(graph.OwnerLayer, edge.To).FirstOrDefault() as QuestGraphNodeView;
                if (toView == null) continue;
                foreach (GraphNode from in edge.From)
                {
                    QuestGraphNodeView fromView = view.GetElementsFromLayer(graph.OwnerLayer, from).FirstOrDefault() as QuestGraphNodeView;
                    if (fromView == null) continue;

                    LBSQuestEdgeView edgeView = CreateEdgeView(graph, edge, fromView, toView);
                    view.AddElementToLayerContainer(graph.OwnerLayer, edge, edgeView);
                    edgeView.layer = fromView.layer + 1;

                }
            } 
        }



        public override void HideVisuals(object target, MainView view)
        {
            var graph = (QuestGraph)target;
            if(graph == null) return;

            foreach (QuestEdge edge in graph.GraphEdges)
            {
                var ve = view.GetElementsFromLayer(graph.OwnerLayer, edge).FirstOrDefault();
                if (ve == null) continue;
                ve.style.display = DisplayStyle.None;
            }

            foreach (var node in graph.GraphNodes)
            {
                var ve = view.GetElementsFromLayer(graph.OwnerLayer, node).FirstOrDefault();
                if (ve == null) continue;
                ve.style.display = DisplayStyle.None;
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            var graph = (QuestGraph)target;
            if (graph == null) return;

            foreach (QuestEdge edge in graph.GraphEdges)
            {
                var ve = view.GetElementsFromLayer(graph.OwnerLayer, edge).FirstOrDefault();
                if(ve == null) continue;
                ve.style.display = DisplayStyle.Flex;
            }

            foreach (var node in graph.GraphNodes)
            {
                var ve = view.GetElementsFromLayer(graph.OwnerLayer, node).FirstOrDefault();
                if (ve == null) continue;
                ve.style.display = DisplayStyle.Flex;
            }
        }

        private static LBSQuestEdgeView CreateEdgeView(QuestGraph graph, QuestEdge edge, QuestGraphNodeView n1, QuestGraphNodeView n2)
        {
            foreach (GraphNode from in edge.From)
            {
                n1.DisplayGrammarState(from);
            }

            n2.DisplayGrammarState(edge.To);
            
            return new LBSQuestEdgeView(graph, edge, n1, n2, 1.5f, 3.5f);
        }

        private static QuestNodeView CreateActionView(QuestNode node) => new(node);
        private static QuestBranchView CreateBranchView(GraphNode node) => new(node);

    }
}