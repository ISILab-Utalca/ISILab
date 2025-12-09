using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleManagerWindow;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

[UxmlElement]
public partial class BundleWizardSetBundleMenu : LBSComplexVisualElement, IBundleWizardTab
{
    //SearchAllBundles() variables
    private List<Bundle> _allBundles = new();
    private readonly List<BundleContainer> _mainBundles = new();
    private List<BundleCategory> AllCategories
    {
        get => new List<BundleCategory>()
            {
                _interiorCategory,
                _exteriorCategory,
                _populationCategory,
                _unassignedCategory,
                _orphanBundlesCategory
            };
    }
    private BundleCategory _interiorCategory = new();
    private BundleCategory _exteriorCategory = new();
    private BundleCategory _populationCategory = new();
    private BundleCategory _unassignedCategory = new();
    private BundleCategory _orphanBundlesCategory = new();

    private BundleFlags currentBundleFlags;
    private ListView listCurrent;
    private ListView listSameLayer;
    private ListView listNoLayer;
    private BundleManagerListGroup bundleListCurrent;
    private BundleManagerListGroup bundleListSameLayer;
    private BundleManagerListGroup bundleListNoLayer;

    private List<BundleManagerWindow.BundleContainer> bundleContainersCurrent = new();
    private List<BundleManagerWindow.BundleContainer> bundleContainersSameLayer = new();
    private List<BundleManagerWindow.BundleContainer> bundleContainersNoLayer = new();

    private LBSCustomTextField nameField;
    TabView tabView;

    public BundleBuilder Builder { get; set; }

    public BundleWizardSetBundleMenu() : base()
    {
        GetVisualTreeForThis();
        
        //nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        //this.Add(nameField);
    }

    public void Init()
    {
        //Debug.Log("Init: " + GetType().Name);
        Debug.Log("Builder data:\n\n" + Builder.ToString());

        SearchAllBundles();

        bundleListCurrent = this.Q<BundleManagerListGroup>("CurrentBundles");
        bundleListCurrent.SetBundleListViewItem<BundleWizardElement>(
            out listCurrent,
            "CurrentBundles",
            bundleContainersCurrent,
            itemHeight: 40
            );
        bundleListSameLayer = this.Q<BundleManagerListGroup>("SameLayerBundles");
        bundleListSameLayer.SetBundleListViewItem<BundleWizardElement>(
            out listSameLayer,
            "SameLayerBundles",
            bundleContainersSameLayer,
            itemHeight: 40
            );
        bundleListNoLayer = this.Q<BundleManagerListGroup>("NoLayerBundles");
        bundleListNoLayer.SetBundleListViewItem<BundleWizardElement>(
            out listNoLayer,
            "NoLayerBundles",
            bundleContainersNoLayer,
            itemHeight: 40
            );

        /*
        _interiorCategory.SetListGroup("Interior", window. rootVisualElement);
        _exteriorCategory.SetListGroup("Exterior", rootVisualElement);
        _populationCategory.SetListGroup("Population", rootVisualElement);
        _unassignedCategory.SetListGroup("Unassigned", rootVisualElement);
        _orphanBundlesCategory.SetListGroup("OrphanBundles", rootVisualElement);
        */

        /*
        foreach (BundleCategory category in AllCategories)
        {
            // Setting MainBundle lists
            category.SetBundleListViewItem();
            // Setting Expand List Buttons
            category.SetExpandButtonSetting(Instance);
        }
        */
    }

    private void SearchAllBundles()
    {
        //Clear lists
        _allBundles.Clear();

        _mainBundles.Clear();
        foreach (BundleCategory category in AllCategories)
        {
            category.Bundles.Clear();
        }

        _allBundles = LBSAssetsStorage.Instance.Get<Bundle>();

        // Normal bundles
        foreach (Bundle b in _allBundles)
        {
            // deben haber 3 casos, current layer without parent, current layer with parent, and no layer
            if (b.ChildsBundles.Count <= 0 && b.LayerContentFlags==currentBundleFlags)
            {
                bundleContainersSameLayer.Add(new BundleManagerWindow.BundleContainer(b));
            }
        }

        Debug.Log("BundleManagerWindow updated");
    }
    
    public void Step()
    {
        //throw new System.NotImplementedException();
    }

    public void Revert()
    {
        Debug.Log("Builder data:\n\n" + Builder.ToString());
        //throw new System.NotImplementedException();
    }
}
