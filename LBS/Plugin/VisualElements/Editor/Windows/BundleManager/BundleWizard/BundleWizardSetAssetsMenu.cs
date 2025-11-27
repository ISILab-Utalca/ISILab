using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager;
using ISILab.Extensions;
using Samples.Editor.General;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard;


[UxmlElement]
public partial class BundleWizardSetAssetsMenu : VisualElement, IBundleWizardTab
{
    private TemplateContainer dragAndDropContainer;
    private VisualElement dragAndDropWindow;
    private DragAndDropWindow.DragAndDropManipulator manipulator;

    private ListView bundleList;
    private List<BundleManagerWindow.BundleContainer> bundleContainers = new();
    private BundleManagerListGroup bundleListGroup;

    private List<Bundle> tempBundles = new();

    //private List<GameObject> prefabs = new();
    //private List<GameObject> models = new();

    TabView tabView;
    
    public BundleBuilder Builder { get; set; }

    public BundleWizardSetAssetsMenu(): base()
    {

    }

    private void GetObjects(List<Object> objects)
    {
        var prefabs = new List<GameObject>(objects.Select(o => o as GameObject)).RemoveEmpties();

        Bundle bundle = SetBundle(prefabs);

        bundleContainers.Add(new BundleManagerWindow.BundleContainer(bundle));

        bundleList.Rebuild();
        bundleList.RefreshItems();

        tempBundles.Add(bundle);

        string s = "";
        prefabs.ForEach(o => s += AssetDatabase.GetAssetPath(o) + "\n");
        Debug.Log(s);
    }

    private Bundle SetBundle(List<GameObject> prefabs)
    {
        Bundle bundle = ScriptableObject.CreateInstance<Bundle>();
        prefabs.ForEach(pref => bundle.AddAsset(pref));
        bundle.BundleName = Builder.bundleName;

        switch (Builder.layerType)
        {
            case "Interior Layer":
                bundle.LayerContentFlags = BundleFlags.Interior;
                bundle.Type = Bundle.TagType.Structural;
                break;
            case "Exterior Layer":
                bundle.LayerContentFlags = BundleFlags.Exterior;
                bundle.Type = Bundle.TagType.Structural;

                break;
            case "Population Layer":
                bundle.LayerContentFlags = BundleFlags.Population;
                bundle.Type = Bundle.TagType.Element;
                bundle.Color = new Color().RandomColorHSV();
                break;
        }

        return bundle;
    }

    public void Init()
    {
        //Debug.Log("Init: " + GetType().Name);
        Debug.Log("Builder data:\n\n" + Builder.ToString()); 
        try
        {
            dragAndDropContainer = this.Q<TemplateContainer>();
            dragAndDropWindow = dragAndDropContainer.Q<VisualElement>("DragAndDrop");
            manipulator = new DragAndDropWindow.DragAndDropManipulator(dragAndDropContainer, GetObjects);
            
            bundleListGroup = this.Q<BundleManagerListGroup>("NewBundles");
        }
        catch (System.Exception e) { Debug.LogException(e); }

        bundleListGroup = this.Q<BundleManagerListGroup>();
        bundleListGroup.SetBundleListViewItem<BundleWizardElement>(
            out bundleList,
            "NewBundles",
            bundleContainers,
            itemHeight: 40
            );
    }

    public void Step()
    {
        //Builder.objects.Add(new List<GameObject>(prefabs));
        Builder.tempBundles.AddRange(tempBundles);
        Builder.newSubBundles.AddRange(tempBundles);
    }

    public void Revert()
    {
        tempBundles.Clear();
        bundleContainers.Clear();

        Builder.tempBundles.Clear();
        Builder.newSubBundles.Clear();

        Builder.objects.Clear();
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }
}
