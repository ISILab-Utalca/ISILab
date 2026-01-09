using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(HillClimbingAssistant))]
    public class HillClimbingDrawer : Drawer
    {
        private readonly Vector2 _nodeSize = new(100, 100);

        private Action _onChangeAction;
        
        private readonly Dictionary<Zone, LBSNodeView> _nodeRefs = new();
        private readonly HashSet<object> _keyRefs = new();

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Set target Assistant
            if (target is not HillClimbingAssistant assistant) return;
         
            ClearElements(assistant);
            
            // Get modules
            List<ConstraintPair> constraints = assistant.ConstrainsZonesMod.Constraints;

            if (assistant.OwnerLayer is not { } layer) return;
            
            //if (_onChangeAction != null) layer.OnChange -= _onChangeAction;
            layer.OnChange += CheckDrawing(view, tesselationSize, assistant, assistant.ConstrainsZonesMod.Constraints);
            //layer.OnChange += _onChangeAction;
           
            PaintElements(view, tesselationSize, assistant, constraints);
            
            //PaintNewTiles(assistant, view, consts, tesselationSize);
            //UpdateLoadedTiles(view, assistant, consts, tesselationSize);
        }

        private Action CheckDrawing(MainView view, Vector2 tesselationSize, HillClimbingAssistant assistant, List<ConstraintPair> constraints)
        {
            return () => { PaintElements(view, tesselationSize, assistant, constraints); };
        }

        private void PaintElements(MainView view, Vector2 tesselationSize, HillClimbingAssistant assistant, List<ConstraintPair> constraints)
        {
            ClearElements(assistant);

            if (!Equals(LBSMainWindow.Instance._selectedLayer, assistant.OwnerLayer)) return; 
            
            PaintEverything(assistant, view, constraints, tesselationSize);
        }

        private void ClearElements(HillClimbingAssistant assistant)
        {
            foreach (object key in _keyRefs)
            {
                MainView.Instance.ClearLayerComponentView(assistant.OwnerLayer, key);
            }

            _keyRefs.Clear();
            _nodeRefs.Clear();
        }

        private void PaintEverything(HillClimbingAssistant assistant, MainView view,
            List<ConstraintPair> constraints, Vector2 tesselationSize)
        {
            if (!assistant.OwnerLayer.IsVisible) return;
            
            foreach (Zone zone in assistant.Zones)
            {
                if(!assistant.ZonesWhitTiles.Contains(zone))
                {
                    _nodeRefs.Remove(zone);
                    continue;
                }

                // Zone node
                LBSNodeView nView = CreateNode(assistant, zone);

                if (!_nodeRefs.TryAdd(zone, nView))
                {
                    _nodeRefs[zone] = nView;
                }
                _keyRefs.Add(zone);

                if (!assistant.visibleConstraints) continue;

                // Constrains
                foreach (ConstraintPair pair in constraints)
                {
                    if (!pair.Zone.Equals(zone)) continue;
                    
                    List<DottedAreaFeedback> vws = CreateFeedBackAreas(nView, pair, tesselationSize);
                    Empty ve = new Empty();
                    foreach (DottedAreaFeedback v in vws)
                    {
                        ve.Add(v);
                    }

                    view.AddElementToLayerContainer(assistant.OwnerLayer, pair, ve);
                    assistant.SaveConstraintKey(zone,pair);
                    _keyRefs.Add(pair);
                    break;
                }
            }
            
            // Edges
            foreach (ZoneEdge edge in assistant.GetEdges())
            {
                // Get view nodes
                LBSNodeView n1 = _nodeRefs[edge.First];
                LBSNodeView n2 = _nodeRefs[edge.Second];

                // Create EdgeView
                LBSEdgeView eView = new(edge, n1, n2, 4, 4);
                
                view.AddElementToLayerContainer(assistant.OwnerLayer, edge, eView);
                _keyRefs.Add(edge);
            }

            foreach(KeyValuePair<Zone, LBSNodeView> node in _nodeRefs)
            {
                view.AddElementToLayerContainer(assistant.OwnerLayer, node.Key, node.Value);
            }
        }

        private void PaintNewTiles(HillClimbingAssistant assistant, MainView view,
            List<ConstraintPair> constraints, Vector2 tesselationSize)
        {
            object[] newTiles = assistant.RetrieveNewTiles();
            
            // Draw new Nodes
            foreach (object o in newTiles)
            {
                if (o is not Zone zone) continue;
                
                LBSNodeView nView = CreateNode(assistant, zone);
                view.AddElementToLayerContainer(assistant.OwnerLayer, zone, nView);
                
                if (!_nodeRefs.TryAdd(zone, nView))
                {
                    _nodeRefs[zone] = nView;
                }
                _keyRefs.Add(zone);

                // Constrains
                foreach (ConstraintPair pair in constraints)
                {
                    if (!pair.Zone.Equals(zone)) continue;
                    
                    
                    List<DottedAreaFeedback> vws = CreateFeedBackAreas(nView, pair, tesselationSize);
                    Empty ve = new Empty();
                    foreach (DottedAreaFeedback v in vws)
                    {
                        ve.Add(v);
                    }

                    view.AddElementToLayerContainer(assistant.OwnerLayer, pair, ve);
                    assistant.SaveConstraintKey(zone,pair);
                    _keyRefs.Add(pair);
                    break;
                }
            }
            
            // Draw new Edges
            foreach (object o in newTiles)
            {
                if (o is not ZoneEdge edge) continue;
                
                // Get view nodes
                LBSNodeView n1 = _nodeRefs[edge.First];
                LBSNodeView n2 = _nodeRefs[edge.Second];

                // Create EdgeView
                LBSEdgeView eView = new LBSEdgeView(edge, n1, n2, 4, 4);
                
                view.AddElementToLayerContainer(assistant.OwnerLayer, edge, eView);
                _keyRefs.Add(edge);
            }
        }
        
        private void UpdateLoadedTiles(MainView view, HillClimbingAssistant assistant, List<ConstraintPair> consts, Vector2 teselationSize)
        {
            // Update visuals
            foreach (object key in _keyRefs.ToList())
            {
                List<GraphElement> elements = view.GetElementsFromLayer(assistant.OwnerLayer, key);
                
                // Remove lost references
                if (elements == null)
                {
                    _keyRefs.Remove(key);
                    continue;
                }

                // Update visuals
                foreach (GraphElement element in elements.Where(element => element != null))
                {
                    switch (key)
                    {
                        case Zone zone:
                            LBSNodeView node = element as LBSNodeView;
                            if (node == null) break;
                            UpdateNode(ref node, assistant, zone);
                            break;
                        
                        case ConstraintPair keyPair:
                            Empty feedback = element as Empty;
                            if (feedback == null) break;

                            foreach (ConstraintPair pair in consts.Where(pair => keyPair.Zone.Equals(pair.Zone)))
                            {
                                UpdateFeedbackArea(ref feedback, pair, teselationSize, assistant.visibleConstraints);
                                break;
                            }
                            break;
                        
                        case ZoneEdge zEdge:
                            LBSEdgeView edgeView = element as LBSEdgeView;
                            UpdateEdge(ref edgeView, _nodeRefs[zEdge.First], _nodeRefs[zEdge.Second]);
                            break;
                        
                        default:
                            Debug.LogWarning("HillClimbingDrawer error: _keyRefs contains unsupported element type " + key);
                            break;
                    }
                    element.layer = assistant.OwnerLayer.index;
                }
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            if (target is not HillClimbingAssistant assistant) return;

            //foreach (var graphElement in view.GetElements(assistant.OwnerLayer, this).Where(graphElement => graphElement != null))
            //{
            //    graphElement.style.display = DisplayStyle.Flex;
            //}

            foreach (object tile in _keyRefs)
            {
                foreach (GraphElement graphElement in view.GetElementsFromLayer(assistant.OwnerLayer, tile).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        {
            if (target is not HillClimbingAssistant assistant) return;

            //var elements = view.GetElements(assistant.OwnerLayer, this);
            //foreach (var graphElement in elements)
            //{
            //    graphElement.style.display = DisplayStyle.None;
            //}

            foreach (object tile in _keyRefs)
            {
                if (tile == null) continue;
            
                List<GraphElement> _elements = view.GetElementsFromLayer(assistant.OwnerLayer, tile);
                foreach (GraphElement graphElement in _elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="center_old"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private List<DottedAreaFeedback> CreateFeedBackAreas(LBSNodeView nodeView, ConstraintPair pair, Vector2 size)
        {
            LBSSettings settings = LBSSettings.Instance;
            Vector2 tileSize = settings.general.TileSize;

            List<DottedAreaFeedback> cViews = new();

            Constraint constr = pair.Constraint;

            // Get points from first dotted area
            Vector2 maxV1 = new Vector2(-constr.maxWidth / 2f, -constr.maxHeight / 2f);
            Vector2 maxV2 = new Vector2(constr.maxWidth / 2f, constr.maxHeight / 2f);

            // Create first dotted area
            DottedAreaFeedback c1 = new DottedAreaFeedback();

            // Get center position
            Vector2 center = nodeView.GetPosition().center;

            // Set values to first doted area
            c1.SetPosition(new Rect(center, new Vector2(10, 10)));
            c1.UpdatePositions((maxV1 * size * tileSize).ToInt(), (maxV2 * size * tileSize).ToInt());
            c1.SetColor(Color.red);

            // Get points from second dotted area
            Vector2 minV1 = new Vector2(-constr.minWidth / 2f, -constr.minHeight / 2f);
            Vector2 minV2 = new Vector2(constr.minWidth / 2f, constr.minHeight / 2f);

            // Create second dotted area
            DottedAreaFeedback c2 = new DottedAreaFeedback();

            // Set value to second dotted area
            c2.SetPosition(new Rect(center, new Vector2(10, 10)));
            c2.UpdatePositions((minV1 * size * tileSize).ToInt(), (minV2 * size * tileSize).ToInt());
            c2.SetColor(Color.blue);

            // add constraint to list
            cViews.Add(c1);
            cViews.Add(c2);

            return cViews;
        }

        private void UpdateFeedbackArea(ref Empty feedbackArea, ConstraintPair pair, Vector2 size, bool visible)
        {
            if (feedbackArea == null) return;
            
            feedbackArea.visible = visible;
            if (!visible) return;
            
            LBSSettings settings = LBSSettings.Instance;
            Vector2 tileSize = settings.general.TileSize;
            Constraint constr = pair.Constraint;

            VisualElement[] areas = feedbackArea.Children().ToArray();
            DottedAreaFeedback a1 = (DottedAreaFeedback) areas[0];
            DottedAreaFeedback a2 = (DottedAreaFeedback) areas[1];
            _nodeRefs.TryGetValue(pair.Zone, out LBSNodeView nodeView);
            
            if(a1 == null || a2 == null || nodeView == null) return;
            Vector2 center = nodeView.GetPosition().center;
            
            // -------- First dotted area --------
            // Get points 
            Vector2 maxV1 = new Vector2(-constr.maxWidth / 2f, -constr.maxHeight / 2f);
            Vector2 maxV2 = new Vector2(constr.maxWidth / 2f, constr.maxHeight / 2f);
            
            // Set values
            a1.SetPosition(new Rect(center, new Vector2(10, 10)));
            a1.UpdatePositions((maxV1 * size * tileSize).ToInt(), (maxV2 * size * tileSize).ToInt());
            a1.SetColor(Color.red);
            
            // -------- Second dotted area --------
            // Get points from second dotted area
            Vector2 minV1 = new Vector2(-constr.minWidth / 2f, -constr.minHeight / 2f);
            Vector2 minV2 = new Vector2(constr.minWidth / 2f, constr.minHeight / 2f);

            // Set value to second dotted area
            a2.SetPosition(new Rect(center, new Vector2(10, 10)));
            a2.UpdatePositions((minV1 * size * tileSize).ToInt(), (minV2 * size * tileSize).ToInt());
            a2.SetColor(Color.blue);
        }

        private LBSNodeView CreateNode(HillClimbingAssistant assistant, Zone zone)
        {
            // Create node view
            LBSNodeView nView = new LBSNodeView();

            List<LBSTile> tiles = assistant.GetTiles(zone);
            Rect bound = tiles.GetBounds();

            // Set position
            Vector2 pos = new Vector2(
                bound.center.x * _nodeSize.x - _nodeSize.x / 2f,
                -(bound.center.y * _nodeSize.y - _nodeSize.y / 2f));

            // Set view values
            nView.SetPosition(new Rect(pos, _nodeSize));
            nView.SetText(zone.ID);
            nView.SetColor(zone.Color);

            return nView;
        }

        private void UpdateNode(ref LBSNodeView nView, HillClimbingAssistant assistant, Zone zone)
        {
            List<LBSTile> tiles = assistant.GetTiles(zone);
            Rect bound = tiles.GetBounds();

            // Set position
            Vector2 pos = new Vector2(
                bound.center.x * _nodeSize.x - _nodeSize.x / 2f,
                -(bound.center.y * _nodeSize.y - _nodeSize.y / 2f));

            // Set view values
            nView.SetPosition(new Rect(pos, _nodeSize));
            nView.SetText(zone.ID);
            nView.SetColor(zone.Color);
        }

        private void UpdateEdge(ref LBSEdgeView edgeView, LBSNodeView node1, LBSNodeView node2)
        {
            Vector2Int sPos1 = new Vector2Int((int)node1.GetPosition().center.x, (int)node1.GetPosition().center.y);
            Vector2Int sPos2 = new Vector2Int((int)node2.GetPosition().center.x, (int)node2.GetPosition().center.y);
            edgeView.ActualizePositions(sPos1, sPos2);
        }
    }
}