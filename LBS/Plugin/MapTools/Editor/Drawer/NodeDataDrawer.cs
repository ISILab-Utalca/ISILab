using ISILab.AI.Grammar;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.VisualElements;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Analytics.IAnalytic;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(NodeDataBehaviour))]
    public class NodeDataBehaviourDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null) return;

            UpdateTiles(bh, view, tesselationSize);

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

            foreach (var expiredKey in bh.RetrieveExpiredTiles())
            {
                view.ClearElementFromComponent(expiredKey, bh.OwnerLayer);
            }

            PaintNewTiles(view, bh);
            UpdateVisibility(bh, view);
        }

        private void LoadAllTiles(NodeDataBehaviour bh, MainView view)
        {
            foreach (var node in bh.Graph.GetQuestNodes())
            {
                var data = node.Data;
                if (data == null) continue;

                foreach (var areaField in data.GetFields<GrammarArea>())
                {
                    var existing = view.GetElementsFromLayer(bh.OwnerLayer, areaField);
                    if (existing != null && existing.Count > 0) continue;

                    CreateTriggerForNode(view, bh, data);
                }
            }
        }

        private static void PaintNewTiles(MainView view, NodeDataBehaviour bh)
        {
            foreach (var tile in bh.RetrieveNewTiles())
            {
                if (tile is QuestNodeData data)
                {
                    if (data == null) continue;

                    foreach (var areaField in data.GetFields<GrammarArea>())
                    {
                        var existing = view.GetElementsFromLayer(bh.OwnerLayer, areaField);
                        if (existing != null && existing.Count > 0) continue;

                        CreateTriggerForNode(view, bh, data);
                    }
                }
            }
        }

        private static void CreateTriggerForNode(MainView view, NodeDataBehaviour bh, QuestNodeData data)
        {
            if (data == null) return;

            var nodeElements = view.GetElementsFromLayer(bh.OwnerLayer, data.Node);
            var parentView = nodeElements?.FirstOrDefault() as QuestNodeView;

            var displayMode = data == bh.Graph.SelectedQuestData ? DisplayStyle.Flex : DisplayStyle.None;

            if (parentView != null)
            {
                foreach (var areaField in data.GetFields<GrammarArea>())
                {
                    if (areaField.GetValue() == null) return;

                    var triggerView = new TriggerElementArea(data, areaField, parentView);
                    view.AddElementToLayerContainer(bh.OwnerLayer, areaField, triggerView);

                    // update visibility
                    triggerView.style.display = displayMode;
                }
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
                var data = node.Data;
                if (data == null) continue;

                foreach (var areaField in data.GetFields<GrammarArea>())
                {
                    if (areaField.GetValue() == null) return;

                    var elements = view.GetElementsFromLayer(bh.OwnerLayer, areaField);
                    if (elements == null) continue;

                    foreach (var el in elements)
                    {
                        if (el == null) continue;
                        el.style.display = DisplayStyle.None;
                    }
                }
            }
        }
        public override void ShowVisuals(object target, MainView view)
        {
            // we only display the selected trigger, hide all others
            HideVisuals(target, view);

            if (target is not NodeDataBehaviour bh || bh.OwnerLayer == null
                || bh.OwnerLayer != LBSMainWindow.Instance._selectedLayer) return;

           
            foreach (var node in bh.Graph.GetQuestNodes())
            {
                if (node.Data != bh.SelectedNodeData) continue;

                var data = node.Data;
                if (data == null) continue;

                foreach (var areaField in data.GetFields<GrammarArea>())
                {
                    if (areaField.GetValue() == null) return;

                    var elements = view.GetElementsFromLayer(bh.OwnerLayer, areaField);
                    if (elements == null) continue;

                    foreach (var el in elements)
                    {
                        if (el == null) continue;
                        el.style.display = DisplayStyle.Flex;
                    }
                }
            }
        }
  
    
    }
}