using ISILab.AI.Grammar;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(Graph)), RequieredAssistant(typeof(GrammarAssistant))]
    public class QuestBehaviour : LBSBehaviour, IBlueprintable
    {
        #region CONSTANTS
        private const string defaultGrammarGuid = "14cb4d99b22a94a45bac4216aca3f57e"; // Default grammar guid
        public const float ViewNodeWidthOffset = 100f;
        public const float SuggestionDistance = 1.5f;
        #endregion

        #region FIELDS

        [SerializeField]
        private string grammarGuid = defaultGrammarGuid;

        private LBSGrammar grammar;

        private NodeKind nodeKind;
        private string activeTerminal = string.Empty;



        #endregion


        #region PROPERTIES

        /// <summary>
        /// Grammar whose's rules and terminals are used to assign and validate quest nodes. It must be stored using GUIDs to avoid corruption.
        /// </summary>
        [ShowOnLayerTemplate]
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

     

        public Graph Graph => OwnerLayer.GetModule<Graph>();
        public List<QuestNode> QuestNodes => Graph.GetNodes<QuestNode>();
        public List<Node> BaseNodes => Graph.GetNodes<Node>();
        public QuestNode SelectedQuestNode => Graph.Selected as QuestNode;
        public string ActiveTerminal
        {
            get => activeTerminal;
            set => activeTerminal = value;
        }

        public NodeKind ActiveNodeKind
        {
            get => nodeKind;
            set => nodeKind = value;
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
            var clone = new QuestBehaviour(IconGuid, Name, ColorTint);
            clone.grammarGuid = grammarGuid;
            return clone;
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;

            layer.OnChange += () =>
            {
                Graph.Selected = null;
                UpdateKeys();
            };

            Graph.OnNewRoot += (oldRoot, newRoot) =>
            {
                if (oldRoot is QuestNode old)
                    old.NodeType = GraphNodeType.Middle;

                if (newRoot is QuestNode root)
                    root.NodeType = GraphNodeType.Start;
            };

            Graph.OnAddNode += (node) => RequestTilePaint(node);

            Graph.PreAddEdge += (edge) =>
            {
                RequestTilePaint(edge);

                bool branches = Graph.GetBranches(edge.To).Count > 0;
                bool roots = Graph.GetRoots(edge.To).Count > 0;

                if(edge.To is QuestNode n)
                {
                    if (branches && roots) 
                        n.NodeType = GraphNodeType.Middle;

                    if (!branches && roots)
                        n.NodeType = GraphNodeType.Goal;
                }
            };

            Graph.OnRemoveNode += (node) =>
            {
                if (Graph.Selected == node)
                    Graph.Selected = null;

                RequestTileRemove(node);
            };

            Graph.PreRemoveEdge += (edge) =>
            {
                RequestTileRemove(edge);

                bool branches = Graph.GetBranches(edge.To).Count > 0;
                bool roots = Graph.GetRoots(edge.To).Count > 0;

                if (edge.To is QuestNode n)
                {
                    n.NodeType = GraphNodeType.Middle;
                }
            };

            Graph.PostEdgesChange += ValidateGraph;
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

            foreach (var node in Graph.Nodes)
            {
                allKeys.Add(node);
            }

            foreach (var edge in Graph.Edges)
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
            List<object> nodesToRemove = new List<object>();
            List<Edge> edgesToRemove = new List<Edge>();

            foreach (var node in BaseNodes)
            {
                Vector2Int nodePos = OwnerLayer.ToFixedPosition(node.Area.center);
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

            foreach (Edge edge in Graph.Edges)
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

            return Graph.Nodes.Count > 0 || Graph.Edges.Count > 0;
        }

        public void SetPosition(Vector2Int parentAnchor, Vector2Int delta)
        {
            // Grid coordinates use inverted Y compared to GraphView
            Vector2Int parentAnchorView = new(parentAnchor.x, -parentAnchor.y);

            Vector2 parentAnchorViewPos = OwnerLayer.FixedToPosition(parentAnchorView);
            Vector2 deltaView = OwnerLayer.FixedToPosition(delta);
            deltaView.y *= -1;

            foreach (var node in BaseNodes)
            {
                Vector2Int distanceToAnchor = node.Position - parentAnchor;
                node.Position = delta + distanceToAnchor;

                Vector2 distanceToAnchorView = node.Area.position - parentAnchorViewPos;

                Vector2 newViewPos = deltaView + distanceToAnchorView;

                node.Area = new Rect(
                    newViewPos,
                    node.Area.size
                );
            }
        }

        public Vector2Int GetAnchor()
        {
            Vector2Int anchor = new Vector2Int(int.MaxValue, int.MinValue);
            if (OwnerLayer is null) return anchor;
   
            foreach (var node in BaseNodes)
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

            for (int i = 0; i < merger.Graph.Nodes.Count; i++)
            {
                var incomingNode = merger.BaseNodes[i];

                Node existingNode = null;

                for (int j = 0; j < Graph.Nodes.Count; j++)
                {
                    var node = Graph.Nodes[j] as Node;

                    if (node.ID == incomingNode.ID)
                    {
                        existingNode = node;
                        break;
                    }
                }

                if (existingNode == null)
                {
                    Graph.AddNode(incomingNode.Clone());
                }
                else if (overwrite)
                {
                    existingNode = incomingNode.Clone() as Node;
                }
            }

            for (int i = 0; i < merger.Graph.Edges.Count; i++)
            {
                var incomingEdge = merger.Graph.Edges[i];


                for (int j = 0; j < Graph.Edges.Count; j++)
                {
                    var edge = Graph.Edges[j];

                    if(edge.To is not Node || edge.From is not Node)
                        continue;
                    bool exists = 
                        (edge.To as Node).ID == (incomingEdge.To as Node).ID && 
                        (edge.From as Node).ID == (incomingEdge.From as Node).ID;

                    if (!exists)
                    {
                        var newEdge = incomingEdge.Clone() as Edge;
                        Graph.AddEdge(newEdge.From, newEdge.To);
                    }
                    else if (overwrite)
                    {
                        edge = incomingEdge.Clone() as Edge;
                    }
                }
            }

            return true;
        }

        #endregion

        #endregion
        #region Validation

        private void ValidateGrammar()
        {
            GrammarAssistant assistant = OwnerLayer.GetAssistant<GrammarAssistant>() ??
                throw new Exception("No GrammarAssistant found");
            assistant.ValidateGraphGrammar();
        }

        private void ValidateConnections()
        {
            foreach (var node in BaseNodes)
                node.ValidConnections = false;

            var expired = RetrieveExpiredReadOnly();

            // connection on removed eges
            foreach (var expire in expired)
            {
                
                if (expire is not Edge e)
                    continue;

                if (e.From is Node from)
                    from.ValidConnections = false;

                if (e.To is Node to)
                    to.ValidConnections = false;
                
            }

            // middle connections
            foreach (Edge e in Graph.Edges)
            {
                if (expired.Contains(e))
                    continue;

                // destination node validation
                var dest = e.To as Node;
                if(dest == null)
                     continue; 

                int destRoots = Graph.GetRoots(dest).Count;
                int destBranches = Graph.GetBranches(dest).Count;
                dest.ValidConnections = destRoots > 0 && destBranches > 0;

                // source nodes validation
                var from = e.From as Node;
                if (from == null)
                    continue;

                int roots = Graph.GetRoots(from).Count;
                int branches = Graph.GetBranches(from).Count;
                from.ValidConnections = roots > 0 && branches > 0;
            }

            // goals connections
            foreach(var n in QuestNodes)
            {
                
                if(n.NodeType == GraphNodeType.Goal)
                {
                    bool hasBranches = Graph.GetBranches(n).Any();
                    bool hasRoots = Graph.GetRoots(n).Any();
                    // the goal must not have branches!
                    n.ValidConnections = !hasBranches && hasRoots;
                }
            }

            // root connections
            var root = Graph.Root as QuestNode;
            if (root is not null)
            {
                var roots = Graph.GetRoots(root).Count == 0;
                var branches = Graph.GetBranches(root).Count > 0;
                root.ValidConnections = roots && branches;
            }
        }


        /// <summary>
        /// Checks and updates the validation of the whole graph against the grammar rules, the nodes' data and connections. 
        /// It should be called after any change in the graph to make sure the graph is updated and valid.
        /// </summary>
        public void ValidateGraph()
        {
            /*
            // reset all connections validations
            foreach (var n in Graph.Nodes)
            {
                if(n is Node node)
                {
                    node.ValidConnections = false;
                    if(node is QuestNode qn)
                    {
                        qn.ValidGrammar = false;
                    }
                }
            }
            */

            ValidateConnections();
            ValidateGrammar();
            
            Graph?.OnForceUpdate?.Invoke();
        }


        #endregion


        #region Helpers

        /// <summary>
        /// Returns a <see cref="Edge"/>. by passing a position and a delta distance(error margin) to check if its near the edge's middle point connection.
        /// </summary>
        /// <param name="pos">Position to check</param>
        /// <param name="delta">Delta distance (error margin)</param>
        /// <returns>The edge if found, otherwise null</returns>
        public Edge GetEdge(Vector2 pos, float delta = 20)
        {
            foreach (Edge e in Graph.Edges)
            {
                var from = (e.From as Node);
                var to = (e.To as Node);

                if (from == null || to == null)
                    continue;

                Vector2 c1 = new Rect(from.Area).center;
                Vector2 c2 = new Rect(to.Area).center;

                if (pos.DistanceToLine(c1, c2) < delta)
                    return e;

            }
            return null;
        }


        public object CreateNode(Vector2Int endPosition)
        {
            if (ActiveNodeKind == NodeKind.Terminal)
                return CreateQuestNode(activeTerminal, endPosition);
            return CreateBranchNode(endPosition);
        }


        /// <summary>
        /// Adds a quest node from a given action type and position. The action type is used to assign the terminalID of the node, which is used to validate the node's data and grammar rules.
        /// </summary>
        /// <param name="action">terminal(for example, "kill")</param>
        /// <param name="pos">position the node gets added at</param>
        /// <returns>the added <see cref="QuestNode"/></returns>
        public QuestNode CreateQuestNode(string action, Vector2 pos)
        {
            string uniqueId = GenerateUniqueId(action, QuestNodes.Select(n => n.ID));
            return new QuestNode(uniqueId, pos, Grammar.GetTerminal(action) ,Graph);
        }



        /// <summary>
        /// Adds an <see cref="OrNode"/> or an <see cref="AndNode"/> in the graph.
        /// </summary>
        /// <param name="behaviour">the behaviour that determines the type of node to add</param>
        /// <param name="pos">position the node gets added at</param>
        /// <returns></returns>
        private BranchNode CreateBranchNode(Vector2 pos)
        {
            if (ActiveNodeKind == NodeKind.Terminal) 
                return null;

            List<string> BranchIDS = new();
            foreach(var node in Graph.Nodes)
            {
                if(node is BranchNode bn)
                {
                    if(bn.Kind == nodeKind)
                    {
                        BranchIDS.Add(bn.ID);
                    }
                }
            }

            var newID = GenerateUniqueId(ActiveNodeKind.ToString(), BranchIDS.AsEnumerable());
            return new BranchNode(newID, nodeKind, pos, Graph);
        }


        /// <summary>
        /// Generates an unique ID
        /// </summary>
        /// <param name="baseName">the base name: the action name</param>
        /// <param name="existingIds">the list of existing IDs</param>
        /// <returns>the unique ID</returns>
        public static string GenerateUniqueId(string baseName, IEnumerable<string> existingIds)
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

        private object GetNode(Vector2Int pos)
        {
            foreach (var node in Graph.Nodes)
            {
                if (node is Node qn && qn.Position == pos)
                    return node;
            }
            return null;
        }


        public void AddSuggestionNode(QuestNode generatedQuestNode)
        {
            if (generatedQuestNode is null) return;
            Vector2Int pos = generatedQuestNode.Area.position.ToInt();
            Vector2 graphPos = OwnerLayer.FixedToPosition(pos, true);
            QuestNode node = CreateQuestNode(generatedQuestNode.TerminalID, graphPos);
            node.Data = generatedQuestNode.Data;
            node.Area = new Rect(
                graphPos,
                generatedQuestNode.Area.size * SuggestionDistance);
        }

        #endregion

        #region BOOLEAN METHODS


        /// <summary>
        /// Checks that all the graph nodes have valid connections.
        /// </summary>
        /// <returns>true or false</returns>
        internal bool HasValidConnections()
        {
            foreach (var node in Graph.Nodes)
            {
                if(node is Node n)
                    if (!n.ValidConnections) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks that all the graph nodes meet grammar structure requirements/rules
        /// </summary>
        /// <returns>true or false</returns>
        internal bool HasValidGrammar()
        {
            foreach (var node in QuestNodes)
            {
                if (!node.ValidGrammar) return false;
            }
            return true;

        }

        /// <summary>
        /// Checks that all nodes(<see cref="QuestNode"/>)'s fields <see cref="QuestNodeData.Fields"/> are valid, according to their terminal definitions.
        /// For example, a <see cref="GrammarBundleGraph"/> must have a tile(<see cref="TileBundleGroup"/>) reference assigned from another layer.
        /// </summary>
        /// <returns>true or false</returns>
        internal bool HasValidData()
        {
            foreach (var node in QuestNodes)
            {
                if (!node.Data.IsValid()) return false;
            }
            return true;
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
            if (referenceNode == null || !Graph.Nodes.Contains(referenceNode))
            {
                Debug.LogWarning("Reference node is null or not in the graph. Adding as regular node.");
                return CreateQuestNode(action, Vector2.zero);
            }

            // Position new node next to reference
            Vector2 position = referenceNode.Area.position;
            position.x += (int)ViewNodeWidthOffset;

            QuestNode newNode = CreateQuestNode(action, position);
            Graph.AddNode(newNode);

            // Move all outgoing edges of reference so they start at new node
            foreach (Edge edge in Graph.GetBranches(referenceNode).ToList())
            {
                Graph.RemoveEdge(edge);
                Graph.AddEdge(newNode, edge.To);
            }

            // Add edge from reference → new node
            Graph.AddEdge(referenceNode, newNode);

            Graph.Selected = newNode;
            return newNode;
        }



        /// <summary>
        /// Inserts a new node before a specified reference node
        /// </summary>
        /// <param name="action">The action type for the new node</param>
        /// <param name="referenceNode">The node before which the new node will be inserted</param>
        public QuestNode InsertQuestNodeBefore(string action, QuestNode referenceNode)
        {
            if (referenceNode == null || !Graph.Nodes.Contains(referenceNode))
            {
                Debug.LogWarning("Reference node is null or not in the graph. Adding as regular node.");
                return CreateQuestNode(action, Vector2.zero);
            }

            // Position new node next to reference
            Vector2 position = referenceNode.Area.position;
            position.x -= (int)ViewNodeWidthOffset;

            QuestNode newNode = CreateQuestNode(action, position);
            Graph.AddNode(newNode);

            // Move all incoming edges of reference so they start at new node
            foreach (Edge edge in Graph.GetRoots(referenceNode).ToList())
            {
                Graph.RemoveEdge(edge);
                Graph.AddEdge(edge.From, newNode);
            }

            // Add edge from new node →reference
            Graph.AddEdge(newNode, referenceNode);
            Graph.OnForceUpdate?.Invoke();

            Graph.Selected = newNode;

            return newNode;
        }

        /// <summary>
        /// Inserts all the nodes to replace the reference node
        /// </summary>
        /// <param name="expandActions">all the actions that correspond to a new node</param>
        /// <param name="referenceNode">the node that will be expanded(replaced)</param>
        public QuestNode ExpandNode(List<string> expandActions, QuestNode referenceNode)
        {
            if (!expandActions.Any()) return null;

            QuestNode iterationNode = referenceNode;

            // cant' redo connections with a root already in use
            if (Equals(referenceNode, Graph.Root)) Graph.SetRoot(null);

            List<QuestNode> newNodes = new List<QuestNode>();

            // add from the previous index position to add the new ones
            for (int i = 0; i < expandActions.Count; i++)
            {
                QuestNode newNode = InsertQuestNodeAfter(expandActions[i], iterationNode);
                Graph.AddNode(newNode);
                newNodes.Add(newNode);
                iterationNode = newNode;
            }

            // the nodes whose destination is the reference node
            if (newNodes.Any())
            {
                List<Edge> roots = referenceNode.Graph.GetRoots(referenceNode);
                foreach (Edge edge in roots)
                {
                    Graph.AddEdge(edge.From, newNodes.First());
                }
            }

            Graph.RemoveNode(referenceNode);
            Graph.Selected = iterationNode;
            return iterationNode;
        }

        #endregion

    }
}