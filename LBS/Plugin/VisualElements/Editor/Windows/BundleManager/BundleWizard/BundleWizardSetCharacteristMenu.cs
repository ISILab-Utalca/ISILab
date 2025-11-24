using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSetCharacteristMenu : VisualElement, IBundleWizardTab
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public BundleBuilder Builder { get; set; }

    public BundleWizardSetCharacteristMenu(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }

    public void Init()
    {
        Debug.Log("Init: " + GetType().Name);
    }

    public void Step()
    {
        //throw new System.NotImplementedException();
    }

    public void Revert()
    {
        //throw new System.NotImplementedException();
    }
}
