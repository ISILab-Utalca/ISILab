using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestAssistant))]
    public class QuestAssistantDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestAssistant assistant) return;
            
            QuestGraph graph = assistant.Graph;
            if (graph == null) return;
            
            UpdateTiles(target, view, tesselationSize);

            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(target, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        public override void UpdateTiles(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not QuestAssistant qa) return;

            // 1. Remove what the behavior says is expired
            RemoveExpired(qa, view);

            // 2. Paint what the behavior says is new
            PaintNewTiles(qa, view);

            // 3. Refresh positions/data for everything currently in the graph
            UpdateLoadedTiles(qa, view);
        }


        private void RemoveExpired(object target, MainView view)
        {
            var qa = (QuestAssistant)target;
            var graph = qa.Graph;
            if (graph == null) return;

            foreach (var expiredKey in qa.RetrieveExpiredTiles())
            {
                view.ClearElementFromComponent(expiredKey, qa.OwnerLayer);
            }
        }
        private void PaintNewTiles(object target, MainView view)
        {
            var qa = (QuestAssistant)target;
            var graph = qa.Graph;
            if (graph == null) return;

            foreach (object key in qa.RetrieveNewTiles())
            {
                var existing = view.GetElementsFromLayer(qa.OwnerLayer, key);
                if (existing != null && existing.Count > 0)
                    continue;

                var ve = CreateSuggestionView(key as QuestNode);
                if (ve == null) 
                    continue;

                view.AddElementToLayerContainer(qa.OwnerLayer, key, ve);
                ve.style.display = qa.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateLoadedTiles(object target, MainView view)
        {
            var qa = (QuestAssistant)target;
            var graph = qa.Graph;
            if (graph == null) return;

            bool assTabActive = LBSInspectorPanel.Instance.IsAssistantTabActive();
            bool isSelected = qa.OwnerLayer == LBSMainWindow.Instance._selectedLayer && assTabActive;
            bool layerVisible = qa.OwnerLayer.IsVisible && assTabActive;
            
            // Refresh existing Nodes
            foreach (QuestNode suggestion in qa.Suggestions)
            {
                var elements = view.GetElementsFromLayer(qa.OwnerLayer, suggestion);
                if (elements == null) continue;

                foreach (var el in elements)
                {
                    el.SetEnabled(isSelected);
                    el.style.display = layerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void LoadAllTiles(object target, MainView view)
        {
            var qa = (QuestAssistant)target;
            if (qa == null) return;

            foreach (QuestNode key in qa.Suggestions)
            {
                var existing = view.GetElementsFromLayer(qa.OwnerLayer, key);
                if (existing != null && existing.Count > 0)
                    continue;

                var ve = CreateSuggestionView(key);
                if (ve == null)
                    continue;

                view.AddElementToLayerContainer(qa.OwnerLayer, key, ve);
                // not display by default as it should only display if the assistant tab is open
                ve.style.display = DisplayStyle.None;

            }
        }

        public override void HideVisuals(object target, MainView view)
          => ToggleVisuals(target, view, DisplayStyle.None);

        public override void ShowVisuals(object target, MainView view)
        {
            if (target is not QuestAssistant qa || qa.Graph == null || qa.OwnerLayer == null)
            {
                HideVisuals(target, view);
                return;
            }

            bool assTabActive = LBSInspectorPanel.Instance.IsAssistantTabActive();
            bool isSelected = qa.OwnerLayer == LBSMainWindow.Instance._selectedLayer;
            bool layerVisible = qa.OwnerLayer.IsVisible;

            if (assTabActive && isSelected && layerVisible)
                ToggleVisuals(target, view, DisplayStyle.Flex);
            else
                HideVisuals(target, view);
        }

        private void ToggleVisuals(object target, MainView view, DisplayStyle style)
        {
            if (target is not QuestAssistant qa || qa.Graph == null || qa.OwnerLayer == null)
                return;

            var graph = qa.Graph;
            var layer = qa.OwnerLayer;

            foreach(var suggest in qa.Suggestions)
            {
                var elements = view.GetElementsFromLayer(layer, suggest);
                if (elements == null || elements.Count == 0) continue;

                foreach (var el in elements)
                {
                    el.style.display = style;
                }
            }
        }


        private static SuggestionElementArea CreateSuggestionView(QuestNode node) => new(node, node.Data.Area.value);

    }
}