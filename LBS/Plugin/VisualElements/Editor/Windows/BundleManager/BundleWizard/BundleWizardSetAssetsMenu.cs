using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSetAssetsMenu : VisualElement, IBundleWizardTab
{
    private LBSCustomTextField nameField;
    TabView tabView;
    
    public BundleBuilder Builder { get; set; }

    public BundleWizardSetAssetsMenu(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        this.Add(nameField);
        
    }

    public void Init()
    {
        //Debug.Log("Init: " + GetType().Name);
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }

    public void Step()
    {
        
    }

    public void Revert()
    {
        
    }
}
