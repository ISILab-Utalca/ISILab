using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Bundle Wizard tab for naming the new main <see cref="Bundle"/> and choosing a layer type.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardSelectBundleType : VisualElement, IBundleWizardTab
    {
        private LBSCustomTextField nameField;
        private LBSCustomRadioButtonGroup layersType;

        public BundleBuilder Builder { get; set; }

        
        public BundleWizardSelectBundleType() : base()
        {

            nameField = new LBSCustomTextField("New Main Bundle’s Name: ");
            nameField.value = "New Main Bundle";
            nameField.RegisterCallback<BlurEvent>(e =>
            {
                nameField.value = nameField.value.Replace(' ', '_');
            });

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

            nameField.Focus();

            //Debug.Log("Radio button group value: " + layersType.value);
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }

        public void Step()
        {
            string choiceName = layersType.choices.ToList()[layersType.value];
            if (string.IsNullOrEmpty(nameField.value))
                nameField.value = $"New{choiceName.Split()[0]}MainBundle";
            Builder.bundleName = nameField.value;
            Builder.layerType = choiceName;

        }

        public void StepBack()
        {
            nameField.Focus();
        }

        public void Revert()
        {
            Builder.bundleName = "";
            Builder.layerType = null;
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }
    }


}

