using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEditor;
using UnityEngine.UIElements;


namespace ISI_Lab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager
{

    [UxmlElement]
    public partial class BundleManagerListGroup : VisualElement
    {
        
        private VectorImage arrowDownIcon;
        private VectorImage arrowSideIcon;

        #region INTERNAL FIELDS

        private VisualElement titleCard;
        private Button leftSideButton;
        private Button rightSideButton;
        private Label titleLabel;
        private LBSCustomListView listView;
        private VisualTreeAsset listItemTemplate;

        #endregion


        #region ATRIBUTES

        [UxmlAttribute]
        public string TitleText
        {
            get => titleLabel.text;
            set
            {
                if (titleLabel != null) titleLabel.text = value;
            }
        }

        [UxmlAttribute]
        public VisualTreeAsset ListItemTemplate
        {
            get => listItemTemplate;
            set
            {
                listItemTemplate = value;
                if (listView != null) listView.itemTemplate = value;
            }
        }

        #endregion


        public BundleManagerListGroup() : base()
        {
            VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(nameof(BundleManagerListGroup));
            vta?.CloneTree(this);

            arrowDownIcon = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("b570a25de51f01c41bd82dbe5372bb3f"));
            arrowSideIcon = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("83eafacbab9ab554299bc4d0f124d980"));
            
            titleCard = this.Q<VisualElement>("TitleCard");
            leftSideButton = this.Q<Button>("ExpandButton");
            rightSideButton = this.Q<Button>("NewBundleButton");
            titleLabel = this.Q<Label>("TitleLabel");
            listView = this.Q<LBSCustomListView>("List");

            
            
        }

        public BundleManagerListGroup(ListView listView) : this()
        {
            //TODO: Implement this constuctor            
        }


        public void SetBundleListViewItem(
            out ListView listView,
            string columnName,
            List<BundleManagerWindow.BundleContainer> bundles,
            bool master = false
        )
        {
            listView = null;
        }
    }
}

