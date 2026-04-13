using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class QuestBehaviour : LBSBehaviour, IBlueprintable
    {
        public Type activeGraphNodeType = null;
        public string ActionToSet { get; set; }

        private GraphNode _selectedNode;

        #region PROPERTIES

        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();

        public GraphNode SelectedGraphNode
        {
            get => _selectedNode;
            set
            {
                if (value is not null && _selectedNode is not null)
                {
                    if (value.Equals(_selectedNode)) return;
                }

                _selectedNode = value;
                _onNodeSelected?.Invoke(_selectedNode);
            }
        }


        public QuestActionData SelectedNodeData => SelectedQuestNode?.Data;

        public QuestNode SelectedQuestNode => SelectedGraphNode as QuestNode;
        #endregion

        #region ACTIONS

        private Action<GraphNode> _onNodeSelected;

        public event Action<GraphNode> OnGraphNodeSelected
        {
            add { _onNodeSelected += value; }
            remove { _onNodeSelected -= value; }
        }

        #endregion


        #region CONSTRUCTOR

        public QuestBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        #endregion

        #region METHODS

        public override void OnGUI()
        {

        }
        
        public override object Clone()
        {
            return new QuestBehaviour(IconGuid, Name, ColorTint);
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
            layer.OnChange += UpdateKeys;

            OwnerLayer.GetModule<QuestGraph>().OnAddNode += OnAddNode;
            OwnerLayer.GetModule<QuestGraph>().OnRemoveNode += OnRemoveNode;
        }

        private void OnAddNode(QuestNode node) => SelectedGraphNode = node;

        private void OnRemoveNode(QuestNode node)
        {
            if (SelectedGraphNode == node) SelectedGraphNode = null;
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;
        }

        public override void CheckKeys()
        {
            UpdateKeys(Graph.GraphNodes.ToList<object>());
        }

        public void UpdateKeys()
        {
            UpdateKeys(Graph.GraphNodes.ToList<object>());
        }

        public void NodeDataChanged(GraphNode node)
        {
            if (Equals(_selectedNode, node)) return;
            _onNodeSelected?.Invoke(node);
        }


        #region IBLUEPRINTABLE
        public bool CaptureAreaData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            List<GraphNode> nodesToRemove = new List<GraphNode>(Graph.GraphNodes);
            List<QuestEdge> edgesToRemove = new List<QuestEdge>(Graph.GraphEdges);

            foreach (GraphNode node in Graph.GraphNodes)
            {
                Vector2Int nodePos = Vector2Int.zero;
                if (node is QuestNode qn)
                {
                    nodePos = qn.Data.Area.position.ToInt();
                }
                bool inside =
                    nodePos.x >= corners.min.x &&
                    nodePos.x <= corners.max.x &&
                    nodePos.y >= corners.min.y &&
                    nodePos.y <= corners.max.y;

                if (inside)
                {
                    nodesToRemove.Remove(node);
                }
            
            }

            foreach (QuestEdge edge in Graph.GraphEdges)
            {
                bool fromInside = nodesToRemove.Exists(n => edge.From.Contains(n));
                bool toInside = nodesToRemove.Exists(n => n.ID == edge.To.ID);

                if (fromInside && toInside)
                {
                    edgesToRemove.Remove(edge);
                }
            }

            foreach (var node in nodesToRemove) Graph.RemoveQuestNode(node);
            foreach (var edge in edgesToRemove) Graph.RemoveEdge(edge);

            return Graph.GraphNodes.Count > 0 || Graph.GraphEdges.Count > 0;
        }

        public void SetPosition(Vector2Int parentAnchor, Vector2Int delta)
        {
            // Grid coordinates use inverted Y compared to GraphView
            Vector2Int parentAnchorView = new(parentAnchor.x, -parentAnchor.y);

            Vector2 parentAnchorViewPos = OwnerLayer.FixedToPosition(parentAnchorView);
            Vector2 deltaView = OwnerLayer.FixedToPosition(delta);
            deltaView.y *= -1;

            foreach (var node in Graph.GraphNodes)
            {

                Vector2Int distanceToAnchor = node.Position - parentAnchor;
                node.Position = delta + distanceToAnchor;

                Vector2 distanceToAnchorView = node.NodeViewPosition.position - parentAnchorViewPos;

                Vector2 newViewPos = deltaView + distanceToAnchorView;

                node.NodeViewPosition = new Rect(
                    newViewPos,
                    node.NodeViewPosition.size
                );
            }
        }

        public Vector2Int GetAnchor()
        {
            Vector2Int anchor = new Vector2Int(int.MaxValue, int.MinValue);
            if (OwnerLayer is null) return anchor;
   
            foreach (var node in Graph.GraphNodes)
            {
                if (node.Position.x < anchor.x) anchor.x = node.Position.x;
                if (node.Position.y > anchor.y) anchor.y = node.Position.y;
            }

            return OwnerLayer.ToFixedPosition(anchor);
        }

        public bool MergeLayerData(object incoming, bool overwrite)
        {
            QuestBehaviour merger = incoming as QuestBehaviour;
            if (merger == null) return false;

            for (int i = 0; i < merger.Graph.GraphNodes.Count; i++)
            {
                var incomingNode = merger.Graph.GraphNodes[i];

                GraphNode existingNode = null;

                for (int j = 0; j < Graph.GraphNodes.Count; j++)
                {
                    var node = Graph.GraphNodes[j];

                    if (node.ID == incomingNode.ID)
                    {
                        existingNode = node;
                        break;
                    }
                }

                if (existingNode == null)
                {
                    Graph.AddNodeToGraph(incomingNode.Clone() as GraphNode);
                }
                else if (overwrite)
                {
                    existingNode = incomingNode.Clone() as GraphNode;
                }
            }

            for (int i = 0; i < merger.Graph.GraphEdges.Count; i++)
            {
                var incomingEdge = merger.Graph.GraphEdges[i];

                //QuestEdge existingEdge = null;

                for (int j = 0; j < Graph.GraphEdges.Count; j++)
                {
                    var edge = Graph.GraphEdges[j];

                    bool fromMatch = true;

                    for (int k = 0; k < edge.From.Count; k++)
                    {
                        var node = edge.From[k];

                        if (incomingEdge.From.Exists(n => n.ID == node.ID))
                        {
                            continue;
                        }

                        fromMatch = false;
                    }

                    bool exists = edge.To == incomingEdge.To && fromMatch;

                    if (!exists)
                    {
                        var newEdge = incomingEdge.Clone() as QuestEdge;

                        for (int f = 0; f < newEdge.From.Count; f++)
                        {
                            var incomingFrom = newEdge.From[f];
                            Graph.AddEdge(incomingFrom, newEdge.To);
                        }
                    }
                    else if (overwrite)
                    {
                        edge = incomingEdge.Clone() as QuestEdge;
                    }
                }
            }

            return true;
        }

        #endregion

        #endregion
    }
}