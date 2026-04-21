using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.VisualElements;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Dropdown;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(NodeDataBehaviour))]
    public class NodeBehaviourDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null) return;

            PaintNewTiles(view, bh);

            // load level
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(bh, view);
                Loaded = true;
                FullRedrawRequested = false;

                HideVisuals(bh, view);
            }

        }

        public override void UpdateTiles(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null) return;

            foreach (var expiredData in bh.RetrieveExpiredTiles())
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, expiredData);
                if (elements == null || elements.Count == 0) continue;

                foreach (var el in elements.ToList()) view.Remove(el);
            }

            PaintNewTiles(view, bh);
            UpdateVisibility(bh, view);
        }

        private void LoadAllTiles(NodeDataBehaviour bh, MainView view)
        {
            foreach (var node in bh.Graph.GetQuestNodes())
            {
                if (node.Data == null) continue;

                var existing = view.GetElementsFromLayer(bh.OwnerLayer, node.Data);
                if (existing != null && existing.Count > 0) continue;

                CreateTriggerForNode(view, bh, node.Data);
            }
        }

        private static void PaintNewTiles(MainView view, NodeDataBehaviour bh)
        {
            foreach (var tile in bh.RetrieveNewTiles())
            {
                if (tile is QuestNodeData newNodeData)
                {
                    var existing = view.GetElementsFromLayer(bh.OwnerLayer, newNodeData);
                    if (existing != null && existing.Count > 0) continue;

                    CreateTriggerForNode(view, bh, newNodeData);
                }
            }
        }

        private static void CreateTriggerForNode(MainView view, NodeDataBehaviour bh, QuestNodeData data)
        {
            if (data == null) return;

            var nodeElements = view.GetElementsFromLayer(bh.OwnerLayer, data.Node);
            var parentView = nodeElements?.FirstOrDefault() as QuestNodeView;

            if (parentView != null)
            {
                var triggerView = new TriggerElementArea(data, data.Area, parentView);
                view.AddElementToLayerContainer(bh.OwnerLayer, data, triggerView);

                // update visibility
                triggerView.style.display = bh.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }


        private void UpdateVisibility(NodeDataBehaviour bh, MainView view)
        {
            // if hidden -> hide everything
            if (!bh.OwnerLayer.IsVisible || bh.OwnerLayer != LBSMainWindow.Instance._selectedLayer)
            {
                HideVisuals(bh, view);
                return;
            }

            // if selected -> show only the selected one
            ShowVisuals(bh, view);
        }

        public override void HideVisuals(object target, MainView view)
        {
            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null) return;
            foreach (var node in bh.Graph.GetQuestNodes())
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, node.Data);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el == null) continue;
                    el.style.display = DisplayStyle.None;
                }
            }
        }
        public override void ShowVisuals(object target, MainView view)
        {
            // we only display the selected trigger, hide all others
            HideVisuals(target, view);

            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null
                || bh.OwnerLayer != LBSMainWindow.Instance._selectedLayer) return;

            var selectedData = bh.SelectedNodeData;
            if (selectedData == null) return;
            var elements = view.GetElementsFromLayer(bh.OwnerLayer, selectedData);
            if (elements == null) return;

            foreach (var el in elements)
            {
                el.style.display = DisplayStyle.Flex;
            }
        }
  
    
    }
}