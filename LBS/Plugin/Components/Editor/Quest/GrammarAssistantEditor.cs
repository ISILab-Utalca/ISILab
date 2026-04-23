using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using ToolBarMain = ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar.ToolBarMain;

namespace ISILab.LBS.Editor
{
    [LBSCustomEditor("GrammarAssistant", typeof(GrammarAssistant))]
    public class GrammarAssistantEditor : LBSCustomEditor, IToolProvider, IAssistantThreadedEditor
    {
        #region FIELDS
        private GrammarAssistant assistant;

        private const float ActionBorderThickness = 1f;
        private const float BackgroundOpacity = 0.25f;

        #endregion

        #region PROPERTIES
        private QuestGraph Graph => assistant.Graph;
        #endregion

        #region VIEW
        private ObjectField grammarField;

        private VisualElement nextInvalidPanel;
        private VisualElement prevInvalidPanel;
        private VisualElement expandInvalidPanel;

        private ListView nextSuggested;
        private ListView prevSuggested;
        private ListView expandSuggested;

        private Label nodeIDLabel;
        private Label paramActionLabel;
        private VisualElement actionColor;
        private VisualElement actionIcon;
        private string[] nextArray;
        private string[] prevArray;
        private List<string>[] expandArray;
        private GraphNode lastSelectedGraphNode;

        #endregion

        #region CONSTRUCTORS
        public GrammarAssistantEditor() 
        {
            
        }

        public GrammarAssistantEditor(GrammarAssistant target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }
        #endregion

        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            if (assistant != null)
            {
                assistant.OnCallAssistant = null;
            }

            //target = paramTarget;
            assistant = target as GrammarAssistant;

            //ActionExtensions.AddUnique(ref assistant.Graph.OnNodeSelected, UpdatePanel);
            assistant.OnCallAssistant = null;
            ActionExtensions.AddUnique(ref assistant.OnCallAssistant, UpdatePanel);
           // ActionExtensions.AddUnique(ref assistant.Graph.OnNodeSelected, UpdatePanel);
            grammarField.value = Graph.Grammar;
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            Clear();

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarAssistantEditor");
            visualTree.CloneTree(this);

            grammarField = this.Q<ObjectField>("Grammar");

            nextInvalidPanel = this.Q<VisualElement>("NextInvalidPanel");
            prevInvalidPanel = this.Q<VisualElement>("PrevInvalidPanel");
            expandInvalidPanel = this.Q<VisualElement>("ExpandInvalidPanel");

            nextSuggested = this.Q<ListView>("NextSuggested");
            prevSuggested = this.Q<ListView>("PrevSuggested");
            expandSuggested = this.Q<ListView>("ExpandSuggested");

            paramActionLabel = this.Q<Label>("ParamAction");
            nodeIDLabel = this.Q<Label>("ParamID");
            actionColor = this.Q<VisualElement>("ActionColor");
            actionIcon = this.Q<VisualElement>("ActionIcon");

            return this;
        }

        private void UpdatePanel(GraphNode selectedGraphNode = null)
        {
            if (selectedGraphNode == lastSelectedGraphNode) 
            {
                Debug.Log("Same node selected - return");
                return; 
            }
            if (selectedGraphNode != null && LBSMainWindow.Instance._selectedLayer != selectedGraphNode.Graph.OwnerLayer) 
            {
                Debug.Log("Different layer from node selected - return");
                return;
            }
            if (Graph is null)
            {
                Debug.Log("No graph - return");
                return;
            }

            Debug.Log($"last [{lastSelectedGraphNode}] | new [{selectedGraphNode}]");
            lastSelectedGraphNode = selectedGraphNode;
            grammarField.value = Graph.Grammar;
            paramActionLabel.text = "none";
            nodeIDLabel.text = "none";
            
            var questNode = selectedGraphNode as QuestNode;
            var selectedAction = questNode?.TerminalID;

            if (string.IsNullOrEmpty(selectedAction))
            {
                ResetPanels();
                return;
            }

            paramActionLabel.text = Graph.SelectedQuestNode.TerminalID;
            nodeIDLabel.text = Graph.SelectedQuestNode.ID;
            SetNodeVisuals();
            
            RunTask(selectedAction);
        }

        #region IAssistantThreadedEditor
        
        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ToolBarMain TaskBar { get; set; }

        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type, UnityEngine.Object loadedLevel)
        {
            // Once done, update UI safely
            EditorApplication.delayCall += () =>
            {
                UpdateNextSuggestions(nextArray, Graph.SelectedQuestNode);
                UpdatePrevSuggestions(prevArray, Graph.SelectedQuestNode);
                UpdateExpandSuggestions(expandArray, Graph.SelectedQuestNode);
                TaskBar.EnableProcess(false);
                
                LBSMainWindow.MessageNotify(new LBSLog(log, type, 5));
            };
        }

        #endregion
        
        void RunTask(string selectedAction)
        {
            var currentAssistant = assistant;
            var currentGrammar = assistant.Graph.Grammar;

            if (currentGrammar == null) return;

            ((IAssistantThreadedEditor)this).SetUpTask(this, currentAssistant);
            Task.Run(() =>
            {
                try
                {
                    nextArray = currentAssistant
                        .GetAllValidNextActionsInsert(selectedAction, progress =>
                        {
                            // progress from 0 → 0.33
                            ((IAssistantThreadedEditor)this).ReportProgress(0.33f * progress);
                        }, CancelToken)
                        ?.ToArray() ?? Array.Empty<string>();
                    
                    Thread.Sleep(1);
                     
                    prevArray = currentAssistant
                        .GetAllValidPrevActionsInsert(selectedAction, progress =>
                        {
                            // progress from 0.33 → 0.66
                            ((IAssistantThreadedEditor)this).ReportProgress(0.33f + 0.33f * progress);
                        }, CancelToken)
                        ?.ToArray() ?? Array.Empty<string>();
                    
                    Thread.Sleep(1);
                    
                    expandArray = currentAssistant
                        .GetAllExpansions(selectedAction, progress =>
                        {
                            // progress from 0.67 → 1.0
                            ((IAssistantThreadedEditor)this).ReportProgress(0.67f + 0.33f * progress);
                        }, CancelToken)
                        ?.Select(l => l?.ToList() ?? new List<string>())
                        .ToArray() ?? Array.Empty<List<string>>();

                   Thread.Sleep(1);
                   string log = "All valid grammar recommendations found.";
                   LogType logType = LogType.Log;
                   EditorApplication.delayCall += () => currentAssistant.OnTermination?.Invoke(log, logType, null);
                   
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, currentAssistant);
                }

            }, CancelToken);



        }

        #region Helpers
        
        public void SetTools(ToolKit toolkit) { }

        private void SetNodeVisuals()
        {
            if (Graph.SelectedQuestNode == null) return;

            Color nodeColor = Graph.SelectedQuestNode.Data.Terminal.color;

            var backgroundColor = nodeColor;
            backgroundColor.a = BackgroundOpacity;
            actionColor.SetBackgroundColor(backgroundColor);

            actionIcon.style.backgroundImage = new StyleBackground(Graph.SelectedQuestNode.Data.Terminal.Icon);
            actionIcon.style.unityBackgroundImageTintColor = nodeColor;
            actionColor.SetBorder(nodeColor, ActionBorderThickness);
        }
        
        private void ResetPanels()
        {
            TogglePanel(nextInvalidPanel, nextSuggested, false);
            TogglePanel(prevInvalidPanel, prevSuggested, false);
            TogglePanel(expandInvalidPanel, expandSuggested, false);
        }

        private void TogglePanel(VisualElement invalidPanel, ListView listView, bool hasData)
        {
            if (invalidPanel != null)
                invalidPanel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
            if (listView != null)
                listView.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Action<VisualElement, int> SafeBind(Action<VisualElement, int> binder)
        {
            return (element, index) =>
            {
                try { binder(element, index); }
                catch (Exception ex)
                {
                    Debug.LogError($"Bind failed at index {index}: {ex}");
                }
            };
        }

        private void UpdateSuggestionList(
            string[] data,
            VisualElement invalidPanel,
            ListView listView,
            Func<string, Action> actionFactory)
        {
            if (invalidPanel != null && data != null)
                invalidPanel.style.display = data.Any() ? DisplayStyle.None : DisplayStyle.Flex;

            if (listView == null) return;

            listView.style.display = data.Any() ? DisplayStyle.Flex : DisplayStyle.None;
            listView.itemsSource = data;
            listView.makeItem = () => new SuggestionActionButton();
            listView.bindItem = SafeBind((element, index) =>
            {
                if (element is not SuggestionActionButton button) return;
                if (index < 0 || index >= data.Length) return;

                string action = data[index];
                button.SetAction(action, actionFactory(action));
            });
            listView.Rebuild();
        }
        #endregion

        #region Suggestion Updates
        private void UpdateNextSuggestions(string[] nextArray, QuestNode currentQuest)
        {
            UpdateSuggestionList(nextArray, nextInvalidPanel, nextSuggested,
                action => assistant.InsertNextAction(action, currentQuest));
        }

        private void UpdatePrevSuggestions(string[] prevArray, QuestNode currentQuest)
        {
            UpdateSuggestionList(prevArray, prevInvalidPanel, prevSuggested,
                action => assistant.InsertPreviousAction(action, currentQuest));
        }

        private void UpdateExpandSuggestions(List<string>[] expandArray, QuestNode currentQuest)
        {
            if (expandInvalidPanel != null && expandArray != null)
                expandInvalidPanel.style.display = expandArray.Any() ? DisplayStyle.None : DisplayStyle.Flex;

            if (expandSuggested == null && expandArray.Length == 0) return;

            expandSuggested.style.display = expandArray.Any() ? DisplayStyle.Flex : DisplayStyle.None;
            expandSuggested.itemsSource = expandArray;
            expandSuggested.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            expandSuggested.makeItem = MakeExpandFoldout;

            expandSuggested.bindItem = SafeBind((visualElement, index) =>
            {
                if (visualElement is not LBSCustomFoldout foldout) return;
                if (index < 0 || index >= expandArray.Length) return;

                foldout.contentContainer.Clear();
                foldout.text = $"Expansion {index + 1}";

                List<string> actions = expandArray[index] ?? new List<string>();

                // Header
                var header = new ExpansionHeader();
                header.ButtonConvert.SetAction(currentQuest.TerminalID, assistant.ExpandAction(actions, currentQuest));
                foldout.contentContainer.Add(header);

                // Entries
                for (int j = 0; j < actions.Count; j++)
                {
                    var type =  j == 0 ? QuestNode.ENodeType.Start :
                                j == actions.Count - 1 ? QuestNode.ENodeType.Goal :
                                QuestNode.ENodeType.Middle;

                    var entry = new ActionExpandEntry
                    {
                        style = { flexGrow = 1, flexShrink = 0 }
                    };
                    entry.SetEntryAction(actions[j], type, actions.Count == 1);
                    foldout.contentContainer.Add(entry);
                }
            });

            expandSuggested.Rebuild();
        }
        #endregion

        #region Actions
        private LBSCustomFoldout MakeExpandFoldout()
        {
            var foldout = new LBSCustomFoldout();
            foldout.contentContainer.style.flexDirection = FlexDirection.Column;
            foldout.style.flexGrow = 1;
            foldout.style.flexShrink = 0;

            foldout.RegisterValueChangedCallback(evt =>
            {
                foldout.contentContainer.style.flexShrink = evt.newValue ? 0 : 1;
                try { expandSuggested.RefreshItems(); }
                catch
                {
                    Debug.LogError("Assistant failed to refresh the expand foldout");
                }
            });

            return foldout;
        }
        
        #endregion

        public override void OnUnfocus()
        {
            LBSMainWindow.Instance.rootVisualElement.Q<ToolBarMain>().CancelProgress();
        }

        #endregion
    }
}
