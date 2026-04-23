using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Graphs;
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
                // Only rebuild if it's empty or null, don't clear every time
                if (terminals == null || terminals.Count == 0)
                {
                    terminals = lbsTerminals.Select(t => t.id).ToList();
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

        public bool IsRule(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return Rules.Contains(id);
        }

        public bool IsTerminal(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return TerminalActions.Contains(id);
        }

        public GrammarTerminal GetTerminal(string id) => LBSTerminals.FirstOrDefault(t => t.id.Equals(id));
        public GrammarRule GetRule(string id) => LBSRules.FirstOrDefault(r => r.id.Equals(id));
        public object GetGrammarElement(string id)
        {
            if (IsRule(id)) return GetRule(id);
            if (IsTerminal(id)) return GetTerminal(id);
            return null;
        }

        /// <summary>
        /// returns any rule that contains a grammar element.
        /// </summary>
        /// <param name="element">a grammar element. Rule or Terminal</param>
        /// <returns></returns>
        public List<string> GetOwningRules(string element)
        {
            HashSet<string> owningRules = new HashSet<string>();

            foreach (GrammarRule rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    if (expansion.sequence.Contains(element)) owningRules.Add(rule.id);
                }
            }

            return owningRules.ToList();
        }

        #region Terminal Retrieve

        public List<string> GetFirstTerminals(string element)
        {
            var result = new HashSet<string>();
            GetNextTerminals(element, result, new HashSet<string>());

            return result.ToList();
        }

        public List<string> GetNextTerminals(string element)
        {
            var result = new HashSet<string>();
            GetNextTerminals(element, result, new HashSet<string>());

            return result.ToList();
        }


        public List<string> GetLastTerminals(string element)
        {
            var result = new HashSet<string>();
            GetLastTerminals(element, result, new HashSet<string>());

            return result.ToList();
        }

        public List<string> GetPreviousTerminals(string element)
        {
            var result = new HashSet<string>();
            GetPreviousTerminals(element, result, new HashSet<string>());

            return result.ToList();
        }

        public List<List<string>> GetExpansions(string element)
        {
            var result = new List<List<string>>();

            var owningRules = GetOwningRules(element);

            foreach (var ruleId in owningRules)
            {
                var rule = GetRule(ruleId);
                if (rule == null) continue;

                foreach (var expansion in rule.Expansions)
                {
                    var sequence = new List<string>();

                    foreach (var item in expansion.sequence)
                    {
                        if (IsTerminal(item))
                        {
                            sequence.Add(item);
                        }
                        else
                        {
                            var terminals = new HashSet<string>();
                            GetFirstTerminals(item, terminals, new HashSet<string>());

                            // pick ALL, not just first (fixes your previous bug)
                            sequence.AddRange(terminals);
                        }
                    }

                    result.Add(sequence);
                }
            }

            return result;
        }

        private void GetPreviousTerminals(
        string element,
        HashSet<string> result,
        HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            foreach (var rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    var seq = expansion.sequence;

                    for (int i = 0; i < seq.Count - 1; i++)
                    {
                        var next = seq[i + 1];

                        // match rule OR terminal
                        if (Matches(next, element))
                        {
                            var prev = seq[i];

                            if (IsRule(prev))
                            {
                                GetLastTerminals(prev, result, new HashSet<string>());
                            }
                            if (IsTerminal(prev))
                            {
                                result.Add(prev);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively collects first terminal(s) from a rule.
        /// </summary>
        private void GetFirstTerminals(
          string element,
          HashSet<string> result,
          HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            if (IsTerminal(element))
            {
                result.Add(element);
                return;
            }

            var rule = GetRule(element);
            if (rule == null) return;

            foreach (var expansion in rule.Expansions)
            {
                if (expansion.sequence.Count == 0) continue;

                var first = expansion.sequence[0];
                GetFirstTerminals(first, result, visited);
            }
        }

        private void GetLastTerminals(
     string element,
     HashSet<string> result,
     HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            if (IsTerminal(element))
            {
                result.Add(element);
                return;
            }

            var rule = GetRule(element);
            if (rule == null) return;

            foreach (var expansion in rule.Expansions)
            {
                if (expansion.sequence.Count == 0) continue;

                var last = expansion.sequence[^1];
                GetLastTerminals(last, result, visited);
            }
        }

        private void GetNextTerminals(
     string element,
     HashSet<string> result,
     HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            foreach (var rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    var seq = expansion.sequence;

                    for (int i = 0; i < seq.Count - 1; i++)
                    {
                        var current = seq[i];

                        // ✅ match rule OR terminal
                        if (Matches(current, element))
                        {
                            var next = seq[i + 1];

                            if (IsRule(next))
                            {
                                GetFirstTerminals(next, result, new HashSet<string>());
                            }
                            else
                            {
                                result.Add(next);
                            }
                        }
                    }
                }
            }
        }

        private bool Matches(string element, string target)
        {
            if (element == target) return true;

            if (IsRule(element))
            {
                var temp = new HashSet<string>();
                GetFirstTerminals(element, temp, new HashSet<string>());
                return temp.Contains(target);
            }

            return false;
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