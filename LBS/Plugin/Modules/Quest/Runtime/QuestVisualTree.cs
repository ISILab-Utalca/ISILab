using ISILab.LBS.Plugin.MapTools.Generators;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class QuestVisualTree : MonoBehaviour
    {
        #region FIELDS
        [SerializeField]
        private GameObject trackerGo; // Reference to the GameObject holding QuestTracker

        [SerializeField]
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
            
            tracker = trackerGo.GetComponent<QuestTracker>();
            if (tracker != null)
            {
                tracker.OnQuestAdvance += UpdateQuest;
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

        private List<TreeViewItemData<QuestTriggerNode>> BuildTreeItems()
        {
            // Builds the hierarchy of TreeView items for root objectives
            var rootItems = new List<TreeViewItemData<QuestTriggerNode>>();

            foreach (var trigger in tracker.Triggers)
            {
                bool FromBranch = false;
                foreach (var prev in trigger.AllPrevious)
                {
                    if(prev is QuestTriggerBranch)
                    {
                        FromBranch = true;
                        break;
                    }
                }
               
                // Only include objectives that are not part of a branch
                if (trigger is QuestTriggerNode qtn && !FromBranch)
                {
                    rootItems.Add(BuildTreeRecursive(qtn));
                }
            }

            return rootItems;
        }

        private TreeViewItemData<QuestTriggerNode> BuildTreeRecursive(QuestTriggerNode triggerNode)
        {
           
            // Recursively builds the TreeView hierarchy for objectives and sub-objectives
            // Note: Uses GetInstanceID for unique IDs to avoid conflicts in TreeView
            var children = new List<TreeViewItemData<QuestTriggerNode>>();

            if (triggerNode.Next is QuestTriggerNode qtn)
            {
                children.Add(BuildTreeRecursive(qtn));
            }

            return new TreeViewItemData<QuestTriggerNode>(
                triggerNode.gameObject.GetHashCode(),
                triggerNode,
                children
            );
        }
        #endregion
    }
}