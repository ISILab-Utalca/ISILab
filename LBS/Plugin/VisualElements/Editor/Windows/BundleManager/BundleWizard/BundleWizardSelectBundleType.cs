using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSelectBundleType : VisualElement, IBundleWizardTab
{
    private LBSCustomTextField nameField;
    private LBSCustomRadioButtonGroup layersType;
    
    public BundleBuilder Builder { get; set; }


    public BundleWizardSelectBundleType(): base()
    {

        nameField = new LBSCustomTextField("New Main Bundle’s Name: ");
        nameField.value = "New Main Bundle";
        layersType = new LBSCustomRadioButtonGroup("Select the Layer for your new Main Bundle:", new List<string>()
        {
            "Interior Layer",
            "Exterior Layer",
            "Population Layer"
        });
        this.Add(nameField);
        this.Add(layersType);
        
        
        
        //nameField.RegisterValueChangedCallback(evt => )
    }

    public void Init()
    {
        //Debug.Log("Init: " + GetType().Name);
        nameField.value = "";
        //layersType.SelectChoice(0);
        layersType.value = 0;
        //Debug.Log("Radio button group value: " + layersType.value);
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }

    public void Step()
    {
        Builder.bundleName = nameField.value;
        Builder.layerType = layersType.choices.ToList()[layersType.value];

    }

    public void Revert()
    {
        Builder.bundleName = "";
        Builder.layerType = null;
        Debug.Log("Builder data:\n\n" + Builder.ToString());
    }
}


