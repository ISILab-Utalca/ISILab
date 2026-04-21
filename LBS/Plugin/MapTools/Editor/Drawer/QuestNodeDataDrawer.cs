using System;
using System.Linq;
using ISILab.LBS.VisualElements.Editor;
using UnityEngine;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestNodeBehaviour))]
    public class QuestNodeBehaviourDrawer : Drawer
    {
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestNodeBehaviour bh || bh.OwnerLayer == null) return;

            // 1. Only draw if this behavior belongs to the currently selected layer
            if (bh.OwnerLayer != LBSMainWindow.Instance._selectedLayer)
            {
                HideVisuals(bh, view);
                return;
            }

            // 2. Use actionData as the key
            QuestNodeData actionData = bh.SelectedNodeData;
            if (actionData?.Node == null) return;

            // Check if TriggerElementArea already exists for this actionData
            var existing = view.GetElementsFromLayer(bh.OwnerLayer, actionData);
            if (existing != null && existing.Count > 0)
            {
                ShowVisuals(bh, view);
                return;
            }

            // 3. Find the parent QuestNodeView
            // Note: We use actionData.Node to find the parent view, but actionData as the key for the trigger
            var nodeElements = view.GetElementsFromLayer(bh.OwnerLayer, actionData.Node);
            var selectedActionView = nodeElements?.FirstOrDefault() as QuestNodeView;

            if (selectedActionView != null)
            {
                var triggerView = new TriggerElementArea(
                    actionData,
                    actionData.Area,
                    selectedActionView
                );

                // Register with actionData as key
                view.AddElementToLayerContainer(bh.OwnerLayer, actionData, triggerView);
                triggerView.style.display = bh.OwnerLayer.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public override void Update(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not QuestNodeBehaviour bh || bh.OwnerLayer == null) return;

            // Selection check to prevent updating visuals on unselected layers
            if (bh.OwnerLayer != LBSMainWindow.Instance._selectedLayer) return;

            var actionData = bh.SelectedNodeData;
            if (actionData == null) return;

            var existing = view.GetElementsFromLayer(bh.OwnerLayer, actionData)?.FirstOrDefault();
            if (existing is TriggerElementArea trigger)
            {
                // If TriggerElementArea needs per-frame updates, do them here
            }
        }

        public override void HideVisuals(object target, MainView view)
            => ToggleVisuals(target, view, DisplayStyle.None);

        public override void ShowVisuals(object target, MainView view)
            => ToggleVisuals(target, view, DisplayStyle.Flex);

        private void ToggleVisuals(object target, MainView view, DisplayStyle style)
        {
            if (target is not QuestNodeBehaviour bh || bh.OwnerLayer == null) return;

            // Since actionData is the key, we must retrieve it from the behavior
            var actionData = bh.SelectedNodeData;
            if (actionData == null) return;


            foreach(var node in bh.Graph.GetQuestNodes())
            {
                var elements = view.GetElementsFromLayer(bh.OwnerLayer, node.Data);
                if (elements == null) return;

                foreach (var el in elements)
                {
                    if (node.Data != actionData) el.style.display = DisplayStyle.None;
                    else if (el != null) el.style.display = style;
                }
            }

        }
    }
}