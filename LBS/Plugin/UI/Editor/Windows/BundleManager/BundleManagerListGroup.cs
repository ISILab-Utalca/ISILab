using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard;
using ISILab.Extensions;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager
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
            Assert.IsNotNull(vta);
            vta.CloneTree(this);

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


        public void SetBundleListViewItem<T>(
            out ListView listView,
            string columnName,
            List<BundleManagerWindow.BundleContainer> bundles,
            bool main = false,
            float itemHeight = 32,
            bool deleteIconBool = true
        ) where T : VisualElement, IBundleElement, new()
        {
            listView = this.listView;

            listView.itemsSource = bundles;
            listView.fixedItemHeight = itemHeight;

            var list = listView;
            listView.makeItem = () => new T();
            listView.bindItem = (item, i) =>
            {
                //Debug.Log("BIND ITEM");

                T element = (T)item;
                BundleManagerWindow.BundleContainer container = (BundleManagerWindow.BundleContainer)list.itemsSource[i];
                element.SetBundleReference(container.GetMainBundle(), list, main);

                element.SetIconDisplay("Main", main);
                element.SetIconDisplay("Warning", container.GetWarnings().Count > 0);
                element.SetIconDisplay("Bundle", false);
                //element.SetIconDisplay("Select", true); // TODO: Handle condition

                if (element is BundleWizardElement wizElement)
                {
                    wizElement.Index = i; // Maybe add Index to IBundleElement to avoid this extra check?
                    System.Action remove = () =>
                    {
                        //bundles.RemoveAt(i); // Apparently the callback is being called twice so using Remove instead ensures deleting only one element.
                        bundles.Remove(container);
                        EditorApplication.delayCall += () =>
                        {
                            list.Rebuild();
                            list.RefreshItems();
                        };
                    };
                    wizElement.SetRemoveCallback(remove);

                    if(!deleteIconBool)wizElement.RemoveDeleteIcon();

                }
            };

            if(typeof(T) == typeof(BundleManagerElement))
            {
                if(BundleManagerWindow.Instance)
                    BundleManagerWindow.Instance.SetBundleListViewSettings(ref listView, columnName, bundles, main);
            }
        }

        public void GetListViewRef(out LBSCustomListView listView)
        {
            listView = this.listView;
        }

        public void SetExpandButtonSetting(VisualElement rootVisualElement, string columnName, ListView list, bool expanded = true)
        {
            var button = rootVisualElement.Q<VisualElement>(columnName).Q<Button>("ExpandButton");

            button.clickable = new Clickable(() =>
            {
                bool newState = !list.GetDisplay();
                list.SetDisplay(newState);
                button.iconImage = Background.FromVectorImage(newState ? arrowDownIcon : arrowSideIcon);
            });

            list.SetDisplay(list.itemsSource is not null && list.itemsSource.Count > 0 && expanded);
            button.iconImage = Background.FromVectorImage(list.GetDisplay() ? arrowDownIcon : arrowSideIcon);
        }

    }
}

