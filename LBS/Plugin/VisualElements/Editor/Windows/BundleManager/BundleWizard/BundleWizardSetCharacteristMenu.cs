using ISILab.Commons.Utility;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard
{
    [UxmlElement]
    public partial class BundleWizardSetCharacteristMenu : VisualElement, IBundleWizardTab
    {
        private LBSCustomTextField nameField;

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

            int iterator = 0;

            mainCharListView = new LBSCustomListView();
            mainCharListView.itemsSource = allCharacteristics;
            mainCharListView.makeItem = () => new BundleWizardCharacteristicElement();
            mainCharListView.bindItem = (item, i) =>
            {
                iterator = i;
                var charElement = item as BundleWizardCharacteristicElement;
                //charElement.CharLabel.text = (mainCharListView.itemsSource[i] as Type).Name;
                charElement.CharLabel.text = allCharacteristics[i].Name;

                charElement.Toggle.RegisterValueChangedCallback(ToggleCallback);
            };
            mainCharListView.unbindItem = (item, i) =>
            {
                (item as BundleWizardCharacteristicElement).Toggle.UnregisterValueChangedCallback(ToggleCallback);
            };

            Add(mainCharListView);

            void ToggleCallback(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                    selectedCharacteristics.Add(allCharacteristics[iterator]);
                else
                    selectedCharacteristics.Remove(allCharacteristics[iterator]);
                string s = "Characteristics List:\n";
                foreach (var cha in selectedCharacteristics)
                    s += "- " + cha.Name + "\n";
                Debug.Log(s);
            }
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
}

