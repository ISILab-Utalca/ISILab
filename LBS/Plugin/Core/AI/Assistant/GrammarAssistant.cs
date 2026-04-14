using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ISILab.AI.Grammar;
using ISILab.Commons.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [Serializable]
    [RequieredModule(typeof(QuestGraph))]
    public class GrammarAssistant : LBSAssistant
    {
        private QuestBehaviour questBehaviour;
        private QuestGraph graph;

        [JsonIgnore]
        public QuestGraph Graph => graph ??= OwnerLayer.GetModule<QuestGraph>();
        public QuestBehaviour Behavior => questBehaviour ??= OwnerLayer.GetBehaviour<QuestBehaviour>();

        public GrammarAssistant(string IconGuid, string name, Color colorTint)
            : base(IconGuid, name, colorTint) { }

        public override object Clone()
        {
            return new GrammarAssistant(IconGuid, this.Name, this.ColorTint);
        }

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
                        List<string> validNextTerminals = GetAllValidNextActions(from.QuestAction);
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
                            List<string> validNextTerminals = GetAllValidNextActions(from.QuestAction);
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

        public List<string> GetAllValidNextActions(string currentElement)
        {
            var grammar = Graph.Grammar;
            var nextValidTerminals = new HashSet<string>();

            if (grammar == null) return nextValidTerminals.ToList();

            // Step 1: Get rules that can produce currentAction
            List<string> owningRules = GetOwningRules(currentElement);
            foreach (var owningRule in owningRules.ToList())
            {
                owningRules.AddRange(GetRulesWithRule(owningRule));
            }
            owningRules.RemoveDuplicates();

            // Step 2: Collect all relevant expansions
            HashSet<GrammarRule> itemsWithRule = new HashSet<GrammarRule>();
            foreach (GrammarRule rule in grammar.LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    // Include expansions with currentAction or its owning rules
                    if (expansion.sequence.Contains(currentElement) || 
                        owningRules.Any(rule => expansion.sequence.Contains(rule)))
                    {
                        itemsWithRule.Add(rule);
                    }
                }
            }

            // Step 3: Find next terminals
            foreach (var ruleItem in itemsWithRule)
            {
                for (int i = 0; i < ruleItem.Expansions.Count - 1; i++)
                {
                    var expansion = ruleItem.Expansions[i];
                    bool isCurrentAction = false;
                    if (expansion.sequence.Count <= 1) continue;
                    // Check if current item matches currentAction or can produce it
                    for (int j = 0; j < expansion.sequence.Count; j++)
                    {
                        string item = expansion.sequence[j];
                        if (grammar.IsRule(item))
                        {
                            if (grammar.GetFirstTerminals(item).Contains(currentElement))
                            {
                                isCurrentAction = true;
                            }
                        }
                        else if (expansion.Equals(currentElement))
                        {
                            isCurrentAction = true;
                        }

                        if(j+1 > expansion.sequence.Count-1) continue;

                        if (isCurrentAction)
                        {
                            var nextElement = expansion.sequence[j + 1];
                            if (grammar.IsRule(nextElement))
                            {
                                // Add all first terminals of the next rule
                                nextValidTerminals.UnionWith(grammar.GetFirstTerminals(nextElement));
                            }
                            else
                            {
                                nextValidTerminals.Add(nextElement);
                            }
                        }
                    }
                  
                }
            }
            
            return nextValidTerminals.ToList();
        }
        
        public List<string> GetAllValidNextActionsInsert(string currentElement, QuestGraph questGraph, Action<float> onProgress = null, CancellationToken token = default)
        {
            // get valid actions out of context
            var nextValidTerminals = GetAllValidNextActions(currentElement);
            
            // Simulate to only get if its valid in the new context for insert
            HashSet<string> nextValidInsert = new HashSet<string>();
            for (var index = 0; index < nextValidTerminals.Count; index++)
            {
                if(token.IsCancellationRequested) return nextValidTerminals.ToList();
                
                var nextValidTerminal = nextValidTerminals[index];
                CloneRefs.Start();
                var clone = questGraph.Clone() as QuestGraph;
                CloneRefs.End();

                onProgress?.Invoke((float)index/nextValidTerminals.Count);

                if (clone is null) break;

                if(token.IsCancellationRequested) return nextValidTerminals.ToList();
                clone.OwnerLayer = questGraph.OwnerLayer;
                var newNode = clone.InsertQuestNodeAfter(nextValidTerminal, Behavior.SelectedQuestNode);
                if (newNode.ValidGrammar)
                {
                    nextValidInsert.Add(nextValidTerminal);
                }

                clone.OwnerLayer = null;
            }

            return nextValidInsert.ToList();
        }
        
        public List<string> GetAllValidPrevActions(string currentAction)
        {
            var grammar = Graph.Grammar;
            var prevValidTerminals = new HashSet<string>();

            if (grammar == null) return prevValidTerminals.ToList();

            var rules = grammar.LBSRules;

            // Step 1: Check for the next as current
            foreach (var rule in rules)
            {
                for (int j = 0; j < rule.Expansions.Count; j++)
                {
                    List<string> expansion = rule.Expansions[j].sequence;
                    for (int i = 0; i < expansion.Count - 1; i++)
                    {
                        var nextElement = expansion[i+1];
                        if (grammar.IsRule(nextElement))
                        {
                            // if the next symbols a ruleRef get the first valid terminal
                            nextElement = grammar.GetFirstTerminals(nextElement).First();
                        }
                        // if the next symbol is the action we are searching for
                        if (nextElement.Equals(currentAction))
                        {
                            var currentElement = expansion[i];
                            if (grammar.IsRule(currentElement))
                            {
                                // if the first next a ruleRef get the first valid terminal
                                currentElement = grammar.GetFirstTerminals(currentElement).First();
                            }
                            
                            // assign the current as a valid prev, because the next is the current
                            prevValidTerminals.Add(currentElement);
                        }
                    }
                }
            }

            return prevValidTerminals.ToList();
        }

        public List<string> GetAllValidPrevActionsInsert(string currentAction, QuestGraph questGraph,  Action<float> onProgress = null, CancellationToken token = default)
        {
            // Get all non context prev actions   
            var prevValidTerminals = GetAllValidPrevActions(currentAction);
            // Simulate to only get if its valid in the new context for insert
            HashSet<string> prevValidInsert = new HashSet<string>();
            for (var index = 0; index < prevValidTerminals.Count; index++)
            {
                if(token.IsCancellationRequested) return prevValidTerminals.ToList();
                
                var nextValidTerminal = prevValidTerminals[index];
                CloneRefs.Start();
                var clone = questGraph.Clone() as QuestGraph;
                CloneRefs.End();

                onProgress?.Invoke((float)index/prevValidTerminals.Count);
                
                if (clone is null) break;
                clone.OwnerLayer = questGraph.OwnerLayer;

                if(token.IsCancellationRequested) return prevValidTerminals.ToList();
                
                var newNode = clone.InsertQuestNodeBefore(nextValidTerminal, Behavior.SelectedQuestNode);
                if (newNode.ValidGrammar)
                {
                    prevValidInsert.Add(nextValidTerminal);
                }

                clone.OwnerLayer = null;
            }

            return prevValidInsert.ToList();
        }
        
        public List<List<string>> GetAllExpansions(string currentAction,Action<float> onProgress = null, CancellationToken token = default)
        {
            HashSet<List<string>> allExpansions = new HashSet<List<string>>();
            var grammar = Graph.Grammar;
            if (grammar == null) return allExpansions.ToList();

            var expansions = new List<List<string>>();
            // Get all the rules that contain the terminal
            foreach (var rule in GetOwningRules(currentAction))
            {
                if(token.IsCancellationRequested) return allExpansions.ToList();
                foreach (var ruleEntry in grammar.LBSRules)
                {
                    if(token.IsCancellationRequested) return allExpansions.ToList();
                    if (ruleEntry.id.Equals(rule))
                    {
                        if(token.IsCancellationRequested) return allExpansions.ToList();
                        foreach (var expansion in ruleEntry.Expansions)
                        {
                            if(token.IsCancellationRequested) return allExpansions.ToList();
                            expansions.Add(expansion.sequence);
                        }
                    }
                }
            }

            if (expansions is null or { Count: 0 }) return allExpansions.ToList();

            // Use a HashSet to track unique sequence content as strings
            HashSet<string> seenSequences = new HashSet<string>();

            // Get terminals from the quests
            for (var index = 0; index < expansions.Count; index++)
            {
                if(token.IsCancellationRequested) return allExpansions.ToList();
                
                var expansion = expansions[index];
                List<string> sequence = new List<string>();

                foreach (var element in expansion)
                {
                    if(token.IsCancellationRequested) return allExpansions.ToList();
                    
                    if (grammar.IsTerminal(element))
                    {
                        sequence.Add(element);
                    }
                    else
                    {
                        sequence.Add(grammar.GetFirstTerminals(element).First());
                    }
                }

                // do not add sequences that return the same action only
                if (sequence.Count == 1 && sequence[0] == currentAction) continue;
                allExpansions.Add(sequence);
                
                onProgress?.Invoke((float)index/expansions.Count);
            }

            return allExpansions.ToList();
        }

        private List<string> GetRulesWithRule(string rule)
        {
            var grammar = Graph.Grammar;
            if (grammar == null) return new List<string>();
            
            HashSet<string> owningRules = new HashSet<string>();
            foreach (GrammarRule ruleEntry in Graph.Grammar.LBSRules)
            {
                foreach (var expansion in ruleEntry.Expansions)
                {
                    // if the rule we are checking for is within the rule item
                    if (expansion.sequence.Contains(rule))
                    {
                        owningRules.Add(ruleEntry.id);
                    }
                }
            }
            return owningRules.ToList();
        }
        
        public List<string> GetOwningRules(string terminal)
        {
            var grammar = Graph.Grammar;
            if (grammar == null) return new List<string>();

            HashSet<string> owners = new HashSet<string>();
            
            foreach (GrammarRule rule in Graph.Grammar.LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    if(expansion.sequence.Contains(terminal)) owners.Add(rule.id);
                }
            }
            
            return owners.ToList();
        }

        
        public Action ExpandAction(List<string> expandAction, QuestNode referenceNode)
        {
            return () =>
            {
                var stopwatch = Stopwatch.StartNew();

                Graph.ExpandNode(expandAction, referenceNode);

                stopwatch.Stop();
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