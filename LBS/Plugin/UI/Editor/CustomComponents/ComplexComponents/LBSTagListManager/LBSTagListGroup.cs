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
    /// <summary>
    /// A list visual element containing a <b>Tag Group</b> (or an abstract element containing Tag Groups within itself). It can then display all elements
    /// associated with the main objcet and allow them to be easily manipulated.
    /// </summary>
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

        /// <summary>
        /// A custom enumerator encompassing all three sorting types available for the element. Elements can be sorted by their original internal order as well
        /// as alphabetically, ascending or descending.
        /// </summary>
        protected enum SortType { Disabled, Ascending, Descending };
        /// <summary>
        /// The current sorting type currently applying to the Visual Element.
        /// </summary>
        protected SortType currentSort;

        #endregion

        #region VISUAL ELEMENTS
        private LBSToolbarButton disabledRemoveButton;
        private Label titleLabel;
        private LBSToolbarButton addButton;
        #endregion

        #region PROPERTIES
        /// <summary>
        /// References the current object associated to the Tag List. It can be either a tag or a <b>Tag Group</b>.
        /// </summary>
        public ScriptableObject AssociatedTag
        {
            get => associatedTag;
            set => associatedTag = value;
        }
        /// <summary>
        /// The name applied to the Tag List. Usually shared with the associated <b>Tag Group</b>, but can be freely personalized.
        /// </summary>
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
        /// <summary>
        /// References the object list contained within the Tag List.
        /// </summary>
        public List<object> TagList
        {
            get => tagList;
        }

        /// <summary>
        /// Enables or disables the <b>Remove</b> button in the visual element, depending on whether it's marked as removable or non-removable.
        /// </summary>
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
        /// <summary>
        /// References the List View object in which all elements within the Tag List are stored.
        /// </summary>
        public ListView TagListView => listView;
        #endregion

        #region EVENTS
        /// <summary>
        /// Called when a tag is removed from the associated object. Normally called to remove the visual residue of the element.
        /// </summary>
        public Action<object> OnTagRemoved;
        /// <summary>
        /// Called when a tag is created within the associated object. Currently used to automatically instantiate it within the visual element.
        /// </summary>
        public Action<object> OnTagCreated;
        /// <summary>
        /// Called when the sorting type for the object is changed to refresh it in real time.
        /// </summary>
        public Action OnSortToggle;
        /// <summary>
        /// Called when the <b>Add</b> button is pressed in the element. Currently unused.
        /// </summary>
        public Action OnAddButton;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// An empty constructor. It simply initializes the object.
        /// </summary>
        public LBSTagListGroup() : base()
        {
            Init();
        }
        /// <summary>
        /// A variant of the constructor that can be initialized as removable. Unused.
        /// </summary>
        /// <param name="removable"></param>
        public LBSTagListGroup(bool removable) : base()
        {
            this.removable = removable;
            Init();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Initializes all containers and objects within the Visual Element. It additionally sets up the object's <b>List View</b> as per the associated
        /// object's necessities, converting all objects added to it into <b>Tag List Objects</b> with the associated element inside.
        /// </summary>
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

        /// <summary>
        /// Initializes the list within the visual element, adding all elemenents from a provided list.
        /// </summary>
        /// <param name="initList">A list with all elements to initialize and introduce into this visual element's tag list.</param>
        public void ListInitialize(List<ScriptableObject> initList)
        {
            tagList.Clear();
            foreach (object tag in initList)
            {
                tagList.Add(tag);
            }
        }
        /// <summary>
        /// Adds a new <b>Tag Group</b> to the tag list. Unused.
        /// </summary>
        /// <param name="newObj">The object to add to the tag list.</param>
        /// <param name="removable">Checks whether the element will be removable or not upon introduction.</param>
        public void AddToGroup(LBSTagGroup newObj, bool removable = false)
        {
            tagList.Add(newObj);
        }
        /// <summary>
        /// Adds a new tag to the tag list. Unused.
        /// </summary>
        /// <param name="newObj">The object to add to the tag list.</param>
        /// <param name="removable">Checks whether the element will be removable or not upon introduction.</param>
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

        /// <summary>
        /// Sets the sorting type for the List View depending on the provided variable (usually the current sorting setting). </br>
        /// When set to Disabled, it'll order all objects within the list by their natural order.</br>
        /// When set to Ascending, it'll order all objects alphabetically in an ascending orrder.</br>
        /// When set to Descending, it'll order all objects alphabetically in a descending order.
        /// </summary>
        /// <param name="type"></param>
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
        /// <summary>
        /// Binds an Add Button option to the visual element's <b>Add Button</b>.
        /// </summary>
        /// <param name="option"></param>
        public void BindAddButton(int option)
        => BindAddButtons(new List<int> { option });

        /// <summary>
        /// Binds a number of "Add Button options" to the visual element's <b>Add</b> Button based on a number of parameters. Each number introduced into the list
        /// of options will add a different functionality to the button:</br>
        /// <b>1</b>: Allows the object to create a new <b>Tag Group</b> and store it.</br>
        /// <b>2</b>: Allows the object to create a new <b>Tag</b> and store it.</br>
        /// <b>3</b>: Allows the object to add an orphan tag to its associated group.</br>
        /// </summary>
        /// <param name="options"></param>
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

        /// <summary>
        /// Unbinds all button functionalities from the visual element's <b>Add</b> button, then remakes all bindings.
        /// </summary>
        public void RebindButtons()
        {
            addButton.Unbind();
            if ((buttons == null)||(buttons.Count==0)) return;
            BindAddButtons(buttons);
        }

        #endregion
    }
}
