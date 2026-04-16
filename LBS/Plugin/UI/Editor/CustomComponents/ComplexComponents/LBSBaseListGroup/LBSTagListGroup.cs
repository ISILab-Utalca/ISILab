using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
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
        private List<object> tagList = new ();

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
        public LBSTagListGroup(bool removable, List<object> listInitialize = null) : base()
        {
            this.removable = removable;
            Init();

            tagList = listInitialize;
            //You can initialize thelist immediately if you introduce the tag list
            if(listInitialize!=null)
            {
                ListInitialize(listInitialize);
            }
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
            listView.itemsSource = tagList;
        }

        public void ListInitialize(List<object> initList)
        {
            foreach(object tag in initList)
            {
                LBSTagListObject newObj = new LBSTagListObject(this, tag, false);
                tagList.Add(newObj);
            }
        }

        public void ToggleSort()
        {
            switch (currentSort)
            {
                case SortType.Disabled:
                    toggleSortButton.SetValueWithoutNotify(true);
                    currentSort = SortType.Ascending;

                    break;
                case SortType.Ascending:
                    toggleSortButton.ToggleIcon = sortDescending;
                    toggleSortButton.SetValueWithoutNotify(true);
                    currentSort = SortType.Descending;

                    break;
                case SortType.Descending:
                    toggleSortButton.ToggleIcon = sortAscending;
                    toggleSortButton.SetValueWithoutNotify(false);
                    currentSort = SortType.Disabled;

                    break;
            }
            OnSortToggle?.Invoke();
        }

        public LBSTagListObject GetObjectFromTag(object tag)
        {
            return tagListVE.Find(c => c.AssociatedTag == tag);
        }
        #endregion








    }
}
