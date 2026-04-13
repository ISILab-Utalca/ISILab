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
using System.Xml.Linq;
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
        
        private VisualElement terminals;
        private const int columns = 6;


        private ObjectField grammarFileField;
        private Button processButton;

        #endregion

        private void OnEnable()
        {
            CreateInspectorGUI();
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
            terminals = root.Q<VisualElement>("Terminals");
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

        private void ClearSubAssets()
        {
            string path = AssetDatabase.GetAssetPath(grammar);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in subAssets)
            {
                if (asset == grammar) continue;
                UnityEngine.Object.DestroyImmediate(asset, true);
            }
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

                var asset = grammarFileField.value;
                ClearSubAssets();
                string path = AssetDatabase.GetAssetPath(asset);
              
                LBSGrammarReader.ReadGrammar(grammar, path);
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
                Debug.Log($"Grammar imported with {grammar.LBSRules.Count} rules and {grammar.LBSTerminals.Count} terminal actions.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Import failed: " + ex.Message);
            }
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
            rulesTree.Clear();
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
                    element.Add(OptionGrammarView(rule));
                    return;
                }

                // expansions
                if (data is GrammarExpansion expansion)
                {
                    for (int j = 0; j < expansion.sequence.Count; j++)
                    {
                        GrammarElement resolved = grammar.GetGrammarObject(expansion.sequence[j]) as GrammarElement;
                        if (resolved == null) continue;

                        element.Add(OptionGrammarView(resolved));

                        if (j < expansion.sequence.Count - 1)
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
            if (terminals== null) return;
            if (grammar.LBSTerminals == null || grammar.LBSTerminals.Count == 0) return;

            terminals.Clear();
            int column = columns;
            VisualElement ActiveRow = null;
            foreach(var terminal in grammar.LBSTerminals)
            {
                if (terminal == null) continue;


                column++;
                if (column >= columns)
                {
                    ActiveRow = new VisualElement();
                    ActiveRow.style.flexDirection = FlexDirection.Row;
                    column = 0;
                    terminals.Add(ActiveRow);
                }

              
                ActiveRow.Add(OptionGrammarView(terminal));

            }
        }

        private OptionView OptionGrammarView(GrammarElement Element)
        {
            OptionView option = new OptionView();
            option.style.display = DisplayStyle.Flex;
            option.Target = Element;
            option.Label = Element.id;
            option.Icon = Element.GetIcon();

            if (Element is GrammarTerminal) 
            { 
                option.tooltip = "Terminal: Can be placed in Graph"; 
            }
            if (Element is GrammarRule)
            { 
                option.tooltip = "Rule: Represents valid Graph Placements";
                option.OnSelect = OnRuleClicked;
                option.RegisterCallback<ClickEvent>(evt => option.OnSelect?.Invoke(option.Target));
            }
            return option;
        }
    }
}
