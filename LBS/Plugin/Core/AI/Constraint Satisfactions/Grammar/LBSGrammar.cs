using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace ISILab.AI.Grammar
{

    [CreateAssetMenu(menuName = "ISILab/LBSGrammar")]
    public class LBSGrammar : ScriptableObject
    {
        [SerializeField]
        public List<GrammarRule> LBSRules = new();

        [SerializeField]
        public List<GrammarTerminal> LBSTerminals = new();

        private List<string> terminals = new List<string>();
        private List<string> rules = new List<string>();


        public List<string> TerminalActions
        {
            get
            {
                if(terminals.Count == 0)
                {
                    foreach (var terminal in LBSTerminals)
                    {
                        terminals.Add(terminal.id);
                    }
                }

                return terminals;
   
            }
        }
            
            
        public List<string> Rules
        {
            get
            {
                if (rules.Count == 0)
                {
                    foreach (var rule in LBSRules)
                    {
                        rules.Add(rule.id);
                    }
                }

                return rules;

            }
        }

        bool isRule(string id)
        {
            return Rules.Contains(id);
        }


        /// <summary>
        /// Recursively collects first terminal(s) from a rule.
        /// </summary>
        public List<string> GetFirstTerminals(string ruleName, HashSet<string> firstTerminals)
        {
            // we need to find the first terminal of each of the rules entries
            foreach (var rule in LBSRules)
            {
                // found the rule we need
                if (rule.id.Equals(ruleName))
                {
                    foreach (var ruleExpansion in rule.Expansions)
                    {
                        if (ruleExpansion.Count == 0) continue;
                        string firstString = ruleExpansion[0];
                        if (isRule(firstString))
                        {
                            // if begins with a rule, then call recursively
                            firstString = GetFirstTerminals(firstString, firstTerminals).First();
                        }
                        else
                        {
                            // terminal found
                            firstTerminals.Add(firstString);
                        }
                    }
                }
            }

            return firstTerminals.ToList();
        }

        public List<string> GetLastTerminals(string ruleName, HashSet<string> lastTerminals)
        {
            // we need to find the last terminal of each of the rules entries
            foreach (var rule in LBSRules)
            {
                // found the rule we need
                if (rule.id.Equals(ruleName))
                {
                    foreach (var ruleExpansion in rule.Expansions)
                    {
                        if (ruleExpansion.Count == 0) continue;
                        string lastString = ruleExpansion.LastOrDefault();
                        if (isRule(lastString))
                        {
                            // if ends with a rule, then call recursively
                            lastString = GetLastTerminals(lastString, lastTerminals).First();
                        }
                        else
                        {
                            // terminal found
                            lastTerminals.Add(lastString);
                        }
                    }
                }
            }

            return lastTerminals.ToList();
        }
        
        public List<string> GetNextTerminals(string current, HashSet<string> nextTerminals)
        {
            // we need to find the first terminal of each of the rules entries
            foreach (var ruleExpansion in LBSRules)
            {
                // found the rule we need
                if (ruleExpansion.id.Equals(current))
                {
                    // we try to get the next value in the expansion
                    var item = ruleExpansion.Expansions.ElementAt(0);
                    if(item.Count < 2) continue;
                    
                    var nextString = item.ElementAt(1);
                    if (isRule(nextString))
                    {
                        // next is a rule therefore we need the next terminal of the rule
                        nextString = GetFirstTerminals(nextString, nextTerminals).First();
                    }
                    else
                    {
                        // next terminal found
                        nextTerminals.Add(nextString);
                    }
                    
                }
            }

            return nextTerminals.ToList();
        }
        
   

        /// <summary>
        /// Debug method to inspect serialized data.
        /// </summary>
        [ContextMenu("Debug Grammar")]
        private void DebugGrammar()
        {
            Debug.Log($"[LBSGrammar] Terminal Actions Count: {terminals?.Count ?? 0}");
            foreach (var action in terminals ?? new List<string>())
            {
                Debug.Log($"[LBSGrammar] Terminal: {action}");
            }

            Debug.Log($"[LBSGrammar] Rule Entries Count: {LBSRules?.Count ?? 0}");
            foreach (var rule in LBSRules ?? new List<GrammarRule>())
            {
                Debug.Log($"[LBSGrammar] Rule: {rule.ruleID}, Expansions: {rule.definitions?.Count ?? 0}");
                foreach (var expansion in rule.definitions ?? new List<GrammarRule>())
                {
                    Debug.Log($"[LBSGrammar]   Expansion: [{string.Join(", ", expansion.items ?? new List<string>())}]");
                }
            }
        }

        // Ensure initialization of serialized fields
        private void OnEnable()
        {
            terminals ??= new List<string>();
            ruleEntries ??= new List<GrammarRule>();
        }

    
    }


}