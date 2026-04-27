using ISILab.AI.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class LBSGrammarReader
{

    private static void ClearSubAssets(LBSGrammar grammar)
    {
        string path = AssetDatabase.GetAssetPath(grammar);
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in subAssets)
        {
            if (asset == grammar) continue;
            UnityEngine.Object.DestroyImmediate(asset, true);
        }
    }

    public static void ReadGrammar(LBSGrammar grammar, string path)
    {
        ClearSubAssets(grammar);
        try
        {
            // ORIGINAL XML (keep terminals)
            XmlDocument xml = new XmlDocument();
            xml.Load(path);

            // COPY for SRGS (remove the terminal definition because SRGS do not read them by default)
            XmlDocument srgsXml = new XmlDocument();
            srgsXml.LoadXml(xml.OuterXml);

            grammar.LBSRules.Clear();
            grammar.LBSTerminals.Clear();

            // Remove terminals ONLY from SRGS copy
            RemoveTerminalNodes(srgsXml);

            SrgsDocument srgs = ParseSrgs(srgsXml);

            CreateRules(srgs, grammar);
            CreateTerminals(grammar);

            // get terminals from original xml
            ApplyTerminalFields(xml, grammar);

            EditorUtility.SetDirty(grammar);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GrammarReader] {ex.Message}");
            throw;
        }
    }

    private static XmlDocument LoadXml(string path)
    {
        var xml = new XmlDocument();
        xml.Load(path);
        return xml;
    }

    private static SrgsDocument ParseSrgs(XmlDocument xml)
    {
        using var reader = new XmlNodeReader(xml);
        return new SrgsDocument(reader);
    }

    #region Rules

    private static void CreateRules(SrgsDocument doc, LBSGrammar grammar)
    {
        foreach (var srgsRule in doc.Rules)
        {
            var rule = CreateRuleAsset(srgsRule.Id, grammar);

            foreach (var element in srgsRule.Elements)
                ExtractExpansions(element, rule.Expansions);
        }
    }

    private static GrammarRule CreateRuleAsset(string id, LBSGrammar grammar)
    {
        var rule = ScriptableObject.CreateInstance<GrammarRule>();
        rule.id = id;
        rule.name = $"Rule_{id}";

        AssetDatabase.AddObjectToAsset(rule, grammar);
        EditorUtility.SetDirty(rule);

        grammar.LBSRules.Add(rule);
        return rule;
    }

    private static void ExtractExpansions(SrgsElement element, List<GrammarExpansion> expansions)
    {
        if (element is SrgsOneOf oneOf)
        {
            foreach (SrgsItem srgsItem in oneOf.Items)
                ExtractExpansions(srgsItem, expansions);

            return;
        }

        if (element is not SrgsItem item) return;

        var sequence = ExtractSequence(item);

        if (sequence.Count == 0) return;

        expansions.Add(new GrammarExpansion { sequence = sequence });
    }

    private static List<string> ExtractSequence(SrgsItem item)
    {
        var sequence = new List<string>();

        foreach (var sub in item.Elements)
        {
            switch (sub)
            {
                case SrgsText text:
                    AddIfValid(sequence, text.Text);
                    break;

                case SrgsRuleRef ruleRef:
                    sequence.Add(ruleRef.Uri.ToString().TrimStart('#'));
                    break;
            }
        }

        return sequence;
    }

    private static void AddIfValid(List<string> list, string value)
    {
        value = value?.Trim();
        if (!string.IsNullOrEmpty(value))
            list.Add(value);
    }

    #endregion

    #region Terminals

    private static void CreateTerminals(LBSGrammar grammar)
    {
        var terminalIds = CollectTerminalIds(grammar);

        foreach (var id in terminalIds)
        {
            var terminal = ScriptableObject.CreateInstance<GrammarTerminal>();
            terminal.id = id;
            terminal.name = $"Terminal_{id}";

            AssetDatabase.AddObjectToAsset(terminal, grammar);
            EditorUtility.SetDirty(terminal);

            grammar.LBSTerminals.Add(terminal);
        }
    }

    private static HashSet<string> CollectTerminalIds(LBSGrammar grammar)
    {
        var set = new HashSet<string>();

        foreach (var rule in grammar.LBSRules)
        {
            foreach (var expansion in rule.Expansions)
            {
                foreach (var token in expansion.sequence)
                {
                    if (!grammar.IsRule(token))
                        set.Add(token);
                }
            }
        }

        return set;
    }
    #endregion
    
    #region Terminal Fields

    private static void ApplyTerminalFields(XmlDocument xml, LBSGrammar grammar)
    {
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("g", "http://www.w3.org/2001/06/grammar");

        var nodes = xml.SelectNodes("//g:terminal", ns);

        var map = grammar.LBSTerminals.ToDictionary(t => t.id);

        foreach (XmlNode node in nodes)
        {
            string id = node.Attributes["id"]?.Value;

            if (!map.TryGetValue(id, out var terminal))
                continue;

            terminal.fields.Clear();

            foreach (XmlNode field in node.SelectNodes("g:field", ns))
                AddField(terminal, field);

            Debug.Log($"terminal{id} has {terminal.fields.Count} fields ");
        }
    }


    private static void AddField(GrammarTerminal terminal, XmlNode node)
    {
        string type = node.Attributes["type"]?.Value;
        string name = node.Attributes["name"]?.Value;

        try
        {
            var field = GrammarField.CreateField(type, name);
            terminal.fields.Add(field);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Field Error] {type} ({name}) → {ex.Message}");
        }
    }


    private static void RemoveTerminalNodes(XmlDocument xml)
    {
        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("g", "http://www.w3.org/2001/06/grammar");

        var nodes = xml.SelectNodes("//g:terminal", ns);

        foreach (XmlNode node in nodes)
            node.ParentNode.RemoveChild(node);
    }

    #endregion
}