using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSetCharacteristMenu : VisualElement
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public BundleWizardSetCharacteristMenu(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }
    
}
