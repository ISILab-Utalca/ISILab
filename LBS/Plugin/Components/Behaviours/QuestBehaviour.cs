using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class QuestBehaviour : LBSBehaviour
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

        public override void ApplyBlueprintOffset(Vector2Int offset)
        {
            foreach (var node in Graph.GraphNodes)
            {
                node.Position += offset;
            }
        }

        public override BlueprintData[] GetBlueprintData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            HashSet<BlueprintData> validObjects = new();

            List<GraphNode> graphNodesClone = new();
            List<QuestEdge> graphEdgesClone = new();
            QuestNode rootClone = null;

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

                if (!inside)
                    continue;

                GraphNode nodeClone = node.Clone() as GraphNode;
                graphNodesClone.Add(nodeClone);

                validObjects.Add(
                    new BlueprintData(
                        graphNodesClone,
                        corners.min,
                        corners.max
                        )
                    );

                if (node is QuestNode questNode && Graph.Root == node)
                {
                    rootClone = nodeClone as QuestNode;

                    validObjects.Add(
                        new BlueprintData(
                            rootClone,
                            corners.min,
                            corners.max
                            )
                        );
                }
            }

            foreach (QuestEdge edge in Graph.GraphEdges)
            {
                bool fromInside = graphNodesClone.Exists(n => edge.From.Contains(n));
                bool toInside = graphNodesClone.Exists(n => n.ID == edge.To.ID);

                if (fromInside && toInside)
                {
                    QuestEdge edgeClone = edge.Clone() as QuestEdge;
                    graphEdgesClone.Add(edgeClone);

                    validObjects.Add(
                        new BlueprintData(
                            graphEdgesClone,
                            corners.min,
                            corners.max
                            )
                        );
                }
            }

            return validObjects.ToArray();
        }

        public override void LoadBlueprintData(BlueprintData[] objects)
        {
            throw new NotImplementedException();
        }
    }
}