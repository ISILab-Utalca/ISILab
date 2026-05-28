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


        #region PROPERTIES

        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();

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

            layer.OnChange += () =>
            {
                Graph.SelectedGraphNode = null;
                UpdateKeys();
            };

            Graph.OnAddNode += (node) =>
            {
                RequestTilePaint(node);
            };

            Graph.OnAddEdge += (edge) =>
            {

                RequestTilePaint(edge);
            };

            Graph.OnRemoveNode += (node) =>
            {
                RequestTileRemove(node);
            };

            Graph.OnRemoveEdge += (edge) =>
            {
                RequestTileRemove(edge);
            };
        }


        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;
        }

        public override void CheckKeys()
        {
            UpdateKeys();
        }

        public void UpdateKeys()
        {
            if (Graph == null) return;

            List<object> allKeys = new List<object>();

            // Add Node as keys
            foreach (var node in Graph.GraphNodes)
            {
                allKeys.Add(node);
            }

            // Add Edges AS TUPLES (as they are registered in PaintNewTiles/LoadAllTiles in Drawer)
            foreach (var edge in Graph.GraphEdges)
            {
                allKeys.Add(edge);
            }

            UpdateKeys(allKeys);
        }


        #region IBLUEPRINTABLE
        public bool CaptureAreaData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            // Start with empty lists of what we actually want to delete
            List<GraphNode> nodesToRemove = new List<GraphNode>();
            List<QuestEdge> edgesToRemove = new List<QuestEdge>();

            foreach (GraphNode node in Graph.GraphNodes)
            {
                Vector2Int nodePos = OwnerLayer.ToFixedPosition(node.NodePosition.center);
                bool inside =
                    nodePos.x >= corners.min.x &&
                    nodePos.x <= corners.max.x &&
                    nodePos.y >= corners.min.y &&
                    nodePos.y <= corners.max.y;


                if (!inside)
                {
                    nodesToRemove.Add(node);
                }
            }

            foreach (QuestEdge edge in Graph.GraphEdges)
            {
                bool fromIsDeleted = nodesToRemove.Contains(edge.From);
                bool toIsDeleted = nodesToRemove.Contains(edge.To);

                if (fromIsDeleted || toIsDeleted)
                {
                    edgesToRemove.Add(edge);
                }
            }


            foreach (var node in nodesToRemove) Graph.RemoveNode(node);
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

                Vector2 distanceToAnchorView = node.NodePosition.position - parentAnchorViewPos;

                Vector2 newViewPos = deltaView + distanceToAnchorView;

                node.NodePosition = new Rect(
                    newViewPos,
                    node.NodePosition.size
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
                    Graph.AddNode(incomingNode.Clone() as GraphNode);
                }
                else if (overwrite)
                {
                    existingNode = incomingNode.Clone() as GraphNode;
                }
            }

            for (int i = 0; i < merger.Graph.GraphEdges.Count; i++)
            {
                var incomingEdge = merger.Graph.GraphEdges[i];


                for (int j = 0; j < Graph.GraphEdges.Count; j++)
                {
                    var edge = Graph.GraphEdges[j];
                    bool exists = edge.To.ID == incomingEdge.To.ID && edge.From.ID == incomingEdge.From.ID;

                    if (!exists)
                    {
                        var newEdge = incomingEdge.Clone() as QuestEdge;
                        Graph.AddEdge(newEdge.From, newEdge.To);
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