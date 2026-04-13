using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    public static class LBSGrammarReader
    {

        public static void ReadGrammar(LBSGrammar lbsGrammar, string path)
        {
            try
            {

                XmlDocument xml = new XmlDocument();
                xml.Load(path);

                lbsGrammar.LBSRules.Clear();
                lbsGrammar.LBSTerminals.Clear();

                RemoveTerminalNodes(xml);

                using (var reader = new XmlNodeReader(xml))
                {
                    var srgsDoc = new SrgsDocument(reader);

                    ParseRules(srgsDoc, lbsGrammar);
                    ParseTerminals(lbsGrammar);
                }

                ParseTerminalFields(xml, lbsGrammar);

                EditorUtility.SetDirty(lbsGrammar);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(); 
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LBSGrammarReader] Failed to parse grammar: {ex.Message}");
                throw;
            }
        }


        private static void ParseTerminals(LBSGrammar lbsGrammar)
        {
            HashSet<string> terminalSet = new HashSet<string>();

            foreach (var rule in lbsGrammar.LBSRules)
            {
                foreach (var expansion in rule.Expansions)
                {
                    foreach (var token in expansion.sequence)
                    {
                        if (!lbsGrammar.IsRule(token))
                            terminalSet.Add(token);
                    }
                }
            }

            lbsGrammar.LBSTerminals.Clear();

            foreach (var terminal in terminalSet)
            {
                var newTerminal = ScriptableObject.CreateInstance<GrammarTerminal>();
                newTerminal.id = terminal;
                newTerminal.name = $"Terminal_{terminal}";

                AssetDatabase.AddObjectToAsset(newTerminal, lbsGrammar);

                EditorUtility.SetDirty(newTerminal); // ✅ IMPORTANT
                lbsGrammar.LBSTerminals.Add(newTerminal);
            }
        }

        /// <summary>
        ///  Parsing Grammar assumes that your grammar meets the following requirements
        /// Rules: Start with #, followed by a Cap Character
        /// Terminals: Start with Cap
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static void ParseRules(SrgsDocument doc, LBSGrammar lbsGrammar)
        {
            foreach (var docRule in doc.Rules)
            {
                var newRule = ScriptableObject.CreateInstance<GrammarRule>();
                newRule.id = docRule.Id;
                newRule.name = $"Rule_{newRule.id}";

                AssetDatabase.AddObjectToAsset(newRule, lbsGrammar);

                EditorUtility.SetDirty(newRule); // ✅ IMPORTANT
                lbsGrammar.LBSRules.Add(newRule);

                foreach (var element in docRule.Elements)
                {
                    ParseExpansions(element, newRule.Expansions);
                }
            }
        }

        private static void ParseExpansions(SrgsElement element, List<GrammarExpansion> expansions)
        {
            switch (element)
            {
                case SrgsOneOf oneOf:
                    foreach (var item in oneOf.Items)
                        ParseExpansions(item, expansions);
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

                    if (sequence.Count > 0){
                        GrammarExpansion expansion = new GrammarExpansion();
                        expansion.sequence = sequence;
                        expansions.Add(expansion); 
                    }
                    break;
            }
        }

        private static void ParseTerminalFields(XmlDocument xml, LBSGrammar lbsGrammar)
        {
            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("g", "http://www.w3.org/2001/06/grammar");

            var terminalNodes = xml.SelectNodes("//g:terminal", ns);

            var terminalMap = new Dictionary<string, GrammarTerminal>();

            foreach (var t in lbsGrammar.LBSTerminals)
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
                        Debug.Log($"[Parser[{terminal.id}]] added field: {type} ({name})");
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
