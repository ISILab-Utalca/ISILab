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

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleManagerWindow;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.TagManager
{
    /// <summary>
    /// The <b>Tag Manager</b> Window can be accessed from the main LBS interface. It's a simple interface that immediately initializes all tags currently
    /// stored in the application and allows for easy manipulation of them.
    /// </summary>
    public class TagManagerWindow : ThemeableWindow
    {
        /// <summary>
        /// References the currently instatiated window to ensure no memory leaks.
        /// </summary>
        public static TagManagerWindow Instance { get; private set; }

        #region FIELDS
        private static List<ScriptableObject> allTagGroups = new();
        private static List<ScriptableObject> allTags = new();
        /// <summary>
        /// A list containing all orphan tags loaded in the project.
        /// </summary>
        public static List<ScriptableObject> _orphanTags = new();

        //VISUAL ELEMENTS
        private VisualElement tagGroupsContainer;
        private LBSTagListGroup groupTagsGroup;
        private List<LBSTagListGroup> groupedTagsContainerList = new();

        private VisualElement otherTagsContainer;
        private LBSTagListGroup orphanTagsGroup;

        #endregion

        #region PROPERTIES
        /// <summary>
        /// A list with every single tag loaded in the current project.
        /// </summary>
        public static List<ScriptableObject> AllTags => allTags;
        /// <summary>
        /// A list with every <b>Tag Group</b> loaded in the project.
        /// </summary>
        public static List<ScriptableObject> AllTagGroups => allTagGroups;

        #endregion

        #region EVENTS
        /// <summary>
        /// Called when the window is closed.
        /// </summary>
        public static Action OnClosed;
        /// <summary>
        /// Called when a particular <b>Tag Group</b> is selected.
        /// </summary>
        public static Action<LBSTagGroup> OnTagGroupSelected;
        /// <summary>
        /// Called when a <b>Tag Group</b> is attempted to be removed via the window.
        /// </summary>
        public static Action<LBSTagListGroup> OnRemovableGroupRemoved;
        /// <summary>
        /// Called when a tag is added for immediate initialization. A <b>Tag Group</b> can be added to the call to immediately associate the added tag to it.
        /// </summary>
        public static Action<LBSTagGroup, LBSTag> OnTagAdded;
        /// <summary>
        /// Called when a <b>Tag Group</b> is added for immediate initialization.
        /// </summary>
        public static Action<LBSTagGroup> OnTagGroupAdded;
        /// <summary>
        /// Called when a tag is removed from its current group and orphaned.
        /// </summary>
        public static Action<LBSTag> OnTagOrphaned;
        /// <summary>
        /// Called when a tag is succesfully added to a <b>Tag Group</b> to automatically remove it from its orphan group.
        /// </summary>
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

        protected override void CreateGUI()
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
        
        /// <summary>
        /// Scans the LBS Asset Storage, then initializes and stores all tags found within the tag manager window. The initialized tags are then divided
        /// between tags, <b>Tag Groups</b> and <b>orphan</b> tags (tags without a Tag Group assigned).
        /// </summary>
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

        /// <summary>
        /// Instantiates the visual element for a <b>Tag List Group</b>.
        /// </summary>
        /// <param name="groupList">A list with all the objects to be stored within the group.
        /// The list can be composed of tags, <b>Tag Groups</b> or a combination of both.</param>
        /// <param name="name">The visual element's name.</param>
        /// <param name="removable">Checks if the object is removable or not. A removable element will have a removal button enabled.</param>
        /// <param name="buttons">A list of every button to be bound to the new visual element. The button list can be checked in the "<b>BindAllButtons</b>" method
        /// in the <b>Tag List Group</b> object.</param>
        /// <returns>The generated visual element is returned, allowing the window to easily add it to a corresponding container.</returns>
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
        
        /// <summary>
        /// Creates a new <b>Create Tag Group</b> window connected to the Tag Manager to allow creation of a new <b>Tag Group</b>.
        /// </summary>
        public static void CreateNewTagGroup()
        {
            Debug.Log("Creating new tag (debug)");
            
            var createTag = new LBSTagListCreateTag();
            createTag.Type = LBSTagListCreateTag.objectType.Group;

            createTag.ShowWindow();
        }

        /// <summary>
        /// Creates a new <b>Create Tag</b> window connected to the Tag Manager to allow creating a new tag.
        /// </summary>
        /// <param name="group">The <b>Tag List Group</b> currently creating this tag. If the group is associated to a <b>Tag Group</b>, the
        /// window will immediately try to assign the new tag to it.</param>
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

        /// <summary>
        /// Attempts to select and expand a <B>Tag Group</B> after selection. The function will attempt to add the Tag Group to the Tag Manager's container if
        /// it isn't already initialized, and will attempt to remove it if it is (functioning as a toggle).
        /// </summary>
        /// <param name="group">The <b>Tag Group</b> to be expanded.</param>
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
        /// <summary>
        /// Associates a tag to a <b>Tag Group</b>, both within the asset and within the visual element.
        /// </summary>
        /// <param name="group">The <b>Tag Group</b> to add the tag to.</param>
        /// <param name="tag">The tag to be added.</param>
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

        /// <summary>
        /// Associates a <b>Tag Group</b> to the window's tag group list in case a new one is created within the window.
        /// </summary>
        /// <param name="group"></param>
        public void AssociateGroup(LBSTagGroup group)
        {
            allTagGroups.Add(group);
            groupTagsGroup.OnTagCreated?.Invoke(group);
        }

        /// <summary>
        /// Adds an orphan tag to the window's orphan tag list, then refreshes all bindings in the window.
        /// </summary>
        /// <param name="toAdd"></param>
        public void AddOrphanTag(LBSTag toAdd)
        {
            _orphanTags.Add(toAdd);
            orphanTagsGroup.OnTagCreated?.Invoke(toAdd);
            foreach(LBSTagListGroup group in groupedTagsContainerList)
            {
                group.RebindButtons();
            }
        }

        /// <summary>
        /// Removes an orphan tag from the window's orphan tag list, then refreshes all bindings in the window.
        /// </summary>
        /// <param name="toRemove"></param>
        public void RemoveOrphanTag(LBSTag toRemove)
        {
            _orphanTags.Remove(toRemove);
            orphanTagsGroup.OnTagRemoved?.Invoke(toRemove);
            foreach (LBSTagListGroup group in groupedTagsContainerList)
            {
                group.RebindButtons();
            }
        }

        /// <summary>
        /// Finds the <b>Tag List Group</b> for a particular <b>Tag Group</b>.
        /// </summary>
        /// <param name="lookingFor">The <b>Tag Group</b> being currently searched.</param>
        /// <returns></returns>
        public LBSTagListGroup FindVisualForGroup(LBSTagGroup lookingFor)
        {
            return groupedTagsContainerList.Find(c => c.AssociatedTag == lookingFor);
        }

        /// <summary>
        /// Removes a <b>Tag List Group</b> from the Tag Manager Window.
        /// </summary>
        /// <param name="group">The <b>Tag List Group</b> to remove.</param>
        public void CleanElement(LBSTagListGroup group)
        {
            var findGroup = groupedTagsContainerList.Find(c => c.Equals(group));
            if(findGroup!=null)
            {
                groupedTagsContainerList.Remove(findGroup);
            }
        }

        /// <summary>
        /// Simply opens the Tag Manager window and sets its dimensions appropriately.
        /// </summary>
        [MenuItem("Window/ISILab/Tag Manager", priority = 2)]
        public static void ShowWindow()
        {
            TagManagerWindow window = GetWindow<TagManagerWindow>();
            Texture icon = AssetMacro.LoadAssetByGuid<Texture>("40d548834301ba14f96af3e1715add5f");
            window.minSize = new Vector2(340, 500); // use the Canvas Size of the uxml
            window.titleContent = new GUIContent("Tag Manager", icon);
        }
        /// <summary>
        /// Closes the window.
        /// </summary>
        public static void CloseWindow()
        {
            TagManagerWindow window = GetWindow<TagManagerWindow>();
            window.Close();
        }
        #endregion

    }
}

