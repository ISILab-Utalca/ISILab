using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class WizardStepOne : VisualElement
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public WizardStepOne(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }
    
}
