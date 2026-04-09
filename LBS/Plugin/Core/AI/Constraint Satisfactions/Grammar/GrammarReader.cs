using System;
using System.Collections.Generic;
using System.Speech.Recognition.SrgsGrammar;
using System.Xml;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    public static class LBSGrammarReader
    {

        public static GrammarData ReadGrammar(string path)
        {
            try
            {
                var grammar = ScriptableObject.CreateInstance<GrammarData>();

                XmlDocument xml = new XmlDocument();
                xml.Load(path);

                RemoveTerminalNodes(xml);

                using (var reader = new XmlNodeReader(xml))
                {
                    var srgsDoc = new SrgsDocument(reader);

                    ParseRules(srgsDoc, grammar);
                    ExtractTerminalActions(grammar);
                }

                ParseTerminals(xml, grammar);

                return grammar;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LBSGrammarReader] Failed to parse grammar: {ex.Message}");
                throw;
            }
        }

        private static bool IsRule(string token, GrammarData grammar)
        {
            return grammar.LBSRules.Exists(r => r.id == token);
        }

        private static void ExtractTerminalActions(GrammarData grammar)
        {
            HashSet<string> terminalSet = new HashSet<string>();

            foreach (var rule in grammar.LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    foreach (var token in expansion)
                    {
                        if (!IsRule(token, grammar))
                            terminalSet.Add(token);
                    }
                }
            }

            grammar.LBSTerminals.Clear();

            foreach (var terminal in terminalSet)
            {
                var newTerminal = ScriptableObject.CreateInstance<GrammarTerminal>();
                newTerminal.id = terminal;
                grammar.LBSTerminals.Add(newTerminal);
            }
        }

        /// <summary>
        ///  Parsing Grammar assumes that your grammar meets the following requirements
        /// Rules: Start with #, followed by a Cap Character
        /// Terminals: Start with Cap
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static void ParseRules(SrgsDocument doc, GrammarData grammar)
        {
            foreach (var docRule in doc.Rules)
            {
                var newRule = new GrammarRule
                {
                    id = docRule.Id
                };

                foreach (var element in docRule.Elements)
                {
                    ExtractExpansionSequences(element, newRule.Expansions);
                }

                grammar.LBSRules.Add(newRule);
            }
        }

        private static void ExtractExpansionSequences(SrgsElement element, List<List<string>> expansions)
        {
            switch (element)
            {
                case SrgsOneOf oneOf:
                    foreach (var item in oneOf.Items)
                        ExtractExpansionSequences(item, expansions);
                    break;

                case SrgsItem item:
                    var sequence = new List<string>();

                    foreach (var sub in item.Elements)
                    {
                        switch (sub)
                        {
                            case SrgsText text:
                                var value = text.Text.Trim();
                                if (!string.IsNullOrEmpty(value))
                                    sequence.Add(value);
                                break;

                            case SrgsRuleRef ruleRef:
                                sequence.Add(ruleRef.Uri.ToString().TrimStart('#'));
                                break;
                        }
                    }

                    if (sequence.Count > 0)
                        expansions.Add(sequence);
                    break;
            }
        }

        private static void ParseTerminals(XmlDocument xml, GrammarData grammar)
        {
            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("g", "http://www.w3.org/2001/06/grammar");

            var terminalNodes = xml.SelectNodes("//g:terminal", ns);

            var terminalMap = new Dictionary<string, GrammarTerminal>();

            foreach (var t in grammar.LBSTerminals)
                terminalMap[t.id] = t;

            foreach (XmlNode terminalNode in terminalNodes)
            {
                string id = terminalNode.Attributes["id"]?.Value;

                if (!terminalMap.TryGetValue(id, out var terminal))
                {
                    Debug.LogWarning($"[Parser] Terminal '{id}' defined in XML but not used in grammar.");
                    continue;
                }

                terminal.fields.Clear();

                foreach (XmlNode fieldNode in terminalNode.SelectNodes("g:field", ns))
                {
                    string type = fieldNode.Attributes["type"]?.Value;
                    string name = fieldNode.Attributes["name"]?.Value;

                    try
                    {
                        var field = GrammarField.CreateField(type, name);
                        terminal.fields.Add(field);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Parser] Failed field: {type} ({name}) → {ex.Message}");
                    }
                }
            }
        }

        private static void RemoveTerminalNodes(XmlDocument xml)
        {
            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("g", "http://www.w3.org/2001/06/grammar");

            var terminalNodes = xml.SelectNodes("//g:terminal", ns);

            foreach (XmlNode node in terminalNodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            Debug.Log($"[Parser] Removed {terminalNodes.Count} terminal nodes before SRGS parsing.");
        }
    }
}
