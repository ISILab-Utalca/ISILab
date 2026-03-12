using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class QuestBehaviour : LBSBehaviour, IBlueprintable
    {
        public Type activeGraphNodeType = null;
        public string ActionToSet { get; set; }

        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();
        
        public QuestBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

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


        public bool CaptureAreaData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            List<GraphNode> nodesToRemove = Graph.GraphNodes;
            List<QuestEdge> edgesToRemove = Graph.GraphEdges;

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
            foreach (var node in Graph.GraphNodes)
            {
                var distanceToAnchor = node.Position - parentAnchor;
                node.Position = delta;
                node.Position += distanceToAnchor;
            }
        }

        public Vector2Int GetAnchor()
        {
            return Vector2Int.zero;
        }
    }
}