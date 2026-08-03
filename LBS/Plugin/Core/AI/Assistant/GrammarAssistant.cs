using ISILab.AI.Grammar;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Assistants;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [Serializable]
    [RequieredModule(typeof(Graph))]
    public class GrammarAssistant : LBSAssistant
    {
        #region FIELDS
        private QuestBehaviour questBehaviour;
        private Graph graph;
        private bool disabled = false;
        #endregion

        #region PROPERTIES
        public bool Disabled => disabled;
        [JsonIgnore]
        public Graph Graph => graph ??= OwnerLayer.GetModule<Graph>();
        public QuestBehaviour Behavior => questBehaviour ??= OwnerLayer.GetBehaviour<QuestBehaviour>();
        #endregion

        /*public GrammarAssistant() 
        { 
            //return GrammarAssistant("hi", "ho", Color.cyan); 
        }//*/

        public GrammarAssistant(string IconGuid, string name, Color colorTint)
            : base(IconGuid, name, colorTint) { }

        public override object Clone()
        {
            return new GrammarAssistant(IconGuid, this.Name, this.ColorTint);
        }

        #region Validation
        public bool ValidateGraphGrammar()
        {
            // 1. Reset all validation states to a clean slate before processing
            foreach (var n in Behavior.QuestNodes)
            {
                n.ValidGrammar = false;
            }

            // 2. Validate all QuestNodes (Terminals) first based on forward paths
            foreach (var n in Behavior.QuestNodes)
            {
                ValidateQuestNodeGrammar(n);
            }

            // 3. Propagate validation states to Branch nodes (And/Or) based on their updated roots
            // We loop a few times to handle nested branches (e.g., branch pointing to another branch)
            bool stateChanged;
            do
            {
                stateChanged = false;
                foreach (var n in Behavior.QuestNodes)
                {
                    if (n is not QuestNode)
                    {
                        bool wasValid = n.ValidGrammar;
                        n.ValidGrammar = BranchNodeRootGrammar(n);

                        if (wasValid != n.ValidGrammar)
                        {
                            stateChanged = true;
                        }
                    }
                }
            } while (stateChanged);

            // 4. Finally, Goal nodes inherit the validation of whatever is feeding into them
            foreach (var n in Behavior.QuestNodes)
            {
                if (n is QuestNode { NodeType: GraphNodeType.Goal } goalNode)
                {
                    // Goal is valid if all its incoming roots/branches are structurally valid
                    goalNode.ValidGrammar = GoalNodeRootsAreValid(goalNode);
                }
            }

            // Return true only if every single node in the graph passed its ruleset check
            return Behavior.QuestNodes.All(node => node.ValidGrammar);
        }

        private void ValidateQuestNodeGrammar(QuestNode from)
        {
            var grammar = Behavior.Grammar;
            if (grammar == null || !grammar.LBSRules.Any()) return;

            // Start or Middle terminal validation
            if (from.NodeType == GraphNodeType.Start || from.NodeType == GraphNodeType.Middle)
            {
                // Find ALL terminal nodes downstream, traversing cleanly across any And/Or branches
                List<QuestNode> nextQuestNodes = GetNextQuestNodes(from);
                List<string> validNextTerminals = grammar.GetNextTerminals(from.TerminalID);

                // If a node has outgoing edges but leads to nothing, it is dead/invalid layout
                bool pathValid = nextQuestNodes.Count > 0;

                foreach (var nextNode in nextQuestNodes)
                {
                    if (!validNextTerminals.Contains(nextNode.TerminalID))
                    {
                        pathValid = false;
                        break;
                    }
                }

                from.ValidGrammar = pathValid;
            }
        }

        /// <summary>
        /// Recursively looks ahead down the graph edges to skip logical branches 
        /// and return the raw terminal QuestNodes that downstream branches eventually feed into.
        /// </summary>
        private List<QuestNode> GetNextQuestNodes(Node currentNode)
        {
            List<QuestNode> foundNodes = new List<QuestNode>();

            if (currentNode == null) 
                return foundNodes;

            // Find all outgoing edges where this structural node is the source
            var outgoingEdges = Graph.Edges.Where(e => e.From == currentNode);

            foreach (var edge in outgoingEdges)
            {
                if (edge.To is QuestNode questNode)
                {
                    foundNodes.Add(questNode);
                }
                else
                {
                    // It's a branch node, tunnel deeper down its forward paths
                    foundNodes.AddRange(GetNextQuestNodes(edge.To as Node));
                }
            }

            return foundNodes;
        }

        // Tries to retrieve from a branch Node the grammar of the immediate quest node root
        private bool BranchNodeRootGrammar(Node branch)
        {
            var roots = Graph.GetRoots(branch);
            if (!roots.Any()) return false;

            foreach (var rootEdge in roots)
            {
                // If the root feeding this branch is a terminal action, it must have valid grammar
                if (rootEdge.From is QuestNode questRoot)
                {
                    if (!questRoot.ValidGrammar) return false;
                }
                else // If the root is another branch, check if that branch has completed its validation pass
                {
                    if(rootEdge.From is QuestNode qn)
                    {
                        if (!qn.ValidGrammar) return false;

                    }
                }
            }

            return true;
        }

        private bool GoalNodeRootsAreValid(QuestNode goalNode)
        {
            var roots = Graph.GetRoots(goalNode);
            if (roots == null || roots.Count == 0) return false;

            foreach (var edge in roots)
            {
                if (edge?.From is QuestNode questNode && !questNode.ValidGrammar)
                    return false;
            }

            return true;
        }
        #endregion

        #region Getters

        public List<string> GetAllValidNextActionsInsert(
            string currentElement,
            Action<float> onProgress = null,
            CancellationToken token = default)
        {

            if (Graph == null || Behavior.Grammar == null) return new List<string>();

            // get valid actions out of context
            onProgress?.Invoke((float)1);
            return Behavior.Grammar.GetNextTerminals(currentElement);

        }

        public List<string> GetAllValidPrevActionsInsert(
            string currentElement, 
            Action<float> onProgress = null, 
            CancellationToken token = default)
        {

            if (Graph == null || Behavior.Grammar == null) return new List<string>();

            // Get all non context prev actions   
            onProgress?.Invoke((float)1);
            return Behavior.Grammar.GetPreviousTerminals(currentElement);
        }

        public List<List<string>> GetAllExpansions(
        string currentAction,
        Action<float> onProgress = null,
        CancellationToken token = default)
        {
            if (Graph == null || Behavior.Grammar == null) return new List<List<string>>();

            // STEP 1: Get raw expansions from grammar (already flattened to terminals!)
            var rawExpansions = Behavior.Grammar.GetExpansions(currentAction);
            if (rawExpansions == null || rawExpansions.Count == 0)
                return new List<List<string>>();

            var uniqueSequences = new HashSet<string>(); // For structural uniqueness
            var final = new List<List<string>>();

            for (int index = 0; index < rawExpansions.Count; index++)
            {
                if (token.IsCancellationRequested)
                    return final;

                var expansion = rawExpansions[index];
                var sequence = new List<string>();
                string lastAdded = null;

                // STEP 2: Simply iterate through the pre-flattened terminals
                foreach (var terminal in expansion)
                {
                    if (token.IsCancellationRequested)
                        return final;

                    // Clean consecutive duplicates right here
                    if (terminal != lastAdded)
                    {
                        sequence.Add(terminal);
                        lastAdded = terminal;
                    }
                }

                // Skip useless or truncated expansions
                if (sequence.Count < 3 ||
                    sequence[0] == currentAction || sequence[1] == currentAction ||
                    sequence[0] == sequence[2])
                    continue;

                // Ensure global structural uniqueness
                var key = string.Join("|", sequence);
                if (uniqueSequences.Add(key))
                {
                    final.Add(sequence);
                }

                onProgress?.Invoke((float)index / rawExpansions.Count);
            }

            return final;
        }

        #endregion

        #region Insert Actions

        public Action ExpandAction(List<string> expandAction, QuestNode referenceNode)
        {
            return () =>
            {
                var stopwatch = Stopwatch.StartNew();
                disabled = true;
                var node = Behavior.ExpandNode(expandAction, referenceNode);
                disabled = false;
                stopwatch.Stop();

                if(node != null) 
                {
                    Graph.Reselect();
                }
                Debug.Log($"ExpandAction took {stopwatch.ElapsedMilliseconds} ms");
            };
        }

        public Action InsertNextAction(string action, QuestNode referenceNode)
        {
            return () =>
            {
                Behavior.InsertQuestNodeAfter(action, referenceNode);
            };
        }

        public Action InsertPreviousAction(string action, QuestNode referenceNode)
        {
            return () =>
            {
                var stopwatch = Stopwatch.StartNew();

                Behavior.InsertQuestNodeBefore(action, referenceNode);
                Behavior.ValidateGraph();

                stopwatch.Stop();
                Debug.Log($"InsertPreviousAction took {stopwatch.ElapsedMilliseconds} ms");
            };
        }

        #endregion

        public override void OnAttachLayer(LBSLayer layer)
        {
            base.OnAttachLayer(layer);
        }

        public override void OnGUI() { }


        public object ExecuteTest(bool b)
        {
            throw new NotImplementedException(); 
            
        }
    }
}