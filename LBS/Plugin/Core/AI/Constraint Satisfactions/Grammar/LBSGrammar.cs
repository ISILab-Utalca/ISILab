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
        #region Fields
        [SerializeField]
        public List<GrammarRule> lbsRules = new();
        [SerializeField]
        public List<GrammarTerminal> lbsTerminals = new();

        [SerializeField]
        private List<string> terminals = new List<string>();
        [SerializeField]        
        private List<string> rules = new List<string>();

        [SerializeField]
        private string pathGuid;
        #endregion

        #region PROPERTIES

        public List<GrammarRule> LBSRules => lbsRules;
        public List<GrammarTerminal> LBSTerminals => lbsTerminals;

        public List<string> TerminalActions
        {
            get
            {
                terminals.Clear();
                foreach (var terminal in LBSTerminals)
                {
                    terminals.Add(terminal.id);
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

        public string PathGUID { get => pathGuid; set => pathGuid = value; }


        #endregion


        #region METHODS

        public bool IsRule(string id) => Rules.Contains(id);
        public bool IsTerminal(string id) => TerminalActions.Contains(id);


        public GrammarTerminal GetTerminal(string id) => LBSTerminals.FirstOrDefault(t => t.id.Equals(id));
        public GrammarRule GetRule(string id) => LBSRules.FirstOrDefault(r => r.id.Equals(id));
        public object GetGrammarObject(string id)
        {
            if (IsRule(id)) return GetRule(id);
            if (IsTerminal(id)) return GetTerminal(id);
            return null;
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
                    foreach (var expansion in rule.Expansions)
                    {
                        if (expansion.sequence.Count == 0) continue;
                        string firstString = expansion.sequence[0];
                        if (IsRule(firstString))
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
                    foreach (var expansion in rule.Expansions)
                    {
                        if (expansion.sequence.Count == 0) continue;
                        string lastString = expansion.sequence.LastOrDefault();
                        if (IsRule(lastString))
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
                    if(item.sequence.Count < 2) continue;
                    
                    var nextString = item.sequence.ElementAt(1);
                    if (IsRule(nextString))
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
                Debug.Log($"[LBSGrammar] Rule: {rule.id}, Expansions: {rule.Expansions?.Count ?? 0}");
                for (int j = 0; j < rule.Expansions.Count; j++)
                {
                    List<string> expansion = rule.Expansions[j].sequence;
                    Debug.Log($"Expansion {j}: ");
                    for (int i = 0; i < expansion.Count; i++)
                    {
                        string item = expansion[i];
                        if (i == 0) Debug.Log(item);
                        else Debug.Log(" -> " + item);
                    }
                }
            }
        }

        // Ensure initialization of serialized fields
        private void OnEnable()
        {
            
            terminals ??= new List<string>();
            rules ??= new List<string>();
        }

        #endregion
    }


}