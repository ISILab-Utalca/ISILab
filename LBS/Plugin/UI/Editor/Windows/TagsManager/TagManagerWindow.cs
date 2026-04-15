using System;
using System.Collections.Generic;
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

namespace ISILab.LBS.Plugin.UI.Editor.Windows.TagManager
{
    public class TagManagerWindow : ThemeableWindow
    {
        /*public class TagCategory 
        {
            private readonly List<LBSTag> _tags = new();
            private LBSTagListGroup group;

            private string _name;

            public List<LBSTag> Tags => _tags;
            public ListView List => group.TagList;
            public string ListName { get => _name; } // Visual element name. Not to be confused with list title.

            public TagCategory(string listName, List<LBSTag> tags, bool removable = false)
            {
                _name = listName;
                _tags = tags;
                group = new LBSTagListGroup(removable, )
            }
        }*/

        public static TagManagerWindow Instance { get; private set; }

        #region FIELDS
        private List<LBSTagGroup> allTagGroups = new();
        private List<LBSTag> allTags = new();
        // private LBSTagListGroup orphanTags = new();

        //VISUAL ELEMENTS
        private VisualElement tagGroupsContainer;
        //private LBSTagListGroup tagGroups = new();

        private VisualElement otherTagsContainer;
        #endregion

        #region EVENTS
        public static Action OnClosed;
        #endregion
               

        //Singleton part
        private void OnEnable()
        {
            Instance = this;
        }

        private void CreateGUI()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TagManagerWindow");
            visualTree.CloneTree(rootVisualElement);

            FindAllTags();
            tagGroupsContainer = rootVisualElement.Q<VisualElement>("TagGroupsVE");

            if(allTagGroups.Count > 0) GenerateTagGroups(allTagGroups);
        }

        private void OnDisable()
        {
            OnClosed?.Invoke();
            Instance = null;
        }

        #region METHODS
        
        public void FindAllTags()
        {
            //Reset first
            allTags.Clear();
            allTagGroups.Clear();

            allTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            allTagGroups = LBSAssetsStorage.Instance.Get<LBSTagGroup>();

            Debug.Log("all tags loaded: " + allTags.Count);
            Debug.Log("all tag groups loaded: " + allTagGroups.Count);
        }

        public void GenerateTagGroups(List<LBSTagGroup> groupList)
        {
            var tagGroups = new LBSTagListGroup();
            tagGroups.TagListName = "Tag Groups";
            tagGroups.isRemovable = false;
            tagGroups.ListInitialize(groupList);

            tagGroupsContainer.Add(tagGroups);

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

