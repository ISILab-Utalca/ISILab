using ISILab.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.MapTools.Generators;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestVisualTree : MonoBehaviour
    {
        #region FIELDS
        [SerializeField]
        private GameObject trackerGo; // Reference to the GameObject holding QuestTracker

        [SerializeField, HideInInspector]
        private QuestTracker tracker; // Reference to the QuestTracker component

        private UIDocument _questVisualTree; // UI document for the quest tree
        private TreeView _questTree; // TreeView UI element for displaying quests
        #endregion

        #region PROPERTIES
        public GameObject Go
        {
            get => trackerGo;
            set => trackerGo = value;
        }

        public QuestTracker Tracker
        {
            get
            {
                return tracker ??= trackerGo != null ? trackerGo.GetComponent<QuestTracker>() : null;
            }
        }
        #endregion

        #region METHODS
        private void Start()
        {
            InitializeUI();
            SubscribeToTracker();
            UpdateQuest();
        }

        private void InitializeUI()
        {
            // Retrieves the UIDocument and TreeView for quest display
            _questVisualTree = GetComponentInParent<UIDocument>();
            if (_questVisualTree == null)
            {
                Debug.LogWarning("No UIDocument found in parent.");
                return;
            }

            _questTree = _questVisualTree.rootVisualElement.Q<TreeView>("QuestTree");
            if (_questTree == null)
            {
                Debug.LogWarning("No TreeView named 'QuestTree' found in UI.");
                return;
            }

            ConfigureTreeView();
        }

        private void SubscribeToTracker()
        {
            if (trackerGo == null) return;

            if (Tracker != null)
            {
                Tracker.OnQuestAdvance += UpdateQuest;
            }
            else
            {
                Debug.LogWarning("QuestTracker component not found on trackerGO.");
            }
        }

        private void ConfigureTreeView()
        {
            // Configures how TreeView items are created and bound
            // Note: VisualElementQuest is assumed to be a custom VisualElement for quests
            _questTree.makeItem = () => new VisualElementQuest();
            _questTree.bindItem = (element, index) =>
            {
                if (element is VisualElementQuest questEntryVe)
                {
                    var item = _questTree.GetItemDataForIndex<QuestTrigger>(index);
                    questEntryVe.SetTrigger(item);
                }
            };
        }

        private void UpdateQuest()
        {
            if (tracker?.Triggers == null)
            {
                Debug.LogWarning("Tracker or Objectives is null.");
                return;
            }

            var rootItems = BuildTreeItems();
            _questTree.SetRootItems(rootItems);
            _questTree.Rebuild();
            _questTree.ExpandAll();
        }

        private List<TreeViewItemData<QuestTrigger>> BuildTreeItems()
        {
            var rootItems = new List<TreeViewItemData<QuestTrigger>>();

            // update by active triggers no need to see ahead
            List<QuestTrigger> activeRoots = tracker.ActiveTriggers;

            if (Application.isPlaying)
            {
                _questVisualTree.rootVisualElement.style.display =
                    activeRoots.Count != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            // for debugging
            else
            {
                _questVisualTree.rootVisualElement.style.display = DisplayStyle.Flex;
            }

            foreach (var trigger in activeRoots)
            {
                if (trigger == null) continue;
                rootItems.Add(BuildTreeRecursive(trigger));
            }

            return rootItems;
        }

        private TreeViewItemData<QuestTrigger> BuildTreeRecursive(QuestTrigger trigger)
        {
            var children = new List<TreeViewItemData<QuestTrigger>>();

            if (trigger.Next != null && trigger.Next.Count > 0)
            {
                // Maps branches to their requirement nodes for Case 2 grouping
                var branchGroups = new Dictionary<QuestTrigger, List<QuestTrigger>>();

                foreach (var nextTrigger in trigger.Next)
                {
                    if (nextTrigger == null) continue;

                    // --- CASE 1: Active trigger directly connects to a branch ---
                    if (nextTrigger is QuestTriggerBranch)
                    {
                        if (nextTrigger.Next != null)
                        {
                            foreach (var branchNext in nextTrigger.Next)
                            {
                                if (branchNext != null) children.Add(BuildTreeRecursive(branchNext));
                            }
                        }
                    }
                    else
                    {
                        // Check if this node's downstream neighbor is a branch
                        QuestTrigger downstreamBranch = nextTrigger.Next?.Find(n => n is QuestTriggerBranch);

                        // --- CASE 2: Next node is a requirement feeding a branch ---
                        if (downstreamBranch != null)
                        {
                            if (!branchGroups.ContainsKey(downstreamBranch))
                            {
                                branchGroups[downstreamBranch] = new List<QuestTrigger>();
                            }
                            branchGroups[downstreamBranch].Add(nextTrigger);
                        }
                        // --- CASE 3: Standard flat display ---
                        else
                        {
                            children.Add(BuildTreeRecursive(nextTrigger));
                        }
                    }
                }

                // Process Case 2: Invert the hierarchy so the branch contains the requirements
                foreach (var group in branchGroups)
                {
                    QuestTrigger branch = group.Key;
                    List<QuestTrigger> requirements = group.Value;

                    var requirementNodes = new List<TreeViewItemData<QuestTrigger>>();
                    foreach (var req in requirements)
                    {
                        requirementNodes.Add(new TreeViewItemData<QuestTrigger>(
                            req.gameObject.GetInstanceID() ^ branch.gameObject.GetInstanceID(),
                            req,
                            new List<TreeViewItemData<QuestTrigger>>()
                        ));
                    }

                    // Add the branch as the direct child, holding its prerequisites
                    children.Add(new TreeViewItemData<QuestTrigger>(
                        branch.gameObject.GetInstanceID(),
                        branch,
                        requirementNodes
                    ));
                }
            }

            return new TreeViewItemData<QuestTrigger>(
                trigger.gameObject.GetInstanceID(),
                trigger,
                children
            );
        }
        #endregion

#if UNITY_EDITOR
        public void PreviewLayoutInEditor()
        {
            // 1. Force find elements if they haven't been cached yet
            if (_questVisualTree == null)
            {
                _questVisualTree = GetComponentInParent<UIDocument>();
            }

            if (_questVisualTree != null && _questTree == null)
            {
                _questTree = _questVisualTree.rootVisualElement?.Q<TreeView>("QuestTree");
                if (_questTree != null) 
                    ConfigureTreeView();
            }

            if (Tracker == null)
            {
                return;
            }

            if (_questTree != null)
            {
                // build as items as if in runtime
                var rootItems = BuildTreeItems();

                // no active triggers -> use all triggers
                if (rootItems.Count == 0 && tracker.Triggers != null)
                {
                    foreach (var trigger in tracker.Triggers)
                    {
                        if (trigger is QuestTriggerNode qtn && qtn.NodeType == GraphNodeType.Start)
                        {
                            rootItems.Add(BuildTreeRecursive(qtn));
                        }
                    }
                }

                _questTree.SetRootItems(rootItems);
                _questTree.Rebuild();
                _questTree.ExpandAll();
                Debug.Log("Quest Tree Preview Generated Successfully!");
            }
            else
            {
                Debug.LogWarning("Cannot preview layout: Make sure UIDocument, TreeView, and TrackerGo are fully assigned.");
            }
        }
#endif
    }


    [CustomEditor(typeof(QuestVisualTree))]
    public class QuestVisualTreeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f); 
            if (GUILayout.Button("Preview Starting Quest Tree", GUILayout.Height(30)))
            {
                QuestVisualTree visualTreeScript = (QuestVisualTree)target;

                // Call the preview logic safely
                visualTreeScript.PreviewLayoutInEditor();
            }
            GUI.backgroundColor = Color.white; // Reset coloring
        }
    }
}