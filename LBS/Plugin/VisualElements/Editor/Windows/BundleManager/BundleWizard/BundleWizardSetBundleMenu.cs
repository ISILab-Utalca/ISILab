using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BundleWizardSetBundleMenu : LBSComplexVisualElement
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public BundleWizardSetBundleMenu(): base()
    {
        GetVisualTreeForThis();
        
        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }
    
}
