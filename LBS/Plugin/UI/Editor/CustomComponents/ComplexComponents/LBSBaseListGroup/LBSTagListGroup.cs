using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.UI.Editor.Windows.TagManager;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    [UxmlElement]
    public partial class LBSTagListGroup : LBSBaseListGroup
    {
        #region FIELDS
        //The usual
        private string tagListName;
        private bool removable = true;
        private List<object> tagList = new();

        //Sort button exclusive to the tag list group
        private VectorImage sortAscending;
        private VectorImage sortDescending;
        protected LBSToolbarToggle toggleSortButton;
        protected enum SortType { Disabled, Ascending, Descending };
        protected SortType currentSort;
        #endregion

        #region VISUAL ELEMENTS
        private LBSToolbarButton disabledRemoveButton;
        private Label titleLabel;
        private LBSToolbarButton addButton;
        #endregion

        #region PROPERTIES
        public string TagListName
        {
            get => tagListName;
            set
            {
                tagListName = value;
                if(titleLabel!=null)
                {
                    titleLabel.text = tagListName;
                }
            }
        }
        [UxmlAttribute]
        public bool isRemovable
        {
            get => removable;
            set
            {
                removable = value;
                if (removeButton != null)
                {
                    removeButton.SetEnabled(isRemovable);
                    removeButton.style.visibility = isRemovable ? Visibility.Visible : Visibility.Hidden;
                    disabledRemoveButton.style.visibility = isRemovable ? Visibility.Hidden : Visibility.Visible;
                    removeButton.style.display = isRemovable ? DisplayStyle.Flex : DisplayStyle.None;
                    disabledRemoveButton.style.display = isRemovable ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        public ListView TagList => listView;
        #endregion

        #region EVENTS
        public Action<object> OnTagRemoved;
        public Action OnSortToggle;
        public Action OnAddButton;
        #endregion

        #region CONSTRUCTOR
        public LBSTagListGroup() : base()
        {
            Init();
        }
        public LBSTagListGroup(bool removable) : base()
        {
            this.removable = removable;
            Init();
        }
        #endregion

        #region METHODS
        public void Init()
        {
            //Sort stuff!
            currentSort = SortType.Disabled;
            sortAscending = AssetMacro.LoadAssetByGuid<VectorImage>("d4a1818454021d74a958b73e1177331d");
            sortDescending = AssetMacro.LoadAssetByGuid<VectorImage>("ed112e167fd361f478992d351e0c3158");
            OnSortToggle += ToggleSort;

            //Add button!
            addButton = this.Q<LBSToolbarButton>("AddButton");
            //addButton.clicked += OnAddButton;

            //Remove button
            disabledRemoveButton = this.Q<LBSToolbarButton>("DisabledRemoveButton");
            OnListRemoved += () => {
                RemoveFromHierarchy();
                TagManagerWindow.OnRemovableGroupRemoved?.Invoke(this);
            };

            //The original list already has the expansion thingy and the toggle sort.
            toggleSortButton = this.Q<LBSToolbarToggle>("SortButton");
            toggleSortButton.RegisterCallback<ClickEvent>(_evt =>
            {
                ToggleSort();
            });

            //Title
            titleLabel = this.Q<Label>("TitleLabel");
            titleLabel.text = tagListName;

            //ListView stuff
            listView.makeItem = () => new LBSTagListObject();
            listView.fixedItemHeight = 30;
            listView.bindItem = (element, index) =>
            {
                var objectEntryVE = element as LBSTagListObject;
                objectEntryVE.Owner = this;
                //All unremovable for now!
                objectEntryVE.IsRemovable = false;

                if (objectEntryVE == null) return;
                //Debug.Log("binding " + objectEntryVE);
                
                var objectEntry = listView.itemsSource[index];
                if (objectEntry.GetType() == typeof(LBSTagGroup))
                {
                    LBSTagGroup tagEntry = objectEntry as LBSTagGroup;
                    objectEntryVE.Type = LBSTagListObject.objectType.Group;
                    objectEntryVE.Name = tagEntry.name;
                    objectEntryVE.AssociatedTag = tagEntry;
                    objectEntryVE.AddLayerTag();
                }
                else if (objectEntry.GetType() == typeof(LBSTag))
                {
                    LBSTag tagEntry = objectEntry as LBSTag;
                    objectEntryVE.Type = LBSTagListObject.objectType.Individual;
                    objectEntryVE.Name = tagEntry.label;
                    objectEntryVE.AssociatedTag = tagEntry;
                }
            };

            listView.itemsChosen += (item) =>
            {
                var tagChosen = item.First();
                if (tagChosen.GetType() == typeof(LBSTagGroup))
                {
                    TagManagerWindow.OnTagGroupSelected?.Invoke(tagChosen as LBSTagGroup);
                }
            };

            listView.makeNoneElement = () => new VisualElement();
            listView.itemsSource = tagList;
        }

        public void ListInitialize(List<ScriptableObject> initList)
        {
            tagList.Clear();
            foreach (object tag in initList)
            {
                tagList.Add(tag);
            }
        }

        public void AddToGroup(LBSTagGroup newObj, bool removable = false)
        {
            tagList.Add(newObj);
        }
        public void AddToGroup(LBSTag newObj, bool removable = false)
        {
            tagList.Add(newObj);
        }

        public void ToggleSort()
        {
            switch (currentSort)
            {
                case SortType.Disabled:

                    toggleSortButton.SetValueWithoutNotify(true);
                    
                    var ascendingSortedList = new List<object>(tagList);
                    ascendingSortedList.Sort((x, y) => (x as ScriptableObject).name.CompareTo((y as ScriptableObject).name));
             
                    listView.itemsSource = ascendingSortedList.ToArray();
                    currentSort = SortType.Ascending;

                    break;
                case SortType.Ascending:

                    toggleSortButton.ToggleIcon = sortDescending;
                    toggleSortButton.SetValueWithoutNotify(true);

                    var descendingSortedList = new List<object>(tagList);
                    descendingSortedList.Sort((x, y) => (y as ScriptableObject).name.CompareTo((x as ScriptableObject).name));
  
                    listView.itemsSource = descendingSortedList.ToArray();
                    currentSort = SortType.Descending;

                    break;
                case SortType.Descending:

                    toggleSortButton.ToggleIcon = sortAscending;
                    toggleSortButton.SetValueWithoutNotify(false);

                    listView.itemsSource = tagList;
                    currentSort = SortType.Disabled;

                    break;
            }

        }

        public void BindAddButton(int option)
        {
            addButton.clickable = new Clickable(() => { });
            //Since most of these have different functionalities, I figured I'd just make a function that lets you switch which one you wanted to use.
            switch (option)
            {
                //OPTION 1: For AllGroups. It allows you to make a new group tag.
                case 1:
                    addButton.clicked += () =>
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("New Tag Group..."), false, TagManagerWindow.CreateNewTagGroup);
                        menu.ShowAsContext();
                    };
                    break;
                //OPTION 2: For layer groups. It allows you to add an orphan tag to the group.
            }
        }

        #endregion








    }
}
