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
                if (rules == null || rules.Count == 0)
                {
                    rules = new List<string>();
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

        public List<string> GetOwningRules(string element)
        {
            HashSet<string> owningRules = new HashSet<string>();
            foreach (GrammarRule rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    if (expansion.sequence.Contains(element) || IsElementInRuleDerivation(rule.id, element, new HashSet<string>()))
                        owningRules.Add(rule.id);
                }
            }
            return owningRules.ToList();
        }

        private bool IsElementInRuleDerivation(string ruleId, string targetElement, HashSet<string> visited)
        {
            if (!visited.Add(ruleId)) return false;
            var rule = GetRule(ruleId);
            if (rule == null) return false;

            foreach (var exp in rule.Expansions)
            {
                foreach (var item in exp.sequence)
                {
                    if (item == targetElement) return true;
                    if (IsRule(item) && IsElementInRuleDerivation(item, targetElement, visited)) return true;
                }
            }
            return false;
        }

        #endregion

        #region Terminal Retrieve

        public List<string> GetFirstTerminals(string element)
        {
            var result = new HashSet<string>();
            GetFirstTerminals(element, result, new HashSet<string>());
            return result.ToList();
        }

        public List<string> GetLastTerminals(string element)
        {
            var result = new HashSet<string>();
            GetLastTerminals(element, result, new HashSet<string>());
            return result.ToList();
        }

        public List<string> GetNextTerminals(string element)
        {
            var result = new HashSet<string>();
            GetNextTerminalsInternal(element, result, new HashSet<string>());
            return result.ToList();
        }

        public List<string> GetPreviousTerminals(string element)
        {
            var result = new HashSet<string>();
            GetPreviousTerminalsInternal(element, result, new HashSet<string>());
            return result.ToList();
        }

        /// <summary>
        /// Validates if a target terminal can be placed between a current element and a known following element.
        /// </summary>
        public bool IsValidNextTerminal(string currentElement, string terminalToInsert, string expectedNextElement)
        {
            if (!IsTerminal(terminalToInsert)) return false;

            // make sure the insert is a valid next
            var validNexts = GetNextTerminals(currentElement);
            if (!validNexts.Contains(terminalToInsert)) return false;

            // If there is a trailing neighbor, that neighbor must be a valid follow-up to our candidate terminal
            if (!string.IsNullOrEmpty(expectedNextElement))
            {
                var downstreamNexts = GetNextTerminals(terminalToInsert);

                if (IsTerminal(expectedNextElement))
                    return downstreamNexts.Contains(expectedNextElement);

                var expectedFirsts = GetFirstTerminals(expectedNextElement);

                // Match using Linq .Any() to check if any first-terminal matches what's next
                return expectedFirsts.Any(firstTerminal => downstreamNexts.Contains(firstTerminal));
            }

            return true;
        }

        /// <summary>
        /// Validates if a target terminal can be placed between a current element and a known preceding element.
        /// </summary>
        public bool IsValidPreviousTerminal(string currentElement, string terminalToInsert, string expectedPrevElement)
        {
            if (!IsTerminal(terminalToInsert)) return false;

            var validPrevs = GetPreviousTerminals(currentElement);
            if (!validPrevs.Contains(terminalToInsert)) return false;

            if (!string.IsNullOrEmpty(expectedPrevElement))
            {
                var upstreamPrevs = GetPreviousTerminals(terminalToInsert);
                if (IsTerminal(expectedPrevElement))
                    return upstreamPrevs.Contains(expectedPrevElement);

                var expectedLasts = GetLastTerminals(expectedPrevElement);

                // Match using Linq .Any() to check if any last-terminal matches what's previous
                return expectedLasts.Any(lastTerminal => upstreamPrevs.Contains(lastTerminal));
            }

            return true;
        }

        private void GetNextTerminalsInternal(string element, HashSet<string> result, HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            foreach (var rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    var seq = expansion.sequence;
                    for (int i = 0; i < seq.Count; i++)
                    {
                        if (IsTargetMatch(seq[i], element))
                        {
                            // If there's a subsequent item in the sequence
                            if (i < seq.Count - 1)
                            {
                                var next = seq[i + 1];
                                if (IsTerminal(next)) result.Add(next);
                                else GetFirstTerminals(next, result, new HashSet<string>());
                            }
                            else
                            {
                                // End of sequence reached: bubble up to parent rules
                                GetNextTerminalsInternal(rule.id, result, visited);
                            }
                        }
                    }
                }
            }
        }

        private void GetPreviousTerminalsInternal(string element, HashSet<string> result, HashSet<string> visited)
        {
            if (!visited.Add(element)) return;

            foreach (var rule in LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    var seq = expansion.sequence;
                    for (int i = 0; i < seq.Count; i++)
                    {
                        if (IsTargetMatch(seq[i], element))
                        {
                            // If there's an item preceding this one
                            if (i > 0)
                            {
                                var prev = seq[i - 1];
                                if (IsTerminal(prev)) result.Add(prev);
                                else GetLastTerminals(prev, result, new HashSet<string>());
                            }
                            else
                            {
                                // Beginning of sequence reached: bubble down from parent rules
                                GetPreviousTerminalsInternal(rule.id, result, visited);
                            }
                        }
                    }
                }
            }
        }

        private bool IsTargetMatch(string currentSequenceItem, string searchTarget)
        {
            if (currentSequenceItem == searchTarget) return true;

            // Recursively verify if the item derives the target structural string
            if (IsRule(currentSequenceItem))
            {
                return IsElementInRuleDerivation(currentSequenceItem, searchTarget, new HashSet<string>());
            }
            return false;
        }

        private void GetFirstTerminals(string element, HashSet<string> result, HashSet<string> visited)
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
                GetFirstTerminals(expansion.sequence[0], result, visited);
            }
        }

        private void GetLastTerminals(string element, HashSet<string> result, HashSet<string> visited)
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
                GetLastTerminals(expansion.sequence[^1], result, visited);
            }
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
                            var terms = new HashSet<string>();
                            GetFirstTerminals(item, terms, new HashSet<string>());
                            sequence.AddRange(terms);
                        }
                    }
                    result.Add(sequence);
                }
            }
            return result;
        }

        #endregion

        #region Debug
        [ContextMenu("Debug Grammar")]
        private void DebugGrammar()
        {
            Debug.Log($"[LBSGrammar] Terminal Actions Count: {terminals?.Count ?? 0}");
            Debug.Log($"[LBSGrammar] Rule Entries Count: {LBSRules?.Count ?? 0}");
        }

        internal void Clear()
        {
            terminals.Clear();
            rules.Clear();
            lbsTerminals.Clear();
            lbsRules.Clear();
        }
        #endregion
    }
}