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
        /// <summary>
        /// Draws the information that corresponds to the quest node behavior selected node.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="view"></param>
        /// <param name="tesselationSize"></param>
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestNodeBehaviour behaviour) return;
            if (behaviour.OwnerLayer is not { } layer) return;

            QuestNodeData actionData = behaviour?.SelectedNodeData;
            if (actionData is null) return;

            DisplayStyle display = behaviour.OwnerLayer.IsVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // 🔍 Try get existing
            var existing = view.GetElementsFromLayer(layer, behaviour);
            TriggerElementArea triggerView = null;

            if (existing != null && existing.Count > 0)
            {
                return;
            }
            else
            {
                var nodeData = behaviour.SelectedNodeData;
                if (nodeData?.Node == null) return;

                var elements = view.GetElementsFromLayer(layer, nodeData.Node);
                if (elements == null || !elements.Any()) return;

                var selectedActionView = elements.First() as QuestNodeView;
                if (selectedActionView == null) return;

                triggerView = new TriggerElementArea(
                    actionData,
                    actionData.Area,
                    selectedActionView
                );

                view.AddElementToLayerContainer(layer, behaviour, triggerView);
            }

            if (triggerView != null)
            {
                triggerView.style.display = display;
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not QuestNodeBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                foreach (GraphElement graphElement in view.GetElementsFromLayer(behaviour.OwnerLayer, tile).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not QuestNodeBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile);
                foreach (GraphElement graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
            }
        }
    }        
    
}