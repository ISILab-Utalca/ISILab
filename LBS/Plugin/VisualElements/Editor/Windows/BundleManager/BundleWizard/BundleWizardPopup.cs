using System;
using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardPopup: VisualElement
{
    
    
    private LBSCustomBreadcrumbs breadcrumbs;

    private int currentStep = 0;
    private List<Tuple<string, Action>> breadcrumbSteps;

    private string[] breadcrumbLabels = new string []
    {
        "New Bundle Collection",
        "Convert Prefabs",
        "Assign Bundles",
        "Add Characteristics"
    };
    
    
    public BundleWizardPopup()
    {
        VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(nameof(BundleWizardPopup));
        vta?.CloneTree(this);
        
        breadcrumbs = this.Q<LBSCustomBreadcrumbs>("WizardBreadcrumbs");
        breadcrumbs.PushItem(breadcrumbLabels[currentStep]);
        
    }
}
