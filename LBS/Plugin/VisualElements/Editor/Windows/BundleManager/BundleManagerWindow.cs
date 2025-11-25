using System.Collections.Generic;
using System.Linq;
using ISI_Lab.LBS.Plugin.Components.Bundles;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Internal;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using JetBrains.Annotations;
using LBS.Bundles;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISI_Lab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager
{
    public class BundleManagerWindow : EditorWindow
    {
        class BundleCategory
        {
            private readonly List<BundleContainer> _bundles = new();
            private ListView _list;

            private BundleManagerListGroup _listGroup;

            private string _name;

            private bool _main = true;

            public List<BundleContainer> Bundles { get => _bundles; }
            public ListView List { get => _list; set => _list = value; }
            public BundleManagerListGroup ListGroup { get => _listGroup; set => _listGroup = value; }
            public string ListName { get => _name; } // Visual element name. Not to be confused with list title.
            public bool Main { get => _main; set => _main = value; }

            public void SetListGroup(string listName, VisualElement root)
            {
                _name = listName;
                ListGroup = root.Q<BundleManagerListGroup>(listName + "List");
            }

            public void SetBundleListViewItem()
            {
                ListGroup.SetBundleListViewItem<BundleManagerElement>(out _list, ListName, _bundles, _main);
            }

            public void SetExpandButtonSetting(BundleManagerWindow inst)
            {
                inst.SetExpandButtonSetting(ListName + "List", List);
            }
        }

        public static BundleManagerWindow Instance { get; private set; }

        // Explicit height for every row so ListView can calculate how many items to actually display
        private const int ItemHeight = 32;
        private const int ItemGap = 2;

        // References
        private VectorImage _arrowDown;
        private VectorImage _arrowSide;

        private readonly BundleSelection _selection = new();

        // Bundle lists
        private List<Bundle> _allBundles = new();
        private readonly List<BundleContainer> _mainBundles = new();

        private BundleCategory _interiorCategory = new();
        private BundleCategory _exteriorCategory = new();
        private BundleCategory _populationCategory = new();
        private BundleCategory _unassignedCategory = new();
        private BundleCategory _subBundlesCategory = new();
        private BundleCategory _orphanBundlesCategory = new();

        private List<BundleCategory> AllCategories 
        {
            get => new List<BundleCategory>()
            {
                _interiorCategory,
                _exteriorCategory,
                _populationCategory,
                _unassignedCategory,
                _subBundlesCategory,
                _orphanBundlesCategory
            };
        }
        
        // ListViews
        private ListView _validatorList;

        [MenuItem("Window/ISILab/Bundle Manager", priority = 1)]
        public static void ShowWindow()
        {
            BundleManagerWindow window = GetWindow<BundleManagerWindow>();
            Texture icon = LBSAssetMacro.LoadAssetByGuid<Texture>("6351057aa17189c44902075c0b9353fd");
            window.titleContent = new GUIContent("Bundle Manager", icon);
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            Instance = null;
        }

        private void CreateGUI()
        {
            //Set references
            _arrowDown = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("b570a25de51f01c41bd82dbe5372bb3f"));
            _arrowSide = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("83eafacbab9ab554299bc4d0f124d980"));

            // Find all bundles in database
            SearchAllBundles();
            // Find issues in bundles
            FindWarnings();
            // Create window
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("BundleManagerWindow");
            visualTree.CloneTree(rootVisualElement);

            _interiorCategory       .SetListGroup("Interior",       rootVisualElement);
            _exteriorCategory       .SetListGroup("Exterior",       rootVisualElement);
            _populationCategory     .SetListGroup("Population",     rootVisualElement);
            _unassignedCategory     .SetListGroup("Unassigned",     rootVisualElement);
            _subBundlesCategory     .SetListGroup("SubBundles",     rootVisualElement);
            _orphanBundlesCategory  .SetListGroup("OrphanBundles",  rootVisualElement);

            _subBundlesCategory.Main = _orphanBundlesCategory.Main = false;
            foreach (BundleCategory category in AllCategories)
            {
                // Setting MainBundle lists
                category.SetBundleListViewItem();
                // Setting Expand List Buttons
                category.SetExpandButtonSetting(Instance);
            }
            
            // Do the same for Validator
            _validatorList = rootVisualElement.Q<VisualElement>("BundleValidator").Q<ListView>();
            SetExpandButtonSetting("BundleValidator", _validatorList);

            // Setting Create Bundle Buttons
            SetCreateBundleButtonSetting("InteriorList", BundleFlags.Interior);
            SetCreateBundleButtonSetting("ExteriorList", BundleFlags.Exterior);
            SetCreateBundleButtonSetting("PopulationList", BundleFlags.Population);
            SetCreateBundleButtonSetting("UnassignedList", BundleFlags.None);
            SetCreateBundleButtonSetting("SubBundlesList", _selection);
            SetCreateBundleButtonSetting("OrphanBundlesList", BundleFlags.None, true);

            // Bundle Wizard
            BundleWizardPopup wizard = rootVisualElement.Q<BundleWizardPopup>("BundleWizardPopup");


            VisualElement bottomBar = rootVisualElement.Q<VisualElement>("BottomBar");
            // Setting organize button
            LBSCustomButton organizeButton = bottomBar.Q<LBSCustomButton>("OrganizeButton");
            organizeButton.clicked += () =>
            {
                rootVisualElement.Q<VisualElement>("SubBundlesList").Q<Label>().text = "Sub Bundles - Layer";
                ClearSelectionInOtherLists();
                _selection.ClearSelection();
                RefreshBundles();
            };

            // Setting findIssues button
            LBSCustomButton issuesButton = bottomBar.Q<LBSCustomButton>("IssuesButton");
            issuesButton.clicked += () =>
            {
                rootVisualElement.Q<VisualElement>("SubBundlesList").Q<Label>().text = "Sub Bundles - Layer";
                ClearSelectionInOtherLists();
                _selection.ClearSelection();
                _subBundlesCategory.List.Clear();
                FindWarnings();

                foreach(BundleCategory category in AllCategories)
                {
                    category.List.RefreshItems();
                }

                _subBundlesCategory.SetExpandButtonSetting(Instance);
                _validatorList = rootVisualElement.Q<VisualElement>("BundleValidator").Q<ListView>();
                SetExpandButtonSetting("BundleValidator", _validatorList);
            };

            // Create new main bundle button
            LBSCustomButton newMainBundleButton = bottomBar.Q<LBSCustomButton>("NewMainBundle");
            newMainBundleButton.clicked += () =>
            {
                wizard.Init();
                wizard.SetDisplay(true);
            };
        }

        #region BOTTOM PANEL / GLOBAL FUNCTIONS
        /// <summary>
        /// Searches new Bundles and updates the elements in the window.
        /// </summary>
        private void RefreshBundles()
        {
            SearchAllBundles();
            FindWarnings();

            foreach (BundleCategory category in AllCategories)
            {
                category.List.RefreshItems();
            }

            _subBundlesCategory.SetExpandButtonSetting(Instance);
            _validatorList = rootVisualElement.Q<VisualElement>("BundleValidator").Q<ListView>();
            SetExpandButtonSetting("BundleValidator", _validatorList);
        }

        /// <summary>
        /// Finds all bundles in project and sets their reference in the BundleContainer lists.
        /// </summary>
        private void SearchAllBundles()
        {
            //Clear lists
            _allBundles.Clear();

            _mainBundles.Clear();
            foreach(BundleCategory category in AllCategories)
            {
                category.Bundles.Clear();
            }

            _allBundles = LBSAssetsStorage.Instance.Get<Bundle>();

            // Normal bundles
            foreach (Bundle b in _allBundles)
            {
                switch (b.ChildsBundles.Count)
                {
                    case > 0: // Bundle has children = MainBundle
                    {
                        List<BundleContainer> subBundles = new();
                        foreach (Bundle cb in b.ChildsBundles)
                        {
                            subBundles.Add(new BundleContainer(cb));
                        }

                        BundleContainer mBundle = new BundleContainer(b, subBundles);
                        _mainBundles.Add(mBundle);
                        break;
                    }

                    case <= 0 when b.Parent() == null: // Bundle has no children and no parent = OrphanBundle
                    {
                        BundleContainer oBundle = new BundleContainer(b);
                        _orphanBundlesCategory.Bundles.Add(oBundle);
                        break;
                    }
                }
            }

            // Divide MainBundles by content
            foreach (var mBundle in _mainBundles)
            {
                switch (mBundle.GetMainBundle().LayerContentFlags)
                {
                    case BundleFlags.Exterior:
                        _exteriorCategory.Bundles.Add(mBundle);
                        break;
                    case BundleFlags.Interior:
                        _interiorCategory.Bundles.Add(mBundle);
                        break;
                    case BundleFlags.Population:
                        _populationCategory.Bundles.Add(mBundle);
                        break;
                    default:
                        _unassignedCategory.Bundles.Add(mBundle);
                        break;
                }
            }

            Debug.Log("BundleManagerWindow updated");
        }

        /// <summary>
        /// Searches through the BundleContainer lists to find dangerous or invalid settings and records them as warning in each corresponding BundleContainer.
        /// </summary>
        private void FindWarnings()
        {
            // ----------------------------------- CLEAR ALL WARNINGS -----------------------------------
            foreach (BundleContainer bundleContainer in _mainBundles)
            {
                bundleContainer.ClearWarnings();
                foreach (BundleContainer subBundleContainer in bundleContainer.GetSubBundles())
                {
                    subBundleContainer.ClearWarnings();
                }
            }

            foreach (BundleContainer orphanContainer in _orphanBundlesCategory.Bundles)
            {
                orphanContainer.ClearWarnings();
            }

            // ----------------------------------- GLOBAL CASES -----------------------------------
            List<BundleContainer> allContainers = new();
            allContainers.AddRange(_mainBundles);
            foreach (BundleContainer bundleContainer in allContainers.ToList())
            {
                allContainers.AddRange(bundleContainer.GetSubBundles());
            }

            allContainers.AddRange(_orphanBundlesCategory.Bundles);

            foreach (BundleContainer bundleContainer in allContainers)
            {
                Bundle bundle = bundleContainer.GetMainBundle();

                // Case 0: Null bundle
                if (bundle == null)
                {
                    bundleContainer.AddWarning("Bundle is null. Use Organize Folder button to clear empty bundles.");
                    continue;
                }

                // Case 1: No characteristics
                if (bundle.Characteristics.Count <= 0)
                {
                    bundleContainer.AddWarning("There are no characteristic assigned to this bundle.");
                }

                for (var i = 0; i < bundle.Characteristics.Count; i++)
                {
                    var cha = bundle.Characteristics[i];

                    // Case 1.1: Characteristics empty
                    if (cha == null)
                    {
                        bundleContainer.AddWarning("Characteristic " + i + " is null.");
                        continue;
                    }

                    // Particular cases (it depends and is defined on the type of the characteristic)
                    List<string> warnings = new();
                    try
                    {
                        warnings.AddRange(cha.Validate());
                    }
                    catch(System.Exception e)
                    {
                        warnings.Add(e.Message); // idk if its receiving the exception message
                    }

                    foreach (var w in warnings)
                    {
                        bundleContainer.AddWarning(w);
                    }
                }
            }


            // ----------------------------------- MAIN BUNDLES -----------------------------------
            // Case 0: Unassigned type in main bundles
            foreach (BundleContainer bundle in _unassignedCategory.Bundles)
            {
                bundle.AddWarning("Layer Content Flag is none.");
            }

            foreach (BundleContainer bundleContainer in _mainBundles)
            {
                Bundle mainBundle = bundleContainer.GetMainBundle();

                // Case 1: Prefab in main bundle
                if (!bundleContainer.IsCollection() && mainBundle.Assets.Count > 0)
                {
                    bundleContainer.AddWarning(
                        "Main bundle contains assets of their own; should have it as subBundle, or be one.");
                }

                // ----------------------------------- SUB BUNDLES -----------------------------------
                foreach (BundleContainer subBundleContainer in bundleContainer.GetSubBundles())
                {
                    Bundle subBundle = subBundleContainer.GetMainBundle();
                    
                    // Case 0: subBundle null
                    if (subBundle == null)
                    {
                        subBundleContainer.AddWarning("SubBundle is null. Refresh window to erase.");
                        continue;
                    }

                    // Case 1: No asset assigned
                    if (subBundle.Assets != null && subBundle.Assets.Count <= 0)
                    {
                        subBundleContainer.AddWarning("SubBundle has no asset assigned.");
                    }

                    // Case 1.1: No prefab in asset
                    for (int i = 0; i < subBundle.Assets!.Count; i++)
                    {
                        Asset a = subBundle.Assets[i];
                        if (a.obj == null)
                        {
                            subBundleContainer.AddWarning("Asset " + i + " has no prefab assigned.");
                        }
                    }
                }
            }
        }

        #endregion
        
        #region VISUAL ELEMENTS SETTINGS
        
        /// <summary>
        /// Sets a given listView with elements from a list of BundleContainer.
        /// </summary>
        /// <param name="listView">ListView to set.</param>
        /// <param name="columnName">Name of the VisualElement in window that contains the specific ListView.</param>
        /// <param name="bundles">List that will be used as itemSource for the ListView.</param>
        /// <param name="main">Bundles with subBundles should be set as "main" bundles.</param>
        void SetBundleViewSettings(out ListView listView, string columnName, List<BundleContainer> bundles,
            bool main = false)
        {
           
            
            
            
            // Get listView
            listView = rootVisualElement.Q<VisualElement>(columnName).Q<ListView>();

            // Set listView params
            listView.itemsSource = bundles;
            listView.fixedItemHeight = ItemHeight;

            // Set listView methods
            var view = listView;
            listView.makeItem = () => new BundleManagerElement(ItemHeight, ItemGap);
            listView.bindItem = (e, i) =>
            {
                BundleManagerElement element = (BundleManagerElement)e;
                BundleContainer container = (BundleContainer)view.itemsSource[i];

                if (!container.IsCollection())
                {
                    element.SetBundleReference(container.GetMainBundle(), view, true);
                }
                else
                {
                    element.SetRefs(container.GetMainCollection(), view);
                }

                element.SetIconDisplay(BundleManagerElement.Icons.Main, main);
                element.SetIconDisplay(BundleManagerElement.Icons.Warning, container.GetWarnings().Count > 0);
                element.SetIconDisplay(BundleManagerElement.Icons.Bundle, !container.IsCollection());
            };

            SetBundleListViewSettings(ref listView, columnName, bundles, main);
        }

        public void SetBundleListViewSettings(ref ListView listView, string columnName, List<BundleContainer> bundles,
            bool main = false)
        {
            var view = listView;
            listView.selectedIndicesChanged += objects =>
            {
                // Omit empty selections
                var selections = objects as int[] ?? objects.ToArray();
                if (selections.Length <= 0)
                {
                    return;
                }

                BundleContainer bundle = (BundleContainer)view.itemsSource[selections.First()];

                // Set subBundle list
                if (main)
                {
                    // Set _subBundle list of BundleContainer, using subBundles from selected item
                    List<BundleContainer> subBundles = bundle.GetSubBundles();
                    _subBundlesCategory.Bundles.Clear();
                    _subBundlesCategory.Bundles.AddRange(subBundles);

                    _subBundlesCategory.ListGroup.TitleText = "Sub Bundles - " + bundle.GetMainBundle().name;

                    _subBundlesCategory.SetBundleListViewItem();
                    _subBundlesCategory.List.RefreshItems();
                    _subBundlesCategory.SetExpandButtonSetting(Instance);
                }

                // Set validator list
                SetValidatorViewSettings(bundle);
                SetExpandButtonSetting("BundleValidator", _validatorList);

                // Display selection on inspector
                ClearSelectionInOtherLists(columnName);
                Debug.Log(columnName);
                _selection.SetSelection(bundle);
            };

            listView.itemsChosen += _ =>
            {
                if (!bundles[view.selectedIndex].IsCollection())
                {
                    Selection.activeObject = bundles[view.selectedIndex].GetMainBundle();
                }
                else
                {
                    Selection.activeObject = bundles[view.selectedIndex].GetMainCollection();
                }
            };
        }

        /// <summary>
        /// Sets a fixed ListView to show warning messages for a specified BundleContainer.
        /// </summary>
        /// <param name="selected">The ListView will show warnings related to the selected BundleContainer.</param>
        private void SetValidatorViewSettings(BundleContainer selected)
        {
            // Get listView
            _validatorList = rootVisualElement.Q<VisualElement>("BundleValidator").Q<ListView>();

            // Set listView params
            _validatorList.itemsSource = selected.GetWarnings();
            _validatorList.fixedItemHeight = ItemHeight * 2;

            // Set listView methods
            _validatorList.makeItem = () => new BundleManagerWarning();
            _validatorList.bindItem = (e, i) =>
                ((BundleManagerWarning)e).SetWarningContent((string)_validatorList.itemsSource[i]);

            _validatorList.selectedIndicesChanged += _ => { Selection.activeObject = selected.GetMainBundle(); };

            _validatorList.itemsChosen += _ => { Selection.activeObject = selected.GetMainBundle(); };
        }

        /// <summary>
        /// Sets a button to show and hide a related ListView.
        /// </summary>
        /// <param name="columnName">Name of the VisualElement in window that contains the specific Button.</param>
        /// <param name="list">ListView that will show and hide with the button.</param>
        void SetExpandButtonSetting(string columnName, ListView list)
        {
            var button = rootVisualElement.Q<VisualElement>(columnName).Q<Button>("ExpandButton");

            button.clickable.clicked += () =>
            {
                list.SetDisplay(!list.GetDisplay());
                button.iconImage = Background.FromVectorImage(list.GetDisplay() ? _arrowDown : _arrowSide);
            };

            list.SetDisplay(list.itemsSource is not null && list.itemsSource.Count > 0);
            button.iconImage = Background.FromVectorImage(list.GetDisplay() ? _arrowDown : _arrowSide);
        }

        /// <summary>
        /// Sets a button to create a new Bundle, with a specified BundleFlags assigned, or a BundleCollection. If not orphan, it will create an empty subBundle.
        /// </summary>
        /// <param name="columnName">Name of the VisualElement in window that contains the specific Button.</param>
        /// <param name="flags">The flag that will be assigned to the new Bundle.</param>
        /// <param name="orphan"></param>
        /// <param name="isCollection"></param>
        private void SetCreateBundleButtonSetting(string columnName, BundleFlags flags, bool orphan = false, bool isCollection = false)
        {
            var button = rootVisualElement.Q<VisualElement>(columnName).Q<Button>("NewBundleButton");

            button.clickable.clicked += () =>
            {
                if (isCollection)
                {
                    var collection = BundleMenuItem.CreateBundleCollection();
                    
                    if (!orphan)
                    {
                        collection.Collection.Add(BundleMenuItem.CreateBundle(flags, "New_SubBundle"));   
                    }
                }
                else
                {
                    var bundle = BundleMenuItem.CreateBundle(flags);
                    
                    if (!orphan)
                    {
                        bundle.AddChild(BundleMenuItem.CreateBundle(flags, "New_SubBundle"));   
                    }
                }
                
                RefreshBundles();
            };
        }

        /// <summary>
        /// Sets a button to create a new Bundle that will be assigned as children to another Bundle.
        /// </summary>
        /// <param name="columnName">Name of the VisualElement in window that contains the specific Button.</param>
        /// <param name="selection"></param>
        private void SetCreateBundleButtonSetting(string columnName, BundleSelection selection)
        {
            var button = rootVisualElement.Q<VisualElement>(columnName).Q<Button>("NewBundleButton");

            button.clickable.clicked += () =>
            {
                var parentContainer = selection.GetSelectedBundle();
                if (parentContainer == null) return;
                
                if (!parentContainer.IsCollection())
                {
                    var bundleParent = parentContainer.GetMainBundle();
                    var bundle = BundleMenuItem.CreateBundle(bundleParent.LayerContentFlags, "New_SubBundle");
                    bundleParent.AddChild(bundle);
                    
                }
                else
                {
                    var collectionParent = parentContainer.GetMainCollection();
                    var bundle = BundleMenuItem.CreateBundle(BundleFlags.None, "New_SubBundle");
                    collectionParent.Collection.Add(bundle);
                }
                
                //POSIBLE SOLUCIÓN? MonoBehaviour.Invoke("RefreshSubBundleList", 0);
            };
        }

        private void RefreshSubBundleList(BundleContainer parentContainer)
        {
            RefreshBundles();
            List<BundleContainer> subBundles = parentContainer.GetSubBundles();
            _subBundlesCategory.Bundles.AddRange(subBundles);
            _subBundlesCategory.List.RefreshItems();
            _subBundlesCategory.SetExpandButtonSetting(Instance);
        }
        #endregion
        
        #region SELECTION CLASS AND FUNCTIONS
        private class BundleSelection
        {
            private BundleContainer _selectedBundle;

            public void SetSelection(BundleContainer bundle)
            {
                _selectedBundle = bundle;
            }

            [CanBeNull]
            public BundleContainer GetSelectedBundle()
            {
                return _selectedBundle;
            }

            public void ClearSelection()
            {
                _selectedBundle = null;
            }
        }
        
        /// <summary>
        /// Clears selection in all ListView elements, but the specified one.
        /// </summary>
        /// <param name="noClear">Name of the ListView that won't be cleared.</param>
        private void ClearSelectionInOtherLists(string noClear = null)
        {
            noClear ??= "NoMatch";
            foreach(BundleCategory category in AllCategories)
            {
                if (noClear.Equals(category.ListName))
                    continue;
                category.List.ClearSelection();
            }

            if (!noClear.Equals("BundleValidator"))
            {
                _validatorList.ClearSelection();
            }
        }
        #endregion
        public class BundleContainer
        {
            private readonly Bundle _main;
            [System.Obsolete]
            private readonly BundleCollection _collection;
            
            private readonly List<BundleContainer> _subBundles;
            private readonly List<string> _warnings;

            public BundleContainer(Bundle main, List<BundleContainer> subBundles = null)
            {
                _main = main;
                _collection = null;
                
                _subBundles = subBundles;
                _warnings = new List<string>();
            }
            public BundleContainer(BundleCollection collection, List<BundleContainer> subBundles = null)
            {
                _main = null;
                _collection = collection;
                
                _subBundles = subBundles;
                _warnings = new List<string>();
            }
            
            public Bundle GetMainBundle()
            {
                return _main;
            }

            [System.Obsolete]
            public BundleCollection GetMainCollection()
            {
                return _collection;
            }
            
            public List<BundleContainer> GetSubBundles()
            {
                return _subBundles;
            }

            public List<string> GetWarnings()
            {
                return _warnings;
            }

            public void AddWarning(string warning)
            {
                _warnings.Add(warning);
            }

            public void ClearWarnings()
            {
                _warnings.Clear();
            }

            [System.Obsolete]
            public bool IsCollection()
            {
                return _collection != null;
            }

            public override string ToString()
            {
                return _main.name;
            }
        }
    }
}
