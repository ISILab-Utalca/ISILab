using System;
using System.Collections.Generic;
using ISILab.LBS.CustomComponents;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    [UxmlElement]
    public partial class LBSBaseListGroup : LBSComplexVisualElement
    {
        
        private readonly VectorImage arrowDownIcon;
        private readonly VectorImage arrowSideIcon;
        
        private bool isEmpty = false;
        private bool isExpanded = true;
        
        // VisualElement references
        private LBSCustomButton overlayButton;
        private Label titleLabel;
        protected LBSCustomListView listView;
        private Button expandArrowButton;
        protected LBSToolbarButton removeButton;

        [UxmlAttribute]
        public bool IsEmpty
        {
            get => isEmpty;
            set
            {
                isEmpty = value;
                if (overlayButton != null){
                    overlayButton.SetEnabled(isEmpty); 
                    overlayButton.style.visibility = isEmpty ? Visibility.Visible : Visibility.Hidden;
                    overlayButton.style.display = isEmpty? DisplayStyle.Flex: DisplayStyle.None;
                }
            }
        }
        
        [UxmlAttribute]
        public string TitleText
        {
            get
            {
                if (titleLabel != null) return titleLabel.text;
                else return "No title label found!";
            }
            set
            {
                if (titleLabel != null) titleLabel.text = value;
            }
        }

        [UxmlAttribute]
        public bool IsFoldoutExpanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                if (listView != null)
                {
                    listView.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (expandArrowButton != null)
                {
                    if (isExpanded)
                    {
                        expandArrowButton.style.backgroundImage = new StyleBackground(arrowDownIcon);
                    }
                    else
                    {
                        expandArrowButton.style.backgroundImage = new StyleBackground(arrowSideIcon);
                    }
                }
            }
        }
        
        /*
        private LBSCustomListItem listItemTemplate;
        [UxmlAttribute]
        public LBSCustomListItem ListItemTemplate
        {
            get => listItemTemplate;
            set
            {
                listItemTemplate = value;
                if (listView != null) listView.itemTemplate = value.visualTreeAssetSource; // such a hack!
            }
        }


        private List<LBSCustomListItem> listItems;

        [UxmlObjectReference("LBSCustomListItem")]
        public List<LBSCustomListItem> ListItems
        {
            get => listItems;
            set
            {
                listItems = value;
            }
        }
        */

        #region EVENTS
        public Action OnListRemoved;
        #endregion

        public LBSBaseListGroup() : base()
        {
            GetVisualTreeForThis();
            AddToClassList("lbs-base-list-group");

            arrowDownIcon = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("b570a25de51f01c41bd82dbe5372bb3f"));
            arrowSideIcon = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("83eafacbab9ab554299bc4d0f124d980"));
            
            overlayButton = this.Q<LBSCustomButton>("EmptyOverlayButton");
            overlayButton.RegisterCallback<ClickEvent>(_evt =>
            {
               isEmpty = false; 
               overlayButton.SetEnabled(false);
               overlayButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            });
            

            titleLabel = this.Q<Label>("TitleLabel");
            listView = this.Q<LBSCustomListView>("ListView");
          
            expandArrowButton = this.Q<Button>("ExpandButton");
            expandArrowButton.RegisterCallback<ClickEvent>(_evt =>
            { 
                IsFoldoutExpanded = !IsFoldoutExpanded;
            });

            removeButton = this.Q<LBSToolbarButton>("RemoveButton");
            removeButton.RegisterCallback<ClickEvent>(_evt =>
            {
                OnListRemoved?.Invoke();
            });
        }
    }
}

