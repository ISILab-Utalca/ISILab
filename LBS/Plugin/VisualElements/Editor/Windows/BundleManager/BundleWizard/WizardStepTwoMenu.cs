using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class WizardStepTwoMenu : VisualElement
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public WizardStepTwoMenu(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }
    
}
