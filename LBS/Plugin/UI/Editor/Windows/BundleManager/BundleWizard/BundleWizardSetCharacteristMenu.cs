using System;
using System.Collections.Generic;
using ISILab.Commons.Utility;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    [UxmlElement]
    public partial class BundleWizardSetCharacteristMenu : VisualElement, IBundleWizardTab
    {
        //private LBSCustomTextField nameField;

        private LBSCustomListView mainCharListView;

        private List<Type> allCharacteristics;
        private HashSet<Type> selectedCharacteristics;

        public BundleBuilder Builder { get; set; }

        public BundleWizardSetCharacteristMenu() : base()
        {
            //nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
            //this.Add(nameField);
            allCharacteristics = new List<Type>(Reflection.GetAllSubClassOf<LBSCharacteristic>());
            selectedCharacteristics = new HashSet<Type>();


            mainCharListView = new LBSCustomListView();
            mainCharListView.itemsSource = allCharacteristics;
            mainCharListView.makeItem = () => new BundleWizardCharacteristicElement();
            mainCharListView.bindItem = (item, i) =>
            {

                var charElement = item as BundleWizardCharacteristicElement;
                //charElement.CharLabel.text = (mainCharListView.itemsSource[i] as Type).Name;
                charElement.CharLabel.text = allCharacteristics[i].Name;

                charElement.toggleCallback = evt =>
                {
                    //Assert.AreNotEqual(evt.previousValue, evt.newValue);
                    if (evt.newValue)
                        selectedCharacteristics.Add(allCharacteristics[i]);
                    else
                        selectedCharacteristics.Remove(allCharacteristics[i]);
                    string s = "Characteristics List:\n";
                    foreach (var cha in selectedCharacteristics)
                        s += "- " + cha.Name + "\n";
                    Debug.Log(s);
                };

                charElement.EnableToggleCallback();

                charElement.Toggle.SetValueWithoutNotify(selectedCharacteristics.Contains(allCharacteristics[i]));
            };
            mainCharListView.unbindItem = (item, i) =>
            {
                (item as BundleWizardCharacteristicElement).DisableToggleCallback();
            };

            Add(mainCharListView);
        }

        public void Init()
        {
            //Debug.Log("Init: " + GetType().Name);
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }

        public void Step()
        {
            Builder.characteristics.AddRange(selectedCharacteristics);
        }

        public void Revert()
        {
            //Debug.Log("Builder data:\n\n" + Builder.ToString());
            selectedCharacteristics.Clear();
        }
    }
}
