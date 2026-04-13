using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements;
using System;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    [UxmlElement]
    public partial class LBSTagListGroup : LBSBaseListGroup
    {
        #region FIELDS
        private string tagListName;
        private bool removable = true;

        #endregion

        #region VISUAL ELEMENTS
        private LBSToolbarButton disabledRemoveButton;
        private Label titleLabel;
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
            //Basic setup
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSTagListLayerTag");
            visualTree.CloneTree(this);
            
            //Remove button
            disabledRemoveButton = this.Q<LBSToolbarButton>("DisabledRemoveButton");
            OnListRemoved += () => { RemoveFromHierarchy(); };
            //The original list already has the expansion thingy and the toggle sort.

            //Title
            titleLabel = this.Q<Label>("TitleLabel");
            titleLabel.text = tagListName;

            OnSortToggle += TagListSortToggle;
        }

        public void TagListSortToggle()
        {
            switch (currentSort)
            {
                case SortType.Disabled:

                    break;
                case SortType.Ascending:

                    break;
                case SortType.Descending:

                    break;
            }
        }
        #endregion








    }
}
