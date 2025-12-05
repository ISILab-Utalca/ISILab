using ISI_Lab.LBS.Plugin.Components.Bundles;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;


[UxmlElement]
public partial class BundleWizardPopup: VisualElement
{
    private readonly LBSCustomTabView tabView;
    
    private readonly LBSCustomBreadcrumbs breadcrumbs;

    private readonly LBSCustomButton OKButton;
    private readonly LBSCustomButton nextButton;
    private readonly LBSCustomButton backButton;
    private readonly LBSCustomButton cancelButton;

    private int currentStep = 0;

    private string[] breadcrumbLabels = new string []
    {
        "New Main Bundle",
        "Convert Prefabs",
        "Assign Bundles",
        "Add Characteristics"
    };

    private Dictionary<string, VisualElement> tabs = new Dictionary<string, VisualElement>();
    

    private int CurrentStep 
    { 
        get => currentStep; 
        set 
        { 
            currentStep = value;
            if (tabView is not null) 
                tabView.selectedTabIndex = value; 
        } 
    }

    private bool InFinalStep => CurrentStep == breadcrumbLabels.Length - 1;

    private string CurrentBreadcrumb => breadcrumbLabels[CurrentStep]; 

    private IBundleWizardTab CurrentWizardTab => tabs[CurrentBreadcrumb] as IBundleWizardTab;
    
    public BundleWizardPopup()
    {
        VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(nameof(BundleWizardPopup));
        vta?.CloneTree(this);

        tabView = this.Q<LBSCustomTabView>("TabView");
        breadcrumbs = this.Q<LBSCustomBreadcrumbs>("WizardBreadcrumbs");
        
        OKButton = this.Q<LBSCustomButton>("OK");
        OKButton.clicked += OK;
        OKButton.clicked += () => OnAnyButtonClicked(OKButton);

        nextButton = this.Q<LBSCustomButton>("Next");
        nextButton.clicked += Next;
        nextButton.clicked += () => OnAnyButtonClicked(nextButton);

        backButton = this.Q<LBSCustomButton>("Back");
        backButton.clicked += Back;
        backButton.clicked += () => OnAnyButtonClicked(backButton);

        cancelButton = this.Q<LBSCustomButton>("Cancel");
        cancelButton.clicked += Cancel;
        cancelButton.clicked += () => OnAnyButtonClicked(cancelButton);

        ToggleNextButton(true);

        #region Local functions

        void Back()
        {
            CurrentWizardTab.Revert();

            if (CurrentStep <= 0)
            {
                Cancel();
                return;
            }

            CheckIfLeavingFinalStep();

            CurrentStep--;
            breadcrumbs.PopItem();
        }

        void OK()
        {
            CheckIfLeavingFinalStep();
            CurrentWizardTab.Builder.TryBuild();
            this.SetDisplay(false);
            CleanUp();
        }

        void Next()
        {
            CurrentWizardTab.Step();
            CurrentStep++;
            breadcrumbs.PushItem(CurrentBreadcrumb);
            CurrentWizardTab.Init();
            if(InFinalStep)
                ToggleNextButton(false);
        }

        void Cancel()
        {
            CheckIfLeavingFinalStep();
            this.SetDisplay(false);
            CleanUp();
        }

        void ToggleNextButton(bool showNext)
        {
            nextButton.SetDisplay(showNext);
            OKButton.SetDisplay(!showNext);
        }

        void CheckIfLeavingFinalStep()
        {
            if (InFinalStep)
                ToggleNextButton(true);
        }

        void OnAnyButtonClicked(Button button)
        {
            //Debug.Log(button.text);
            Debug.Log("Current Step: " + CurrentStep);
        }

        #endregion
    }

    public void Init()
    {
        var tabNames = new[] { "SelectBundleTypeMenu", "SetAssetsMenu", "SetBundleMenu", "SetCharacteristicsMenu" };
        Assert.IsTrue(breadcrumbLabels.Length == tabNames.Length);
        BundleBuilder builder = new BundleBuilder();
        for (int i = 0; i < tabNames.Length; i++)
        {
            VisualElement value = this.Q<VisualElement>(tabNames[i]);
            tabs.Add(breadcrumbLabels[i], value);
            (value as IBundleWizardTab).Builder = builder;
        }

        breadcrumbs.PushItem(CurrentBreadcrumb);
        CurrentWizardTab.Init();
    }

    void OnTabChanged()
    {
        return;
        foreach (VisualElement tab in tabs.Values)
        {
            tab.SetDisplay(false);
        }
        tabs[CurrentBreadcrumb].SetDisplay(true);

        string s = "Tabs Display:\n\n";
        foreach (VisualElement tab in tabs.Values)
        {
            s += tab.GetDisplay() + "\n";
        }
        Debug.Log(s);
    }

    void CleanUp()
    {
        try
        {
            while (CurrentStep > 0)
            {
                CurrentStep--;
                breadcrumbs.PopItem();
            }
            breadcrumbs.PopItem();
        }
        finally
        {
            tabs.Clear();
        }
    }
}

public class BundleBuilder
{
    public string bundleName { get; set; }
    public string layerType { get; set; }

    public List<List<GameObject>> objects { get; private set; } = new();
    
    public List<Bundle> tempBundles { get; private set; } = new();
    public List<Bundle> newSubBundles { get; private set; } = new();
     
    public List<LBSCharacteristic> characteristics { get; private set; } = new();

    public BundleBuilder() { }

    public void GetBundleConfiguration(ref Bundle bundle, string layerType)
    {
        switch (layerType)
        {
            case "Interior Layer":
                bundle.LayerContentFlags    = BundleFlags.Interior;
                bundle.Type                 = Bundle.TagType.Structural;
                bundle.Color                = default;
                break;

            case "Exterior Layer":
                bundle.LayerContentFlags    = BundleFlags.Exterior;
                bundle.Type                 = Bundle.TagType.Structural;
                bundle.Color                = default;
                break;

            case "Population Layer":
                bundle.LayerContentFlags    = BundleFlags.Population;
                bundle.Type                 = Bundle.TagType.Element;
                bundle.Color                = new Color().RandomColorHSV();
                break;

            default:
                bundle.LayerContentFlags    = default;
                bundle.Type                 = default;
                bundle.Color                = default;
                break;
        }
    }

    public void TryBuild()
    {
        Bundle main = ScriptableObject.CreateInstance<Bundle>();
        GetBundleConfiguration(ref main, layerType);
        main = BundleMenuItem.CreateBundleWithInstance(main, bundleName);
        for(int i = 0; i < newSubBundles.Count; i++)
        {
            newSubBundles[i] = BundleMenuItem.CreateBundleWithInstance(newSubBundles[i], newSubBundles[i].BundleName);
            main.AddChild(newSubBundles[i]);
        }
    }

    public override string ToString()
    {
        string s = "> Bundle Name:\t" + bundleName + "\n";
        s += "> Layer type:\t" + layerType + "\n\n";

        s += "> Objects:\n";
        objects.ForEach(obj => obj.ForEach(o => s += "\t" + o.ToString() + " | "));
        s += "\n\n";

        s += "> Sub Bundles:\n";
        newSubBundles.ForEach(sb => s += "\t" + sb.BundleName + " | ");
        s += "\n\n";

        s += "> Characteristics:\n";
        characteristics.ForEach(c => s += "\t" + c.ToString() + " | ");
        s += "\n\n";

        return s;
    }
}
