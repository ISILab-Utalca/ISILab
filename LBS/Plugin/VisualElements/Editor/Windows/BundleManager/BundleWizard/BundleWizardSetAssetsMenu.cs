using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSetAssetsMenu : VisualElement
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public BundleWizardSetAssetsMenu(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }
    
}
