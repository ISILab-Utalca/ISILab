using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard;
using ISILab.Extensions;
using UnityEngine;

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

        public static System.Action<object> OnRequestMove;
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

        private VisualTreeAsset listItemTemplate;
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
            //TODO: Implement this constructor            
        }


        public void SetBundleListViewItem<T>(
            out ListView listView,
            string columnName,
            List<BundleManagerWindow.BundleContainer> bundles,
            bool main = false,
            float itemHeight = 32,
            BundleWizardElement.Func buttonFunc = BundleWizardElement.Func.TRASH
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
                    wizElement.ChangeButtonIcon(buttonFunc);
                    
                    if (buttonFunc == BundleWizardElement.Func.TRASH)
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
                    }
                    else if(buttonFunc == BundleWizardElement.Func.ADD || buttonFunc == BundleWizardElement.Func.REMOVE)
                    {
                        wizElement.Index = i; // Maybe add Index to IBundleElement to avoid this extra check?
                        System.Action changeList = () =>
                        {
                            //bundles.RemoveAt(i); // Apparently the callback is being called twice so using Remove instead ensures deleting only one element.
                            if (bundles.Contains(container))
                            {
                                //bundles.Remove(container);

                                OnRequestMove?.Invoke(container);
                                EditorApplication.delayCall += () =>
                                {
                                    list.Rebuild();
                                    list.RefreshItems();
                                };
                            }
                        };
                        wizElement.SetTextAsUneditable();
                        wizElement.SetRemoveCallback(changeList);
                    }
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

        public void SetExpandButtonSetting(VisualElement rootVisualElement, string columnName, ListView list, bool expanded = true, bool forceExpanded = false)
        {
            var button = rootVisualElement.Q<VisualElement>(columnName).Q<Button>("ExpandButton");

            button.clickable = new Clickable(() =>
            {
                bool newState = !list.GetDisplay();
                list.SetDisplay(newState);
                button.iconImage = Background.FromVectorImage(newState ? arrowDownIcon : arrowSideIcon);
            });

            list.SetDisplay((list.itemsSource is not null && list.itemsSource.Count > 0 && expanded)||forceExpanded);
            button.iconImage = Background.FromVectorImage(list.GetDisplay() ? arrowDownIcon : arrowSideIcon);
        }
    }
}

