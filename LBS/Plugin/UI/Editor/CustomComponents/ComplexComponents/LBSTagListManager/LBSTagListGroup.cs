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
using UnityEditor.UIElements;
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

        //Only important when it comes from a group
        private ScriptableObject associatedTag = null;

        //Sort button exclusive to the tag list group
        private List<int> buttons;
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
        public ScriptableObject AssociatedTag
        {
            get => associatedTag;
            set => associatedTag = value;
        }
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
        public List<object> TagList
        {
            get => tagList;
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

        public ListView TagListView => listView;
        #endregion

        #region EVENTS
        public Action<object> OnTagRemoved;
        public Action<object> OnTagCreated;
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
                objectEntryVE.IsRemovable = true;

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
                Selection.activeObject = tagChosen as UnityEngine.Object;

            };

            listView.makeNoneElement = () => new VisualElement();
            listView.itemsSource = tagList;

            OnTagCreated += (obj) =>
            {
                tagList.Add(obj);
                listView.Rebuild();
            };

            OnTagRemoved += (obj) =>
            {
                if(tagList.Contains(obj))
                {
                    tagList.Remove(obj);
                    listView.Rebuild();
                }
            };
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

        private void ToggleSort()
        {
            switch(currentSort)
            {
                case SortType.Disabled: SetSort(SortType.Ascending); break;
                case SortType.Ascending: SetSort(SortType.Descending); break;
                case SortType.Descending: SetSort(SortType.Disabled); break;
            }
        }

        private void SetSort(SortType type)
        {
            switch (type)
            {
                case SortType.Disabled:

                    toggleSortButton.ToggleIcon = sortAscending;
                    toggleSortButton.SetValueWithoutNotify(false);

                    listView.itemsSource = tagList;
                    currentSort = SortType.Disabled;

                break;

                case SortType.Ascending:

                    toggleSortButton.SetValueWithoutNotify(true);
                    
                    var ascendingSortedList = new List<object>(tagList);
                    ascendingSortedList.Sort((x, y) => (x as ScriptableObject).name.CompareTo((y as ScriptableObject).name));
             
                    listView.itemsSource = ascendingSortedList.ToArray();
                    currentSort = SortType.Ascending;

                break;

                case SortType.Descending:

                    toggleSortButton.ToggleIcon = sortDescending;
                    toggleSortButton.SetValueWithoutNotify(true);

                    var descendingSortedList = new List<object>(tagList);
                    descendingSortedList.Sort((x, y) => (y as ScriptableObject).name.CompareTo((x as ScriptableObject).name));
  
                    listView.itemsSource = descendingSortedList.ToArray();
                    currentSort = SortType.Descending;

                break;
                
            }

        }

        public void BindAddButton(int option)
        => BindAddButtons(new List<int> { option });
        
        public void BindAddButtons(List<int> options)
        {
            buttons = options;
            addButton.clickable = new Clickable(() => { });

            GenericMenu menu = new GenericMenu();
            //Since most of these have different functionalities, I figured I'd just make a function that lets you switch which one you wanted to use.
            //Just send the list of options and they'll be automatically added!
            
            foreach(int option in options) { 
                switch (option)
                {
                    //Make a new Tag Group
                    case 1:
                            menu.AddItem(new GUIContent("New Tag Group"), false, TagManagerWindow.CreateNewTagGroup);
                            SetSort(currentSort);
                        break;
                    //Make a new Tag, then add to the current group.
                    case 2:
                        
                            menu.AddItem(new GUIContent("New Tag"), false, () => {
                                //make the new tag
                                TagManagerWindow.CreateNewTag(this);
                                //add the tag to the current VISUAL group regardless
                                //Refresh sort
                                SetSort(currentSort);
                            });
                        
                        break;
                    //Add an orphan tag to the group (if it has one)
                    case 3:
                        //menu.AddItem(new GUIContent("Add Tag"), false, )
                        if((AssociatedTag!=null)&&(AssociatedTag.GetType() == typeof(LBSTagGroup))) {
                            //Add EVERY Orphan tag lol
                            foreach (LBSTag orphanTag in TagManagerWindow._orphanTags)
                            {
                                menu.AddItem(new GUIContent("Add Tag/" + orphanTag.name), false, () =>
                                {
                                    var group = AssociatedTag as LBSTagGroup;
                                    group.Add(orphanTag);
                                    OnTagCreated?.Invoke(orphanTag);
                                    TagManagerWindow.OnTagUnorphaned?.Invoke(orphanTag);
                                    SetSort(currentSort);
                                });
                            }
                        }
                        
                        break;
                }
            }

            addButton.clicked += () =>
            {
                menu.ShowAsContext();
            };
        }

        public void RebindButtons()
        {
            addButton.Unbind();
            if ((buttons == null)||(buttons.Count==0)) return;
            BindAddButtons(buttons);
        }

        #endregion








    }
}
