using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using Node = ISILab.LBS.Components.Node;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestBehaviour))]
    public class QuestBehaviorDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestBehaviour bh)
                return;

         

            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(bh, view);
                Loaded = true;
                FullRedrawRequested = false;
            }

            UpdateLoadedTiles(bh, view);
        }

        public override void UpdateTiles(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not QuestBehaviour bh) 
                return;

            PaintNewTiles(bh, view);

            RemoveExpired(bh, view);

            UpdateLoadedTiles(bh, view);
        }

        private void RemoveExpired(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;


            foreach (var expiredKey in bh.RetrieveExpiredTiles())
            {
                Debug.Log($"Removing {expiredKey}");

                view.ClearElementFromComponent(expiredKey, bh.OwnerLayer);
            }
        }
        private void PaintNewTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;
            var graph = bh.Graph;
            if (graph == null) 
                return;

            foreach (object newKey in bh.RetrieveNewTiles())
            {

                Debug.Log($"Adding {newKey}");

                var existing = view.GetElementsFromLayer(bh.OwnerLayer, newKey);
                if (existing != null && existing.Count > 0) 
                    continue;

                VisualElement ve = null;

                if (newKey is Components.Node node)
                {
                    ve = node switch
                    {
                        QuestNode qn => CreateActionView(qn),
                        BranchNode bn => CreateBranchView(bn),
                        _ => null
                    };
                    if (ve is QuestGraphNodeView nodeView)
                    {
                        nodeView.SelectView(node.IsSelected());
                    }
                }
                else if (newKey is Modules.Edge edge)
                {
                    var fromViews = view.GetElementsFromLayer(bh.OwnerLayer, edge.From);
                    var toViews = view.GetElementsFromLayer(bh.OwnerLayer, edge.To);

                    if (fromViews == null || fromViews.Count == 0 
                        || toViews==null || toViews.Count == 0) 
                        continue;

                    QuestGraphNodeView toView = toViews.FirstOrDefault() as QuestGraphNodeView;
                    QuestGraphNodeView fromView = fromViews.FirstOrDefault() as QuestGraphNodeView;

                    if (toView != null && fromView != null)
                    {
                        var edgeView = CreateEdgeView(graph, edge, fromView, toView);
                        edgeView.layer = fromView.layer + 1;
                        ve = edgeView;
                    }
                }

                if (ve != null)
                {
                    view.AddElementToLayerContainer(bh.OwnerLayer, newKey, ve as GraphElement);
                    ve.style.display = bh.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void UpdateLoadedTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;

            bh.Keys.RemoveWhere(item => item == null);
            
            var graph = bh.Graph;
            if (graph == null) return;

            bool isSelected = bh.OwnerLayer == LBSMainWindow.Instance.SelectedLayer;
   //         Debug.Log($"Layer:{graph.OwnerLayer.Name}: selected => {isSelected}");
            bool layerVisible = bh.OwnerLayer.IsVisible;
            var pickMode = isSelected ? PickingMode.Position : PickingMode.Ignore;

            // Refresh existing Nodes
            foreach (object node in graph.Nodes)
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
            foreach (Modules.Edge edge in graph.Edges)
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, edge);
                if (elements == null) 
                    continue;

                foreach (var el in elements)
                {
                    if (el is not QuestEdgeView edgeView) continue;
                    edgeView.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    edgeView.UpdatePositions();
                }
            }


            var allElements = view.GetAllElementsInLayer(bh.OwnerLayer);
            foreach(var element in allElements)
                element.SetEnabled(isSelected);
            


        }

        private void LoadAllTiles(object target, MainView view)
        {
            var bh = (QuestBehaviour)target;

            var graph = bh.Graph;
            var layer = graph.OwnerLayer;
            if (graph == null) return;

            bool isSelected = layer == LBSMainWindow.Instance.SelectedLayer;

            foreach (var node in graph.Nodes)
            {
                var elements = view.GetElementsFromLayer(layer, node);

                /*
                Debug.Log(
                $"{(node as Node).ID} " +
                $"nodeHash={node.GetHashCode()} " +
                $"views={(elements?.Count ?? 0)}");
                */

                var existing = view.GetElementsFromLayer(layer, node);
                if (existing != null && existing.Count > 0)
                    continue;

                QuestGraphNodeView nodeView = node switch
                {
                    QuestNode qn => CreateActionView(qn),
                    BranchNode bn => CreateBranchView(bn),
                    _ => null
                };

                if (nodeView == null) continue;

                // disable when loading levels
                nodeView.SetEnabled(isSelected);

                view.AddElementToLayerContainer(layer, node, nodeView);
            }

            foreach (Modules.Edge edge in graph.Edges)
            {

                var existing = view.GetElementsFromLayer(layer, edge);
                if (existing != null && existing.Count > 0)
                    continue;

                QuestGraphNodeView toView = view
                    .GetElementsFromLayer(layer, edge.To)
                    .FirstOrDefault() as QuestGraphNodeView;

                if (toView == null) 
                    continue;

                QuestGraphNodeView fromView = view
                    .GetElementsFromLayer(layer, edge.From)
                    .FirstOrDefault() as QuestGraphNodeView;

                if (fromView == null)
                    continue;

                var edgeView = CreateEdgeView(graph, edge, fromView, toView);

                view.AddElementToLayerContainer(layer, edge, edgeView);
                edgeView.layer = fromView.layer + 1;

                // disable when loading levels
                edgeView.SetEnabled(isSelected);
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
            foreach (Modules.Edge edge in graph.Edges)
            {
                SetKeyDisplayStyle(view, layer, edge, style);
            }

            // 2. Toggle Nodes
            foreach (var node in graph.Nodes)
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
            Graph graph,
            Modules.Edge edge,
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
        private static QuestBranchView CreateBranchView(BranchNode node) => new(node);
    }
}