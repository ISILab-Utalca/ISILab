using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Editor.UI.CustomComponents;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleManagerWindow;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.TagManager
{
    public class TagManagerWindow : ThemeableWindow
    {
        public static TagManagerWindow Instance { get; private set; }

        #region FIELDS
        private static List<ScriptableObject> allTagGroups = new();
        private static List<ScriptableObject> allTags = new();
        public static List<ScriptableObject> _orphanTags = new();

        //VISUAL ELEMENTS
        private VisualElement tagGroupsContainer;
        private LBSTagListGroup groupTagsGroup;
        private List<LBSTagListGroup> groupedTagsContainerList = new();

        private VisualElement otherTagsContainer;
        private LBSTagListGroup orphanTagsGroup;

        #endregion

        #region PROPERTIES
        public static List<ScriptableObject> AllTags => allTags;
        public static List<ScriptableObject> AllTagGroups => allTagGroups;

        #endregion

        #region EVENTS
        public static Action OnClosed;
        public static Action<LBSTagGroup> OnTagGroupSelected;
        public static Action<LBSTagListGroup> OnRemovableGroupRemoved;
        public static Action<LBSTagGroup, LBSTag> OnTagAdded;
        public static Action<LBSTagGroup> OnTagGroupAdded;
        public static Action<LBSTag> OnTagOrphaned;
        public static Action<LBSTag> OnTagUnorphaned;
        #endregion
               

        //Singleton part
        private void OnEnable()
        {
            OnTagGroupSelected += SelectTagGroup;
            OnRemovableGroupRemoved += CleanElement;
            OnTagAdded += AssociateTag;
            OnTagGroupAdded += AssociateGroup;
            OnTagOrphaned += AddOrphanTag;
            OnTagUnorphaned += RemoveOrphanTag;
            Instance = this;
        }

        private void CreateGUI()
        {
            //Basic setup: Clone tree, find tags, generate main tag group container
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TagManagerWindow");
            visualTree.CloneTree(rootVisualElement);

            FindAllTags();

            //Generate the group with all the groups... The group group? I guess
            tagGroupsContainer = rootVisualElement.Q<VisualElement>("TagGroupsVE");
            if (allTagGroups.Count > 0)
            {
                groupTagsGroup = GenerateTagGroups(allTagGroups, "Tag Groups", false, new List<int> { 1 });
                tagGroupsContainer.Add(groupTagsGroup);
            }

            //Generate special tags
            otherTagsContainer = rootVisualElement.Q<VisualElement>("OtherTagsVE");
            if (_orphanTags.Count > 0) { 
                orphanTagsGroup = GenerateTagGroups(_orphanTags, "Orphan Tags", false, new List<int> { 2 });
                otherTagsContainer.Add(orphanTagsGroup);
            }
        }

        private void OnDisable()
        {
            OnClosed?.Invoke();
            OnRemovableGroupRemoved -= CleanElement;
            OnTagGroupSelected -= SelectTagGroup;
            OnTagAdded -= AssociateTag;
            OnTagGroupAdded -= AssociateGroup;
            OnTagOrphaned -= AddOrphanTag;
            OnTagUnorphaned -= RemoveOrphanTag;

            Instance = null;
        }

        #region METHODS
        
        public void FindAllTags()
        {
            //Reset first
            allTags.Clear();
            allTagGroups.Clear();
            _orphanTags.Clear();

            allTags = LBSAssetsStorage.Instance.Get<LBSTag>().ToList<ScriptableObject>();
            allTagGroups = LBSAssetsStorage.Instance.Get<LBSTagGroup>().ToList<ScriptableObject>();

            // Normal bundles
            foreach (LBSTag tag in allTags)
            {
                if(allTagGroups.Find(c => (c as LBSTagGroup).Tags.Contains(tag))==null)
                {
                    _orphanTags.Add(tag);
                }
            }
            Debug.Log(_orphanTags.Count + " orphan tags found");
        }

        public LBSTagListGroup GenerateTagGroups(List<ScriptableObject> groupList, string name = "Tags", bool removable = false, List<int> buttons = null)
        {
            var tagGroups = new LBSTagListGroup();
            tagGroups.TagListName = name;
            tagGroups.isRemovable = removable;
            tagGroups.ListInitialize(groupList);

            if ((buttons!=null)&&(buttons?.Count!=0)) { 
                tagGroups.BindAddButtons(buttons);
            }
            return tagGroups;
        }

        public static void CreateNewTagGroup()
        {
            Debug.Log("Creating new tag (debug)");
            
            var createTag = new LBSTagListCreateTag();
            createTag.Type = LBSTagListCreateTag.objectType.Group;

            createTag.ShowWindow();
        }

        public static void CreateNewTag(LBSTagListGroup group)
        {
            Debug.Log("Creating new tag (debug)");
            Debug.Log("associated tag: " + (group.AssociatedTag != null ? group.AssociatedTag.name : "none"));

            var createTag = new LBSTagListCreateTag();
            createTag.Type = LBSTagListCreateTag.objectType.Individual;
            createTag.TargetVisualGroup = group;
            if (group.AssociatedTag != null)
            {
                if (group.AssociatedTag.GetType() == typeof(LBSTagGroup))
                {
                    createTag.TargetTagGroup = group.AssociatedTag as LBSTagGroup;
                }
            }
            createTag.ShowWindow();
        }

        public void SelectTagGroup(LBSTagGroup group)
        {
            if (group == null) return;
            //Check if group is in the container (so, displayed)
            var findGroup = groupedTagsContainerList.Find(c => c.TagListName.Equals(group.name));

            //If it isn't, add it to the container.
            if(findGroup == null)
            {
                //Convert to scriptable objects
                List<ScriptableObject> scriptTagList = new();
                scriptTagList.AddRange(group.Tags);
                var newGroup = GenerateTagGroups(scriptTagList, group.name, true);
                newGroup.AssociatedTag = group;
                newGroup.BindAddButtons(new List<int> { 2, 3 });
                
                groupedTagsContainerList.Add(newGroup);
                tagGroupsContainer.Add(newGroup);
            } else
            {
                //If it isn't, remove from both lists.
                groupedTagsContainerList.Remove(findGroup);
                tagGroupsContainer.Remove(findGroup);
            }
        }

        public void AssociateTag(LBSTagGroup group, LBSTag tag)
        {
            allTags.Add(tag);
            //This means it's orphan
            if (group == null)
            {
                orphanTagsGroup.OnTagCreated?.Invoke(tag);
            }
            else
            {
                var groupVisual = FindVisualForGroup(group);
                if (groupVisual == null) return;
                else
                {
                    groupVisual.OnTagCreated?.Invoke(tag);
                }
            }
        }

        public void AssociateGroup(LBSTagGroup group)
        {
            allTagGroups.Add(group);
            groupTagsGroup.OnTagCreated?.Invoke(group);
        }

        public void AddOrphanTag(LBSTag toAdd)
        {
            _orphanTags.Add(toAdd);
            orphanTagsGroup.OnTagCreated?.Invoke(toAdd);
            foreach(LBSTagListGroup group in groupedTagsContainerList)
            {
                group.RebindButtons();
            }
        }
        public void RemoveOrphanTag(LBSTag toRemove)
        {
            _orphanTags.Remove(toRemove);
            orphanTagsGroup.OnTagRemoved?.Invoke(toRemove);
            foreach (LBSTagListGroup group in groupedTagsContainerList)
            {
                group.RebindButtons();
            }
        }

        public LBSTagListGroup FindVisualForGroup(LBSTagGroup lookingFor)
        {
            return groupedTagsContainerList.Find(c => c.AssociatedTag == lookingFor);
        }

        public void CleanElement(LBSTagListGroup group)
        {
            var findGroup = groupedTagsContainerList.Find(c => c.Equals(group));
            if(findGroup!=null)
            {
                groupedTagsContainerList.Remove(findGroup);
            }
        }

        [MenuItem("Window/ISILab/Tag Manager", priority = 2)]
        public static void ShowWindow()
        {
            TagManagerWindow window = GetWindow<TagManagerWindow>();
            Texture icon = AssetMacro.LoadAssetByGuid<Texture>("40d548834301ba14f96af3e1715add5f");
            window.minSize = new Vector2(340, 500); // use the Canvas Size of the uxml
            window.titleContent = new GUIContent("Tag Manager", icon);
        }
        public static void CloseWindow()
        {
            TagManagerWindow window = GetWindow<TagManagerWindow>();
            window.Close();
        }
        #endregion

    }
}

