using ISILab.AI.Grammar;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(Graph))]
    public class NodeDataBehaviour : LBSBehaviour
    {
        public QuestNodeData SelectedNodeData
        {
            get
            {
                if(Graph.Selected is QuestNode qn)
                {
                    return qn.Data;
                }
                return null;
            }
        }

        public Graph Graph => OwnerLayer.GetModule<Graph>();

        // Events
        public Action<QuestNode> OnNodeDataChanged;
        public Action<QuestNodeData> OnNodeDataChangedBegin;
        public Action<QuestNodeData> OnNodeDataChangedEnd;
        public Action<GrammarField> OnAddField;
        public Action<GrammarField> OnRemoveField;

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
                if (node is QuestNode qt)
                {
                    foreach (var field in qt.Data.GetFields<GrammarField>())
                    {
                        RequestTileRemove(field);
                    }
                }
               
            };

            Graph.OnAddNode += (node) => 
            {
                if (node is QuestNode qt)
                {
                    foreach (var field in qt.Data.GetFields<GrammarField>())
                    {
                        RequestTilePaint(field);
                    }
                }
            };

            OnAddField += (field) =>
            {
                RequestTilePaint(field);
            };

            OnRemoveField += (field) =>
            {
                RequestTileRemove(field);
            };
        }

        private void OnDataChanged(Node node)
        {
            if (Equals(Graph.Selected, node)) return;
            Graph.OnSelect?.Invoke(node);
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
            foreach (var node in Graph.GetNodes<QuestNode>())
            {
                allKeys.AddRange(node.Data.GetFields<GrammarField>());
            }

            UpdateKeys(allKeys);
        }

    }
}