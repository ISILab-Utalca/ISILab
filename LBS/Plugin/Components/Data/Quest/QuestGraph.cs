using ISILab.AI.Grammar;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEditor.Search;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class QuestGraph : LBSModule, ICloneable, ISelectable
    {
        #region CONSTANTS
        private const string defaultGrammarGuid = "14cb4d99b22a94a45bac4216aca3f57e"; // Default grammar guid
        private const float ViewNodeWidthOffset = 100f;
        private const float SuggestionDistance = 1.5f;
        #endregion

        #region FIELDS
        [SerializeField, SerializeReference]
        private List<GraphNode> graphNodes = new();
       
        [SerializeField, SerializeReference]
        private List<QuestEdge> graphEdges = new();

        [SerializeField, SerializeReference]
        private QuestNode root;
       
        [SerializeField]
        private string grammarGuid = defaultGrammarGuid; 

        private LBSGrammar grammar;



        #endregion

        #region PROPERTIES
        public QuestNode Root => root;
        public List<GraphNode> GraphNodes => graphNodes;

        public List<QuestEdge> GraphEdges => graphEdges;

        public LBSGrammar Grammar
        {
            get
            {
                if (grammar != null) return grammar;

                Grammar = AssetMacro.LoadAssetByGuid<LBSGrammar>(grammarGuid)
                      ?? AssetMacro.LoadAssetByGuid<LBSGrammar>(defaultGrammarGuid);

                return grammar;
            }
            set
            {
                grammar = value;
                grammarGuid = AssetMacro.GetGuidFromAsset(Grammar);
                ValidateGraph();
            }
        }

        public GraphNode SelectedGraphNode
        {
            get => selectedNode;
            set
            {
                if (value is not null && selectedNode is not null)
                {
                    if (value.Equals(selectedNode)) return;
                }


                // assign if its null or it is a graphnode contained in the existing nodes
                if (value == null || (value is not null && GraphNodes.Contains(value)))
                {

                    // deselect the previous node
                    selectedNode?.OnDeselect?.Invoke();

                    selectedNode = value;
                    Reselect();

                }
            }
        }

        public QuestNodeData SelectedQuestData => SelectedQuestNode?.Data;

        public QuestNode SelectedQuestNode => SelectedGraphNode as QuestNode;

        #endregion

        #region ACTIONS

        public Action OnUpdateGraph;
        private GraphNode selectedNode;
        public Action<GraphNode> OnNodeSelected;

        #endregion

        #region EVENTS
        public Action<Vector2Int> GoToNodeInGraph;
        public Action<QuestEdge> OnAddEdge;
        public Action<QuestEdge> OnRemoveEdge;
        public Action<QuestNode> OnAddSuggestion;
        public Action<QuestNode> OnRemoveSuggestion;
        public Action<QuestNode> OnAddNode;
        public Action<QuestNode> OnRemoveNode;

        #endregion

        #region CONSTRUCTOR
        public QuestGraph()
        {
            // changing one edge can change the values of all the graph so we recheck all the graph for
            ActionExtensions.AddUnique(ref OnAddEdge, PostEdge);
            ActionExtensions.AddUnique(ref OnRemoveEdge, PostEdge);
            ActionExtensions.AddUnique(ref OnAddNode, AddNode);
            ActionExtensions.AddUnique(ref OnRemoveNode, RemoveNode);
        }

        private void PostEdge(QuestEdge edge) => ValidateGraph();

        #endregion

        #region METHODS

        #region Action methods
        public void Reselect()
        {

            // Print the address of the Assistant and the Graph it is using
            int graphAddr = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

            Debug.Log($"[Graph {graphAddr}] On Node selected");

            // delegeates related to the graph node selection
            OnNodeSelected?.Invoke(selectedNode);

            // delgates related to the node only
            selectedNode?.OnSelect?.Invoke();
        }

        private void AddNode(QuestNode node)
        {
            SelectedGraphNode = node;
            ValidateGraph();
        }

        private void RemoveNode(QuestNode node)
        {
            if (SelectedGraphNode == node) SelectedGraphNode = null;
        }

        #endregion

        #region Grammar

        private void ValidateGrammar()
        {
            if (GraphEdges.Count == 0) return;

            GrammarAssistant assistant = OwnerLayer.GetAssistant<GrammarAssistant>();
            if (assistant == null) throw new Exception("No GrammarAssistant found");

            foreach (QuestEdge edge in GraphEdges)
                assistant.ValidateEdgeGrammar(edge);

        }
        
        private void ValidateConnections()
        {
            //  Update quest node types (Goal or Middle) by their connections
            foreach (QuestEdge innerEdge in GraphEdges)
            {
                if (innerEdge.To is QuestNode qn)
                {
                    if (qn == root) continue;
                    qn.NodeType = GetBranches(qn).Any()
                        ? QuestNode.ENodeType.Middle
                        : QuestNode.ENodeType.Goal;
                }
            }
            // we must revalidate all edges connections
            foreach (QuestEdge edge in GraphEdges)
            {
                // destination node validation
                GraphNode dest = edge.To;
                int destRoots = GetRoots(dest).Count;
                int destBranches = GetBranches(dest).Count;
                dest.ValidConnections = destRoots > 0 && destBranches > 0;

                // source nodes validation
                foreach (GraphNode node in edge.From)
                {
                    int roots = GetRoots(node).Count;
                    int branches = GetBranches(node).Count;
                    node.ValidConnections = roots > 0  && branches > 0;
                }

                if (dest is QuestNode { NodeType: QuestNode.ENodeType.Goal } goalNode)
                {
                    bool hasBranches = GetBranches(goalNode).Any();
                    bool hasRoots = GetRoots(goalNode).Any();
                    // the goal must not have branches!
                    goalNode.ValidConnections = !hasBranches && hasRoots;
                }
            }


        }

        public void ValidateGraph()
        {
            // reset all connections validations
            foreach (GraphNode node in GraphNodes)
            {
                node.ValidConnections = false;
                node.ValidGrammar = false;
            }

            ValidateConnections();
            ValidateGrammar();
            RootValidation();

            OnUpdateGraph?.Invoke();
        }

        #endregion

        #region Nodes

        public T GetNodeAtPosition<T>(Vector2 pos) where T : GraphNode
        {
            foreach (GraphNode node in graphNodes)
            {
                if (node.NodePosition.Contains(pos) && node is T casted)
                    return casted;
            }
            return null;
        }

        public List<QuestNode> GetQuestNodes() =>
            graphNodes.OfType<QuestNode>().ToList();

        public GraphNode AddNewNode(QuestBehaviour behaviour, Vector2 pos)
        {
            if (behaviour.activeGraphNodeType is null) return null;

            // adding a quest action node
            if (behaviour.activeGraphNodeType == typeof(QuestNode))
                return AddNewQuestNode(behaviour.ActionToSet, pos);

            // adding a branching node
            GraphNode node = behaviour.activeGraphNodeType == typeof(OrNode)
                     ? new OrNode(string.Empty, pos, this)
                     : new AndNode(string.Empty, pos, this);

            node.ID = GenerateUniqueId(node.ToString(), GraphNodes.Select(n => n.ID));
            AddNodeToGraph(node);
            return node;
        }

        public QuestNode CreateSuggestionNode(string action,  List<QuestNode> tempSuggestions, Vector2 pos = default)
        {
            string uniqueSuggestionId = "s" + GenerateUniqueId(action, tempSuggestions.Select(n => n.ID));
            QuestNode node = new QuestNode(uniqueSuggestionId, pos, action, this);
            return node;
        }
        
        public QuestNode AddNewQuestNode(string action, Vector2 pos)
        {
            string uniqueId = GenerateUniqueId(action, GetQuestNodes().Select(n => n.ID));
            QuestNode node = new QuestNode(uniqueId, pos, action, this);
            AddNodeToGraph(node);
            return node;
        }
        
        public void AddSuggestionNode(QuestNode generatedQuestNode)
        {
            if(generatedQuestNode is null) return;
            Vector2Int pos = generatedQuestNode.NodePosition.position.ToInt();
            Vector2 graphPos = OwnerLayer.FixedToPosition(pos, true);
            QuestNode node = AddNewQuestNode(generatedQuestNode.TerminalID, graphPos);
             node.Data = generatedQuestNode.Data;
             node.NodePosition = new Rect(
                 graphPos, 
                 generatedQuestNode.NodePosition.size * SuggestionDistance);
        }

        private string GenerateUniqueId(string baseName, IEnumerable<string> existingIds)
        {
            var enumerable = existingIds.ToList();
            if (!enumerable.Contains(baseName))
                return baseName;

            int suffix = 1;
            string uniqueId;
            do { uniqueId = $"{baseName} ({suffix++})"; }
            while (enumerable.Contains(uniqueId));
            return uniqueId;
        }


        public void AddNodeToGraph(GraphNode node)
        {
            graphNodes.Add(node);

            if (node is QuestNode qn)
            {
                if (root == null) 
                    SetRoot(qn);
            }

            OnAddNode?.Invoke(node as QuestNode);
        }

        public void RemoveQuestNode(GraphNode node)
        {
            graphNodes.Remove(node);
            
            foreach (QuestEdge e in GetEdgesWithNode(node))
            {
                RemoveEdge(e); 
            }

            if (Equals(node, root)) root = null;
            OnRemoveNode?.Invoke(node as QuestNode);
        }
        #endregion

        #region Edges
        public Tuple<string, LogType> AddEdge(GraphNode from, GraphNode to)
        {
            QuestEdge newEdge = new QuestEdge(from, to);
            graphEdges.Add(newEdge);
            OnAddEdge?.Invoke(newEdge);

            return Tuple.Create($"Connection: {from} → {to}", LogType.Log);
        }

        public bool IsLooped(GraphNode origin, GraphNode current, HashSet<GraphNode> visited)
        {
            if (Equals(origin, current))
                return true;

            if (!visited.Add(current))
                return false;

            // Traverse *forward only* (branches)
            foreach (QuestEdge branch in GetBranches(current))
            {
                if (IsLooped(origin, branch.To, visited))
                    return true;
            }
            
            /* Code for single direction 
            if (origin == current)
                return true;

            if (!visited.Add(current)) // returns false if already in visited
                return false;

            // Check roots
            foreach (var rootEdge in GetRoots(current))
            {
                foreach (var fromNode in rootEdge.From)
                {
                    if (IsLooped(origin, fromNode, visited))
                        return true;
                }
            }

            // Check branches
            foreach (var branch in GetBranches(current))
            {
                if (IsLooped(origin, branch.To, visited))
                    return true;
            }
*/
            return false;
        }

        public bool RemoveEdge(QuestEdge edge)
        {
            if (edge == null) return false;
            OnRemoveEdge?.Invoke(edge);
            graphEdges.Remove(edge);
            return true;
        }

        public QuestEdge GetEdge(Vector2 pos, float delta)
        {
            foreach (QuestEdge e in graphEdges)
            {
                foreach (GraphNode from in e.From)
                {
                    Vector2 c1 = new Rect(from.NodePosition).center;
                    Vector2 c2 = new Rect(e.To.NodePosition).center;
                    if (pos.DistanceToLine(c1, c2) < delta)
                        return e;
                }
            }
            return null;
        }

        private List<QuestEdge> GetEdgesWithNode(GraphNode node) =>
            graphEdges.Where(e => e.From.Contains(node) || e.To.Equals(node)).ToList();

        public List<QuestEdge> GetBranches(GraphNode node)
        {
            List<QuestEdge> list = new List<QuestEdge>();
            
            if (!graphNodes.Contains(node)) return list;
            
            foreach (QuestEdge edge in graphEdges)
            {
                // Check that the edge target is valid
                if (!graphNodes.Contains(edge.To)) continue;
                
                // Check if at least one From exists in the graph
                bool found = false;
                foreach (GraphNode from in edge.From)
                {
                    if (Equals(from, node) && graphNodes.Contains(from))
                    {
                        found = true;
                        break;
                    }
                }

                if (found) list.Add(edge);
            }

            return list;
        }


        public List<QuestEdge> GetRoots(GraphNode node)
        {
            List<QuestEdge> valid = new List<QuestEdge>();

            foreach (QuestEdge edge in graphEdges)
            {
                if (!Equals(edge.To, node)) continue;
                
                // Check if To exists in the graph
                if (!graphNodes.Contains(edge.To)) continue;
                
                // Check if at least one From exists in the graph
                bool validFrom = false;
                foreach (GraphNode from in edge.From)
                {
                    if (!graphNodes.Contains(from)) continue;
                    validFrom = true;
                    break; 
                }

                if (validFrom) valid.Add(edge);
            }

            return valid;
        }

           
        #endregion
        
        #region AssistantCalls
        
         /// <summary>
        /// finds the edge of a referenced node. makes a new action that turns into the "To"
        /// of the connection and makes a new edge from the new action and the original "To"
        /// of the referenced node
        /// </summary>
        /// <param name="action">The action type for the new node</param>
        /// <param name="referenceNode">The node after which the new node will be inserted</param>
        public QuestNode InsertQuestNodeAfter(string action, QuestNode referenceNode)
        {
            if (referenceNode == null || !graphNodes.Contains(referenceNode))
            {
                Debug.LogWarning("Reference node is null or not in the graph. Adding as regular node.");
                return AddNewQuestNode(action, Vector2.zero);
            }

            // Position new node next to reference
            Vector2 position = referenceNode.NodePosition.position;
            position.x += (int)ViewNodeWidthOffset;

            QuestNode newNode = AddNewQuestNode(action, position);

            // Move all outgoing edges of reference so they start at new node
            foreach (QuestEdge edge in GetBranches(referenceNode).ToList())
            {
                RemoveEdge(edge);
                AddEdge(newNode, edge.To);
            }
            
            // Add edge from reference → new node
            AddEdge(referenceNode, newNode);

            SelectedGraphNode = newNode;
            return newNode;
        }



        /// <summary>
        /// Inserts a new node before a specified reference node
        /// </summary>
        /// <param name="action">The action type for the new node</param>
        /// <param name="referenceNode">The node before which the new node will be inserted</param>
        public QuestNode InsertQuestNodeBefore(string action, QuestNode referenceNode)
        {
            if (referenceNode == null || !graphNodes.Contains(referenceNode))
            {
                Debug.LogWarning("Reference node is null or not in the graph. Adding as regular node.");
                return AddNewQuestNode(action, Vector2.zero);
            }

            // Position new node next to reference
            Vector2 position = referenceNode.NodePosition.position;
            position.x -= (int)ViewNodeWidthOffset;

            QuestNode newNode = AddNewQuestNode(action, position);

            // Move all incoming edges of reference so they start at new node
            foreach (QuestEdge edge in GetRoots(referenceNode).ToList())
            {
                RemoveEdge(edge);
                foreach (GraphNode from in edge.From)
                {
                    AddEdge(from, newNode);
                }          
     
            }
            
            // Add edge from new node →reference
            AddEdge(newNode, referenceNode);
            OnUpdateGraph?.Invoke();

            SelectedGraphNode = newNode;

            return newNode;
        }
        
        /// <summary>
        /// Inserts all the nodes to replace the reference node
        /// </summary>
        /// <param name="expandActions">all the actions that correspond to a new node</param>
        /// <param name="referenceNode">the node that will be expanded(replaced)</param>
        public QuestNode ExpandNode(List<string> expandActions, QuestNode referenceNode)
        {
            if(!expandActions.Any()) return null;

            QuestNode iterationNode = referenceNode;
            
            // cant' redo connections with a root already in use
            if(Equals(referenceNode, Root)) SetRoot(null);
            
            List<QuestNode> newNodes = new List<QuestNode>();

            // add from the previous index position to add the new ones
            for (int i = 0; i < expandActions.Count; i++)
            {
                QuestNode newNode = InsertQuestNodeAfter(expandActions[i], iterationNode);
                newNodes.Add(newNode);
                iterationNode = newNode;
            }

            // the nodes whose destination is the reference node
            if (newNodes.Any())
            {
                List<QuestEdge> roots = referenceNode.Graph.GetRoots(referenceNode);
                foreach (QuestEdge edge in roots)
                {
                    foreach (GraphNode from in edge.From)
                    {
                        // connect the last inserted node to the original reference node's destinations
                        AddEdge(from, newNodes.First());
                    }
                }
            }

            RemoveQuestNode(referenceNode);
            SelectedGraphNode = iterationNode;
            return iterationNode;
        }
        
        #endregion
        
        #region Root
        public void SetRoot(QuestNode node)
        {
            if (node == root) return;

            if (root != null)
            {
                root.NodeType = QuestNode.ENodeType.Middle;
            }
            
            root = node;
            if(root != null) root.NodeType = QuestNode.ENodeType.Start;

            ValidateGraph();
        }

        private void RootValidation()
        {
           if(root is not null) 
            {
                var roots = !GetRoots(root).Any();
                var branches = GetBranches(root).Any();
                root.ValidConnections = roots && branches; 
                root.NodeType = QuestNode.ENodeType.Start;
            }
        }

        #endregion

        #region Clone & Utils

        public override bool IsEmpty() => graphNodes.Count == 0;

        public override object Clone()
        {
            QuestGraph clone = new QuestGraph { grammarGuid = grammarGuid };
 
            // cloning nodes and their data
            var nodes = graphNodes.Select(CloneRefs.Get).Cast<GraphNode>();
            foreach (GraphNode n in nodes)
            {
                if(n is QuestNode qn)
                {
                    if (Root is not null)
                    {
                        if (Root.ID == qn.ID)
                        {
                            clone.root = qn;
                        }
                    }
                   
                }
                clone.graphNodes.Add(n);
                n.Graph = clone;
            }

            // cloning edges
            var edges = graphEdges.Select(CloneRefs.Get).Cast<QuestEdge>();
            foreach (QuestEdge e in edges) clone.graphEdges.Add(e);

            return clone;
        }

        public List<object> GetSelected(Vector2Int pos)
        {
            var list = new List<object>();
            GraphNode node = GetGraphNode(pos);
            if (node != null) list.Add(node);
            return list;
        }

        private GraphNode GetGraphNode(Vector2Int pos) =>
            graphNodes.FirstOrDefault(n => n.Position == pos);

        #endregion

        #region Unused
        public void ChangeConnection(QuestEdge edge, Type graphNodeType) =>
            throw new NotImplementedException();

        public override void Print() => throw new NotImplementedException();
        public override void Clear() => throw new NotImplementedException();
        public override Rect GetBounds() => throw new NotImplementedException();
        public override void Rewrite(LBSModule other) => throw new NotImplementedException();

        #endregion

        #endregion

    }
}
