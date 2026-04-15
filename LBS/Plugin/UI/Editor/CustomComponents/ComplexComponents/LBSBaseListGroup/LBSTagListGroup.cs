using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
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
        private List<LBSTagListObject> tagListVE;
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

            //Remove button
            disabledRemoveButton = this.Q<LBSToolbarButton>("DisabledRemoveButton");
            OnListRemoved += () => { RemoveFromHierarchy(); };
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
                
                var objectEntry = tagList[index];
                if (tagList[index].GetType() == typeof(LBSTagGroup))
                {
                    LBSTagGroup tagEntry = tagList[index] as LBSTagGroup;
                    objectEntryVE.Type = LBSTagListObject.objectType.Group;
                    objectEntryVE.Name = tagEntry.name;
                    objectEntryVE.AssociatedTag = tagEntry;
                    objectEntryVE.AddLayerTag();
                }
                else if (tagList[index].GetType() == typeof(LBSTag))
                {
                    LBSTag tagEntry = tagList[index] as LBSTag;
                    objectEntryVE.Type = LBSTagListObject.objectType.Individual;
                    objectEntryVE.Name = tagEntry.label;
                    objectEntryVE.AssociatedTag = tagEntry;
                }
            };
            listView.itemsSource = tagList;
            listView.Rebuild();
        }

        public void ListInitialize(List<LBSTagGroup> initList)
        {
            tagList.Clear();
            foreach (object tag in initList)
            {
                tagList.Add(tag);
            }
        }

        public void ListInitialize(List<LBSTag> initList)
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
                    tagList.Sort((a, b) => tagListVE.Find(c => c.AssociatedTag.Equals(a)).Name.CompareTo(tagListVE.Find(d => d.AssociatedTag.Equals(b)).Name));
                    //tagList = tagList.OrderBy(i => i).ToList();
                    currentSort = SortType.Ascending;

                    break;
                case SortType.Ascending:
                    toggleSortButton.ToggleIcon = sortDescending;
                    toggleSortButton.SetValueWithoutNotify(true);
                    tagList.Sort((a, b) => tagListVE.Find(c => c.AssociatedTag.Equals(b)).Name.CompareTo(tagListVE.Find(d => d.AssociatedTag.Equals(a)).Name));
                    currentSort = SortType.Descending;

                    break;
                case SortType.Descending:
                    toggleSortButton.ToggleIcon = sortAscending;
                    toggleSortButton.SetValueWithoutNotify(false);
                    //tagList.Sort((a, b) => a.GetHashCode().CompareTo(b.GetHashCode())); <- This isP probably very dumb lol - Alice
                    //tagList.OrderBy(i => i);
                    currentSort = SortType.Disabled;

                    break;
            }
            listView.Rebuild();
            //OnSortToggle?.Invoke();
        }

        public LBSTagListObject GetObjectFromTag(object tag)
        {
            return tagListVE.Find(c => c.AssociatedTag == tag);
        }
        #endregion








    }
}
