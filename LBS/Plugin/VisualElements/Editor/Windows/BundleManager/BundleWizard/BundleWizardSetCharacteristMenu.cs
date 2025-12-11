using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.UI.Editor.Windows;
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
        //Debug.Log("Init: " + GetType().Name);
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }

    public void Step()
    {
        //throw new System.NotImplementedException();
    }

    public void Revert()
    {
        Debug.Log("Builder data:\n\n" + Builder.ToString());
        //throw new System.NotImplementedException();
    }
}
