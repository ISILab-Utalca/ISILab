using ISILab.AI.Grammar;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.VisualElements;
using System.Linq;
using UnityEditor.Experimental.GraphView;
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

            // load level
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(bh, view);
                Loaded = true;
                FullRedrawRequested = false;
            }

            UpdateTiles(bh, view, tesselationSize);
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
                if (node.Data == null) continue;

                var displayMode = (node.Data == bh.Graph.SelectedQuestData) ? DisplayStyle.Flex : DisplayStyle.None;

                var questNodeView = view.GetElementsFromLayer(bh.OwnerLayer, node);
                if (questNodeView == null && questNodeView.Count == 0) continue;

                foreach (var field in node.Data.GetFields<GrammarField>())
                {
                    if (field == null) return;

                    // already drawn no need to draw
                    var existing = view.GetElementsFromLayer(bh.OwnerLayer, field);
                    if (existing != null && existing.Count > 0)
                    {
                        Debug.Log("element for field " + field.name + " already exists, skipping");
                        continue;
                    }

                    // make graph element if its required
                    var visual = CreateGrammarGraphView(field, questNodeView.FirstOrDefault() as QuestNodeView);
                    if (visual == null) continue;

                    view.AddElementToLayerContainer(bh.OwnerLayer, field, visual);
                    visual.style.display = displayMode;
                }
            }
        }

        private static void PaintNewTiles(MainView view, NodeDataBehaviour bh)
        {
            foreach (var tile in bh.RetrieveNewTiles())
            {
                var field = tile as GrammarField;
                if (field == null) return;

                var displayMode = (field.data == bh.Graph.SelectedQuestData) ? DisplayStyle.Flex : DisplayStyle.None;

                // already drawn no need to draw
                var existing = view.GetElementsFromLayer(bh.OwnerLayer, field);
                if (existing != null && existing.Count > 0)
                { 
                    Debug.Log("element for field " + field.name + " already exists, skipping");
                    continue; 
                }

                var questNodeView = view.GetElementsFromLayer(bh.OwnerLayer, field.data.Node);
                if (questNodeView == null && questNodeView.Count == 0) continue;

                // make graph element if its required
                var visual = CreateGrammarGraphView(field, questNodeView.FirstOrDefault() as QuestNodeView);
                if (visual == null) continue;

                view.AddElementToLayerContainer(bh.OwnerLayer, field, visual);
                visual.style.display = displayMode;
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
            foreach (var key in bh.Keys)
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, key);
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

            foreach (var key in bh.Keys)
            {
                var field = key as GrammarField;
                var data = field?.data;
                if (data != bh.SelectedNodeData) continue;

                    var elements = view.GetElementsFromLayer(bh.OwnerLayer, key);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    if (el == null) continue;
                    el.style.display = DisplayStyle.Flex;
                }
            }
        }

        public static GraphElement CreateGrammarGraphView(GrammarField field, QuestNodeView parentView)
        {
            if (field is GrammarArea area && area.GetValue() != null)
            {
                return new TriggerElementArea(area, parentView);
            }

            return null; 
        }
    }
}