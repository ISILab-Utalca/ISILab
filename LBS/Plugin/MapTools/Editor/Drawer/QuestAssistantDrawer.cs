using ISILab.AI.Optimization.Populations;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestAssistant))]
    public class QuestAssistantDrawer : Drawer
    {
        private readonly Dictionary<QuestNode, SuggestionElementArea> _suggestionViews = new();
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestAssistant assistant) return;
            if (assistant.OwnerLayer is not { } layer) return;
            
            QuestGraph graph = assistant.Graph;
            if (graph == null) return;
            
            layer.OnChange += OnLayerChange(graph, assistant);
            
            _suggestionViews.Clear();
            LoadAllTiles(graph, assistant, view);

            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(graph, assistant, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        private Action OnLayerChange(QuestGraph graph, QuestAssistant assistant)
        {
            return () =>
            {
                // Reset layer input when changing to another layer
                _suggestionViews.Clear();

            };
        }

        private void LoadAllTiles(QuestGraph questGraph, QuestAssistant assistant, MainView view)
        {  
            foreach (QuestNode suggestNode in assistant.Suggestions)
            {
                // only draw suggestions when the assistant tab is active
                if(!LBSInspectorPanel.Instance.IsAssistantTabActive())
                {
                    break;
                }
                
                if (!Equals(LBSMainWindow.Instance._selectedLayer, assistant.OwnerLayer)) continue;
                
                _suggestionViews.TryGetValue(suggestNode, out SuggestionElementArea suggestView);
              
                // if not successfully created
                if(suggestView is null)
                {
                    // make a quest action visual element
                    suggestView = CreateSuggestionView(suggestNode);
                    _suggestionViews.Add(suggestNode, suggestView);
                }
                
                if (assistant.displaySuggestions)
                {
                    suggestView.style.display = (DisplayStyle)(assistant.OwnerLayer.IsVisible ? 0 : 1);
                }
                else
                {
                    suggestView.style.display = DisplayStyle.None;
                }
                //assistant.Keys.Add(suggestNode);
            }
            
            foreach (var entry in _suggestionViews)
            {
                view.AddElementToLayerContainer(questGraph.OwnerLayer, entry.Key, entry.Value);
            }
            
        }

        public override void ShowVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not QuestBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                var elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile)?.Where(graphElement => graphElement != null);
                if (elements == null) continue;
                foreach (GraphElement graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        { 
            foreach (var ve in _suggestionViews.Values)
            {
                ve.style.display = DisplayStyle.None;
            }
        }
        
        private static SuggestionElementArea CreateSuggestionView(QuestNode node)
        {
            SuggestionElementArea nodeView = new(node, node.Data.Area.value);
            
            return nodeView;
        }
    }
}