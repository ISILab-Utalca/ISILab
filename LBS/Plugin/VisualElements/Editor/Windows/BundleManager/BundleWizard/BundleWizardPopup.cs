using System;
using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Bundles;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardPopup: VisualElement
{
    private LBSCustomTabView tabView;
    
    private LBSCustomBreadcrumbs breadcrumbs;

    private int currentStep = 0;
    private List<Tuple<string, Action>> breadcrumbSteps;

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
            currentStep = value; if (tabView is not null) tabView.selectedTabIndex = value; 
        } 
    }
    private string CurrentBreadcrumb { get => breadcrumbLabels[CurrentStep]; }
    private IBundleWizardTab CurrentWizardTab { get => tabs[CurrentBreadcrumb] as IBundleWizardTab; }
    
    public BundleWizardPopup()
    {
        VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(nameof(BundleWizardPopup));
        vta?.CloneTree(this);

        tabView = this.Q<LBSCustomTabView>("TabView");
        breadcrumbs = this.Q<LBSCustomBreadcrumbs>("WizardBreadcrumbs");
        
        var backButton = this.Q<LBSCustomButton>("Back");
        backButton.clicked += Back;
        backButton.clicked += () => OnAnyButtonClicked(backButton);

        var nextButton = this.Q<LBSCustomButton>("Next");
        nextButton.clicked += Next;
        nextButton.clicked += () => OnAnyButtonClicked(nextButton);

        var cancelButton = this.Q<LBSCustomButton>("Cancel");
        cancelButton.clicked += Cancel;
        cancelButton.clicked += () => OnAnyButtonClicked(cancelButton);

        #region Local functions

        void Back()
        {
            CurrentWizardTab.Revert();

            if (CurrentStep <= 0)
            {
                Cancel();
                return;
            }

            CurrentStep--;
            breadcrumbs.PopItem();
            //CurrentWizardTab.Init();
            //OnTabChanged();
        }

        void Next()
        {
            CurrentWizardTab.Step();
            CurrentStep++;
            breadcrumbs.PushItem(CurrentBreadcrumb);
            CurrentWizardTab.Init();
            //OnTabChanged();
        }

        void Cancel()
        {
            this.SetDisplay(false);
            CleanUp();
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
        BundleBuilder builder = new BundleBuilder();
        for (int i = 0; i < 4; i++)
        {
            VisualElement value = this.Q<VisualElement>(tabNames[i]);
            tabs.Add(breadcrumbLabels[i], value);
            (value as IBundleWizardTab).Builder = builder;
        }

        breadcrumbs.PushItem(CurrentBreadcrumb);
        CurrentWizardTab.Init();
        //OnTabChanged();

        //var tab = this.Q<LBSCustomTabView>("TabView");
        //Debug.Log(tab.DisplayTabs);
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

    public List<GameObject> objects { get; private set; } = new();
    public List<GameObject> models { get; private set; } = new();
     
    public List<Bundle> newSubBundles { get; private set; } = new();
     
    public List<LBSCharacteristic> characteristics { get; private set; } = new();

    public BundleBuilder() { }

    public override string ToString()
    {
        string s = "> Bundle Name:\t" + bundleName + "\n";
        s += "> Layer type:\t" + layerType + "\n\n";

        s += "> Objects:\n";
        objects.ForEach(o => s += "\t" + o.ToString() + " | ");
        s += "\n";
        s += "> Models:\n";
        models.ForEach(m => s += "\t" + m.ToString() + " | ");
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
