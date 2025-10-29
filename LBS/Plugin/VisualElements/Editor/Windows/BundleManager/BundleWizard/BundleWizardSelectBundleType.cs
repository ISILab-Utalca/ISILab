using System.Collections.Generic;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class BundleWizardSelectBundleType : VisualElement
{
    private LBSCustomTextField nameField;
    private LBSCustomRadioButtonGroup layersType;
    
    
    public BundleWizardSelectBundleType(): base()
    {

        nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
        layersType = new LBSCustomRadioButtonGroup("Select the Layer for your New Bundle Collection:", new List<string>()
        {
            "Interior Layer",
            "Exterior Layer",
            "Population Layer"
        });
        this.Add(nameField);
        this.Add(layersType);
        
        layersType.SelectChoice(0);
        
        
    }
}
