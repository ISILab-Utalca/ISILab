using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class NodeDataBehaviour : LBSBehaviour
    {
        public QuestNodeData SelectedNodeData => Graph.SelectedQuestNode?.Data;
        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();

        public Action<GraphNode> OnNodeDataChanged;
        public Action<QuestNodeData> OnNodeDataChangedBegin;
        public Action<QuestNodeData> OnNodeDataChangedEnd;
        /// <summary>
        /// Assigned from the QuestNodeView On MouseDown event. It will assign the current selected node, allowing to
        /// modify it based on its action type.
        /// </summary>


        public NodeDataBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override void OnGUI()
        {
  
        }
        
        public override object Clone()
        {
            return new NodeDataBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;

            ActionExtensions.AddUnique(ref OnNodeDataChanged, OnDataChanged);


            layer.OnChange += () =>
            {
                UpdateKeys();
            };

            Graph.OnRemoveNode += (node) =>
            {
                RequestTileRemove(node.Data);
            };

            Graph.OnAddNode += (node) => 
            {
                RequestTilePaint(node.Data);
            };
        }

        private void OnDataChanged(GraphNode node)
        {
            if (Equals(Graph.SelectedGraphNode, node)) return;
            Graph.OnNodeSelected?.Invoke(node);
        }

        public override void OnDetachLayer(LBSLayer layer) 
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;
        }
        
        public override void CheckKeys() 
        {
            UpdateKeys();
            RequestTilePaint(SelectedNodeData);
        } 

        public void UpdateKeys()
        {
            if (Graph == null) return;

            List<object> allKeys = new List<object>();

            // Add Node as keys
            foreach (var node in Graph.GetQuestNodes())
            {
                allKeys.Add(node.Data);
            }

            UpdateKeys(allKeys);
        }

    }
}