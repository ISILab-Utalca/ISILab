using ISILab.AI.Grammar;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
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
    [RequieredModule(typeof(QuestGraph))]
    public class GrammarAssistant : LBSAssistant
    {
        #region FIELDS
        private QuestBehaviour questBehaviour;
        private QuestGraph graph;
        private bool disabled = false;
        #endregion

        #region PROPERTIES
        public Action<GraphNode> OnCallAssistant;
        public bool Disabled => disabled;
        [JsonIgnore]
        public QuestGraph Graph => graph ??= OwnerLayer.GetModule<QuestGraph>();
        public QuestBehaviour Behavior => questBehaviour ??= OwnerLayer.GetBehaviour<QuestBehaviour>();
        #endregion

        public GrammarAssistant(string IconGuid, string name, Color colorTint)
            : base(IconGuid, name, colorTint) { }

        public override object Clone()
        {
            return new GrammarAssistant(IconGuid, this.Name, this.ColorTint);
        }

        #region Validation
        public bool ValidateQuestGraph()
        {
            foreach (var node in Graph.GraphNodes)
            {
                if (!node.ValidGrammar) return false;
            }

            return true;
        }
        
        public bool ValidateEdgeGrammar(QuestEdge edge)
        {
            if (edge?.From is null || edge.To is null) return false;
            
            var grammar = Graph.Grammar;
            if (grammar == null || !grammar.LBSRules.Any()) return false;

            bool returnValid = false;

            foreach (var nodeFrom in edge.From)
            {
                if (nodeFrom is QuestNode from)
                {
                     // validate start 
                    if (from.NodeType == QuestNode.ENodeType.Start)
                    {
                        List<string> validNextTerminals = Graph.Grammar.GetNextTerminals(from.TerminalID);
                        bool validGrammar = validNextTerminals.Contains(edge.To.ToString());
                        from.ValidGrammar = validGrammar;
                        returnValid = validGrammar;
                    }
                
                    // validate middle
                    if (from.NodeType == QuestNode.ENodeType.Middle)
                    {
                        // check that the next terminal is valid
                        if (edge.To.GetType() == typeof(QuestNode))
                        {
                            List<string> validNextTerminals = Graph.Grammar.GetNextTerminals(from.TerminalID);
                            var validGrammar = validNextTerminals.Contains(edge.To.ToString());
                            from.ValidGrammar = validGrammar;
                        }
                        else
                        {
                            returnValid = from.ValidGrammar;
                        }
                        
                    }
                }
                else
                {
                    // branchis are grammarly valid 
                    nodeFrom.ValidGrammar = BranchNodeRootGrammar(nodeFrom);
                }
                
                // goal is unique
                if (edge.To is QuestNode { NodeType: QuestNode.ENodeType.Goal })
                    // validate goal
                {
                    
                    // if the from is valid(so is the goal). Because the "From" gets validated first
                    // by checking that the "To" is a valid terminal
                    edge.To.ValidGrammar = nodeFrom.ValidGrammar;
                    returnValid = edge.To.ValidGrammar;
                }   
            }
                
         
            return returnValid;
            
        }

        // Tries to retrieve from a branch Node the grammar of the immediate quest node root
        private bool BranchNodeRootGrammar(GraphNode nodeFrom)
        {
            foreach (var rootEdge in Graph.GetRoots(nodeFrom))
            {
                foreach (var from in rootEdge.From)
                {
                    // a quest node was found as root, get its grammar value
                    if (from.GetType() == typeof(QuestNode) & !from.IsValid())
                    {
                        // atleast one of the roots is not of valid grammar
                        return false;
                    }
                    // a branching node as root, keep searching within 
                    return BranchNodeRootGrammar(from);
                }
            }
            
            //all quest node roots were valid
            return true;
        }

        #endregion

        #region Getters

        public List<string> GetAllValidNextActionsInsert(
            string currentElement,
            Action<float> onProgress = null,
            CancellationToken token = default)
        {

            if (Graph == null || Graph.Grammar == null) return new List<string>();

            // get valid actions out of context
            onProgress?.Invoke((float)1);
            return Graph.Grammar.GetNextTerminals(currentElement);

        }

        public List<string> GetAllValidPrevActionsInsert(
            string currentElement, 
            Action<float> onProgress = null, 
            CancellationToken token = default)
        {

            if (Graph == null || Graph.Grammar == null) return new List<string>();

            // Get all non context prev actions   
            onProgress?.Invoke((float)1);
            return Graph.Grammar.GetPreviousTerminals(currentElement);
        }

        public List<List<string>> GetAllExpansions(
            string currentAction,
            Action<float> onProgress = null,
            CancellationToken token = default)
        {
            if (Graph == null || Graph.Grammar == null) return new List<List<string>>();

            // STEP 1: get raw expansions from grammar
            var rawExpansions = Graph.Grammar.GetExpansions(currentAction);
            if (rawExpansions == null || rawExpansions.Count == 0)
                return new List<List<string>>();

            var result = new HashSet<string>(); // for uniqueness (string key)
            var final = new List<List<string>>();

            for (int index = 0; index < rawExpansions.Count; index++)
            {
                if (token.IsCancellationRequested)
                    return final;

                var expansion = rawExpansions[index];
                var sequence = new List<string>();

                foreach (var element in expansion)
                {
                    if (token.IsCancellationRequested)
                        return final;

                    if (Graph.Grammar.IsTerminal(element))
                    {
                        sequence.Add(element);
                    }
                    else
                    {
                        var terminals = new HashSet<string>();
                        Graph.Grammar.GetFirstTerminals(element);

                        sequence.AddRange(terminals);
                    }
                }

                // skip useless self-only expansions
                if (sequence.Count == 1 && sequence[0] == currentAction)
                    continue;

                // ensure uniqueness (since List<> in HashSet is broken)
                var key = string.Join("|", sequence);
                if (result.Add(key))
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
                var node = Graph.ExpandNode(expandAction, referenceNode);
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
                Graph.InsertQuestNodeAfter(action, referenceNode);
            };
        }

        public Action InsertPreviousAction(string action, QuestNode referenceNode)
        {
            return () =>
            {
                var stopwatch = Stopwatch.StartNew();

                Graph.InsertQuestNodeBefore(action, referenceNode);
                Graph.ValidateGraph();

                stopwatch.Stop();
                Debug.Log($"InsertPreviousAction took {stopwatch.ElapsedMilliseconds} ms");
            };
        }

        #endregion

        public override void OnAttachLayer(LBSLayer layer)
        {
            base.OnAttachLayer(layer);
            ActionExtensions.AddUnique(ref Graph.OnNodeSelected, CallAssistant);
        }

        private void CallAssistant(GraphNode node) => OnCallAssistant?.Invoke(node);

        public override void OnGUI() { }


        public object ExecuteTest(bool b)
        {
            throw new NotImplementedException(); 
            
        }
    }
}