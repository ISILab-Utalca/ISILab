using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        // Ensure initialization of serialized fields
        private void OnEnable()
        {
            terminals ??= new List<string>();
            rules ??= new List<string>();
        }

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


        #region Terminal Retrieve

        public List<string> GetFirstTerminals(string ruleName) =>
            GetFirstTerminals(ruleName, new HashSet<string>(), new HashSet<string>()).ToList();

        public List<string> GetNextTerminals(string ruleName) =>
            GetNextTerminals(ruleName, new HashSet<string>(), new HashSet<string>()).ToList();

        public List<string> GetLastTerminals(string ruleName) =>
            GetLastTerminals(ruleName, new HashSet<string>(), new HashSet<string>()).ToList();


        /// <summary>
        /// Recursively collects first terminal(s) from a rule.
        /// </summary>
        private HashSet<string> GetFirstTerminals(string ruleName,
            HashSet<string> result,
            HashSet<string> visited)
        {
            if (!visited.Add(ruleName)) return result;

            var rule = LBSRules.FirstOrDefault(r => r.id == ruleName);
            if (rule == null) return result;

            foreach (var expansion in rule.Expansions)
            {
                if (expansion.sequence.Count == 0) continue;

                string firstElement = expansion.sequence[0];

                if (IsRule(firstElement))
                {
                    GetFirstTerminals(firstElement, result, visited);
                }
                else
                {
                    result.Add(firstElement);
                }
            }

            return result;
        }

        private HashSet<string> GetLastTerminals(string ruleName,
            HashSet<string> result,
            HashSet<string> visited)
        {
            if (!visited.Add(ruleName)) return result;

            // we need to find the last terminal of each of the rules entries
            foreach (var rule in LBSRules)
            {
                // found the rule we need
                if (rule.id.Equals(ruleName))
                {
                    foreach (var expansion in rule.Expansions)
                    {
                        if (expansion.sequence.Count == 0) continue;
                        string lastElement = expansion.sequence.LastOrDefault();
                        if (IsRule(lastElement) && lastElement != ruleName)
                        {
                            // if ends with a rule, then call recursively
                            lastElement = GetLastTerminals(lastElement, result, visited).First();
                        }
                        else
                        {
                            // terminal found
                            result.Add(lastElement);
                        }
                    }
                }
            }

            return result;
        }

        private HashSet<string> GetNextTerminals(
        string ruleName,
        HashSet<string> result,
        HashSet<string> visited)
        {
            if (!visited.Add(ruleName)) return result;
            // we need to find the first terminal of each of the rules entries
            foreach (var rule in LBSRules)
            {
                // found the rule we need
                if (rule.id.Equals(ruleName))
                {
                    // we try to get the next value in the expansion
                    var item = rule.Expansions.ElementAt(0);
                    if(item.sequence.Count < 2) continue;
                    
                    var nextElement = item.sequence.ElementAt(1);
                    if (IsRule(nextElement) && nextElement != ruleName)
                    {
                        // next is a rule therefore we need the first terminal of the rule
                        nextElement = GetFirstTerminals(nextElement, result, new HashSet<string>()).First();
                    }
                    else
                    {
                        // next terminal found
                        result.Add(nextElement);
                    }
                    
                }
            }

            return result;
        }

        #endregion

        #region Debug
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
        #endregion


        #endregion
    }


}