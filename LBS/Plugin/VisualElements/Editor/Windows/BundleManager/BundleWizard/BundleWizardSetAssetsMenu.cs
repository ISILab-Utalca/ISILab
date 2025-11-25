using ISILab.Extensions;
using Samples.Editor.General;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSetAssetsMenu : VisualElement, IBundleWizardTab
{
    private TemplateContainer dragAndDropContainer;
    private VisualElement dragAndDropWindow;
    private DragAndDropWindow.DragAndDropManipulator manipulator;

    private List<GameObject> prefabs = new();
    private List<GameObject> models = new();

    TabView tabView;
    
    public BundleBuilder Builder { get; set; }

    public BundleWizardSetAssetsMenu(): base()
    {

    }

    private void GetObjects(List<Object> objects)
    {
        prefabs.AddRange(objects.Select(o => o as GameObject).ToList().RemoveEmpties());
        string s = "";
        prefabs.ForEach(o => s += AssetDatabase.GetAssetPath(o) + "\n");
        Debug.Log(s);
    }

    public void Init()
    {
        //Debug.Log("Init: " + GetType().Name);
        Debug.Log("Builder data:\n\n" + Builder.ToString()); 
        try
        {
            dragAndDropContainer = this.Q<TemplateContainer>();
            dragAndDropWindow = dragAndDropContainer.Q<VisualElement>("DragAndDrop");
            manipulator = new DragAndDropWindow.DragAndDropManipulator(dragAndDropContainer, GetObjects);
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    public void Step()
    {
        Builder.objects.AddRange(prefabs);
        Builder.models.AddRange(models);
    }

    public void Revert()
    {
        prefabs.Clear();
        models.Clear();
        Builder.objects.Clear();
        Builder.models.Clear();
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }
}
