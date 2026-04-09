using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

namespace ISILab.LBS.VisualElements
{
    [CustomEditor(typeof(LBSGrammar))]
    public class LBSGrammarEditor : UnityEditor.Editor
    {
        #region FIELDS
        private LBSGrammar grammar;

        private TreeView rulesTree;
        private ListView terminalList;
        private ObjectField grammarFileField;
        private Button processButton;

        #endregion

        private void OnEnable()
        {
            Reload();
        }

        private void Reload()
        {
            BindObjectField();
            BindButton();
            BindRules();
            BindTerminals();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSGrammarDisplay");
            visualTree.CloneTree(root);

            grammar = (LBSGrammar)target;

            rulesTree = root.Q<TreeView>("RulesTree");
            terminalList = root.Q<ListView>("TerminalList");
            grammarFileField = root.Q<ObjectField>("GrammarFileField");
            processButton = root.Q<Button>("ProcessButton");

            Reload();

            return root;
        }

        private void BindButton()
        {
            if(processButton == null) return;

            processButton.clicked += ProcessGrammarFile;
        }

        private void ProcessGrammarFile()
        {
            try
            {
                if (grammarFileField.value == null)
                {
                    Debug.LogError("No grammar file selected.");
                    return;
                }

                grammar.Grammar = null;
                var asset = grammarFileField.value;

                string assetPath = AssetDatabase.GetAssetPath(asset);
                var structure = LBSGrammarReader.ReadGrammar(assetPath);


                structure.LBSTerminals = SaveTerminalAssets(
                    grammar,
                    structure.LBSTerminals
                );

                grammar.Grammar = structure;
                grammar.PathGUID = LBSAssetMacro.GetGuidFromAsset(asset);

                UnityEditor.EditorUtility.SetDirty(grammar);
                UnityEditor.AssetDatabase.SaveAssets();

                // Force inspector redraw
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    Reload();
                };
                
                LBSLog log = new LBSLog("New LBS Grammar Generated Successfully.");
                LBSMainWindow.MessageNotify(log);
                Debug.Log($"Grammar imported with {structure.LBSRules.Count} rules and {structure.LBSTerminals.Count} terminal actions.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Import failed: " + ex.Message);
            }
        }

        private static string GetTerminalFolderPath(UnityEngine.Object grammarAsset)
        {
            string grammarPath = AssetDatabase.GetAssetPath(grammarAsset);
            string folder = System.IO.Path.GetDirectoryName(grammarPath);
            string terminalFolder = System.IO.Path.Combine(folder, "Terminals");

            if (!AssetDatabase.IsValidFolder(terminalFolder))
            {
                AssetDatabase.CreateFolder(folder, "Terminals");
            }

            return terminalFolder;
        }

        private static List<GrammarTerminal> SaveTerminalAssets(
           UnityEngine.Object grammarAsset,
           List<GrammarTerminal> parsedTerminals)
        {
            string folder = GetTerminalFolderPath(grammarAsset);

            var result = new List<GrammarTerminal>();

            foreach (var terminal in parsedTerminals)
            {
                string assetPath = $"{folder}/{terminal.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<GrammarTerminal>(assetPath);

                if (existing == null)
                {
     
                    var newTerminal = ScriptableObject.CreateInstance<GrammarTerminal>();
                    newTerminal.id = terminal.id;
                    newTerminal.fields = terminal.fields;

                    AssetDatabase.CreateAsset(newTerminal, assetPath);
                    result.Add(newTerminal);
                }
                else
                {
                    existing.fields = terminal.fields;

                    EditorUtility.SetDirty(existing);
                    result.Add(existing);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return result;
        }

        private void BindObjectField()
        {
            if (grammarFileField == null) return;

            if (grammar.PathGUID != null)
            {
                grammarFileField.value = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(grammar.PathGUID));
            }

            grammarFileField.objectType = typeof(TextAsset);
            grammarFileField.allowSceneObjects = false;

            grammarFileField.RegisterValueChangedCallback(evt =>
            {
                var asset = evt.newValue as TextAsset;

                if (asset == null) return;

                string path = AssetDatabase.GetAssetPath(asset);

                if (!path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("Only .xml grammar files are allowed.");
                    grammarFileField.value = null;
                }
            });
        }
        private void BindRules()
        {
            if (rulesTree == null) return;
            if (grammar.LBSRules == null || grammar.LBSRules.Count == 0) return;

            var items = new List<TreeViewItemData<object>>();
            int id = 0;

            foreach (var rule in grammar.LBSRules)
            {
                if (rule == null) continue;
                var expansionItems = new List<TreeViewItemData<object>>();

                foreach (var expansion in rule.Expansions)
                {
                    expansionItems.Add(new TreeViewItemData<object>(
                        id++,
                        expansion 
                    ));
                }

                items.Add(new TreeViewItemData<object>(
                    id++,
                    rule,
                    expansionItems
                ));
            }

            rulesTree.SetRootItems(items);

            rulesTree.makeItem = () => new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignContent = Align.Center,
                    alignSelf = Align.Stretch
                }
            };

            rulesTree.bindItem = (element, i) =>
            {
                element.Clear();

                var data = rulesTree.GetItemDataForIndex<object>(i);

                // top most rule
                if (data is GrammarRule rule)
                {
                    OptionView option = new OptionView();
                    option.Target = rule;
                    option.Label = rule.id;
                    option.Icon = rule.GetIcon();
                    element.Add(option);
                    return;
                }

                // expansions
                if (data is List<string> expansion)
                {
                    for (int t = 0; t < expansion.Count; t++)
                    {
                        GrammarElement resolved = grammar.GetGrammarObject(expansion[t]) as GrammarElement;
                        if (resolved == null) continue;

                        OptionView option = new OptionView();
                        option.Target = resolved;
                        option.Label = resolved.id;
                        option.Icon = resolved.GetIcon();

                        option.OnSelect = OnRuleClicked;
                        option.RegisterCallback<ClickEvent>(evt => option.OnSelect?.Invoke(option.Target));

                        element.Add(option);

                        if (t < expansion.Count - 1)
                            element.Add(new Label(" → "));
                    }
                }
            };
            rulesTree.Rebuild();

        }

        private void OnRuleClicked(object obj)
        {
            if (obj is not GrammarRule rule) return;

            int index = FindRuleIndex(rule);
            if (index < 0) return;

            rulesTree.CollapseAll();

            rulesTree.SetSelection(new[] { index });
            rulesTree.ScrollToItem(index);

            VisualElement container = rulesTree.GetRootElementForIndex(index);

            if (container != null)
            {
                OptionView view = container.Q<OptionView>();
                view?.SetSelected(true);
                Debug.Log("Focusing on:" + (view.Target as GrammarElement).id);
            }
        }

        private int FindRuleIndex(GrammarRule targetRule)
        {
            for (int i = 0; i < rulesTree.viewController.itemsSource.Count; i++)
            {
                var data = rulesTree.GetItemDataForIndex<object>(i);
                if (data is GrammarRule rule && rule.id == targetRule.id)
                {
                    return i;
                }
            }
            return -1;
        }

        private void BindTerminals()
        {
            if(terminalList==null) return;
            if (grammar.LBSTerminals == null) return;
            if (grammar.LBSTerminals.Count == 0) return;

            terminalList.itemsSource = grammar.LBSTerminals;

            terminalList.makeItem = () => new OptionView();

            terminalList.bindItem = (element, i) =>
            {
                if (element == null || grammar.LBSTerminals.Count==0) return;
                var terminal = grammar.LBSTerminals[i];
                var option = element as OptionView;
                if (option == null) return;

                option.Target = terminal;
                option.Label = terminal.id;
                option.Icon = terminal.GetIcon();
            };

            terminalList.Rebuild();
        }

  
    }
}
