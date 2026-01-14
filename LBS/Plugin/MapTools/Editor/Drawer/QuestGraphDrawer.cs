using System;
using ISILab.LBS.VisualElements.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ISILab.LBS.Behaviours;
using ISILab.LBS.VisualElements;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using ISILab.AI.Optimization.Populations;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestBehaviour))]
    public class QuestGraphDrawer : Drawer
    {
        // for actions, and ors,
        private readonly Dictionary<GraphNode, QuestGraphNodeView> _actionViews = new();
        private readonly Dictionary<QuestNode, SuggestionElementArea> _suggestionViews = new();
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestBehaviour behaviour) return;
            if (behaviour.OwnerLayer is not { } layer) return;
            
            QuestGraph graph = behaviour.Graph;
            if (graph == null) return;
            
            layer.OnChange += OnLayerChange(graph, behaviour);
            
            _actionViews.Clear();
            _suggestionViews.Clear();
            LoadAllTiles(graph, behaviour, view);

            /* Unused drawing system
            view.ClearLayerContainer(behaviour.OwnerLayer, true);
            PaintNewTiles(quest, behaviour, nodeViews, view);
            view.ClearLayerComponentView(behaviour.OwnerLayer, behaviour);
            view.ClearLayerComponentView(behaviour.OwnerLayer, behaviour.Graph);
            */

            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(graph, behaviour, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        private Action OnLayerChange(QuestGraph graph, QuestBehaviour behaviour)
        {
            return () =>
            {
                // Reset layer input when changing to another layer
                graph.SelectedGraphNode = null;
                behaviour.ActionToSet = string.Empty;
                _suggestionViews.Clear();
                QuestGraphNodeView.Deselect();

            };
        }

        private void LoadAllTiles(QuestGraph questGraph, QuestBehaviour behaviour, MainView view)
        {
            QuestGraphNodeView.Deselect();
            QuestGraphNodeView selectedGraphView = null;
            
            foreach (GraphNode node in questGraph.GraphNodes)
            {
                if (!_actionViews.TryGetValue(node, out QuestGraphNodeView nodeView) || nodeView == null)
                {
                    nodeView = node switch
                    {
                        // make a quest action visual element
                        QuestNode qn => CreateActionView(qn),
                        // make a branch visual element
                        OrNode or AndNode => CreateBranchView(node),
                        _ => null
                    };

                    _actionViews[node] = nodeView;
                }
                
                if (Equals(LBSMainWindow.Instance._selectedLayer, behaviour.OwnerLayer))
                {
                    if (behaviour.Graph.SelectedGraphNode is not null)
                    {
                        // to find the highlighted element is within the active quest layer
                        nodeView?.IsSelected(Equals(node, behaviour.Graph.SelectedGraphNode));
                    }

                }
                
                // if not successfully created
                if(nodeView is null) continue;
           
                if(nodeView.IsSelectedView()) selectedGraphView = nodeView;
                
                nodeView.style.display = (DisplayStyle)(behaviour.OwnerLayer.IsVisible ? 0 : 1);
               // view.AddElementToLayerContainer(questGraph.OwnerLayer, node, nodeView);
                behaviour.Keys.Add(node);
            }
            
            foreach (QuestNode suggestNode in questGraph.Suggestions)
            {
                // only draw suggestions when the assistant tab is active
                if(!LBSInspectorPanel.Instance.IsAssistantTabActive())
                {
                    break;
                }
                
                if (!Equals(LBSMainWindow.Instance._selectedLayer, behaviour.OwnerLayer)) continue;
                
                _suggestionViews.TryGetValue(suggestNode, out SuggestionElementArea suggestView);
              
                // if not successfully created
                if(suggestView is null)
                {
                    // make a quest action visual element
                    suggestView = CreateSuggestionView(suggestNode);
                    _suggestionViews.Add(suggestNode, suggestView);
                }
                
                if (questGraph.displaySuggestions)
                {
                    suggestView.style.display = (DisplayStyle)(behaviour.OwnerLayer.IsVisible ? 0 : 1);
                }
                else
                {
                    suggestView.style.display = DisplayStyle.None;
                }
                behaviour.Keys.Add(suggestNode);
            }
            
            foreach (QuestEdge edge in questGraph.GraphEdges)
            {
                if (!_actionViews.TryGetValue(edge.To, out QuestGraphNodeView n2) || n2 == null) continue;
                foreach (GraphNode from in edge.From)
                {
                    if (!_actionViews.TryGetValue(from, out QuestGraphNodeView n1) || n1 == null) continue;
                    
                    LBSQuestEdgeView edgeView = CreateEdgeView(questGraph, edge, n1, n2);
                    view.AddElementToLayerContainer(questGraph.OwnerLayer, edge, edgeView);
                    edgeView.layer = n1.layer + 1;
                    behaviour.Keys.Add(edge);
                }
            }
            
            foreach (var entry in _actionViews)
            {
                if(entry.Value == selectedGraphView) continue;
                view.AddElementToLayerContainer(questGraph.OwnerLayer, entry.Key, entry.Value);
            }
            foreach (var entry in _suggestionViews)
            {
                view.AddElementToLayerContainer(questGraph.OwnerLayer, entry.Key, entry.Value);
            }
            
            // the selected node is the last to be added so it can be moved around on top of other nodes
            if (selectedGraphView is not null)
            {
                // key has to be the selected node
                view.AddElementToLayerContainer(questGraph.OwnerLayer, questGraph.SelectedGraphNode, selectedGraphView);
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
            // Get behaviours
            if (target is not QuestBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile)?.Where(graphElement => graphElement != null);
                if(elements == null) continue;
                foreach (GraphElement graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
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
        
        private static QuestActionView CreateActionView(QuestNode node)
        {
            QuestActionView nodeView = new(node);
            return nodeView;
        }
        

        private static QuestBranchView CreateBranchView(GraphNode node)
        {
            QuestBranchView nodeView = new(node);
            return nodeView;
        }
        
        private static SuggestionElementArea CreateSuggestionView(QuestNode node)
        {
            SuggestionElementArea nodeView = new(node, node.Data.Area);
            
            return nodeView;
        }
    }
}