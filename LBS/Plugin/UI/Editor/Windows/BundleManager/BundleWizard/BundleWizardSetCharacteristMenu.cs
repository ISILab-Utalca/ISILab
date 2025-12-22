using ISILab.Commons.Utility;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Bundle Wizard tab for choosing characteristics.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardSetCharacteristMenu : VisualElement, IBundleWizardTab
    {
        public enum ContainerKey { Main, Children }

        private readonly List<Type> allCharacteristics;

        private readonly Dictionary<ContainerKey, CharacteristicContainer> containers = new();
        private ContainerKey charKey;

        CharacteristicContainer Con => containers[charKey];

        public BundleBuilder Builder { get; set; }

        class CharacteristicContainer
        {
            //BundleWizardSetCharacteristMenu menu;

            public LBSCustomListView listView;
            public BundleManagerListGroup listGroup = new();
            public HashSet<Type> selectedCharacteristics = new();
            /// <summary>
            /// Dictionary containing every characteristic element displayed, accessed through a characteristic type.
            /// </summary>
            public Dictionary<Type, BundleWizardCharacteristicElement> elements = new();

            public CharacteristicContainer(BundleWizardSetCharacteristMenu menu, ContainerKey key)
            {
                //this.menu = menu;

                listGroup.style.flexGrow = 1;

                listGroup.GetListViewRef(out listView);

                listView.itemsSource = menu.allCharacteristics;
                listView.makeItem = () => new BundleWizardCharacteristicElement();
                listView.bindItem = (item, i) =>
                {
                    menu.charKey = key;
                    menu.BindItem(item, i);
                };
                listView.unbindItem = (item, i) =>
                {
                    menu.charKey = key;
                    menu.UnbindItem(item, i);
                };
            }

            public void Reset()
            {
                selectedCharacteristics.Clear();
            }
        }

        public BundleWizardSetCharacteristMenu() : base()
        {
            allCharacteristics = new List<Type>(Reflection.GetAllSubClassOf<LBSCharacteristic>());

            ContainerKey 
                mainKey = ContainerKey.Main,
                childrenKey = ContainerKey.Children;
            containers.Add(mainKey, new CharacteristicContainer(this, mainKey));
            containers.Add(childrenKey, new CharacteristicContainer(this, childrenKey));

            style.flexDirection = FlexDirection.Row;
            Add(containers[mainKey].listGroup);
            Add(containers[childrenKey].listGroup);
        }

        public void Init()
        {
            //Debug.Log("Init: " + GetType().Name);
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }

        public void BindItem(VisualElement item, int i)
        {
            var charElement = item as BundleWizardCharacteristicElement;
            if (!Con.elements.ContainsKey(allCharacteristics[i]))
                Con.elements.Add(allCharacteristics[i], charElement);
            //charElement.CharLabel.text = (mainCharListView.itemsSource[i] as Type).Name;
            charElement.CharLabel.text = allCharacteristics[i].Name;

            bool exclusiveChar = LBSCharacteristic.IsExclusive(allCharacteristics[i], out List<List<Type>> exclusivenessGroups);

            var con = Con;
            charElement.toggleCallback = evt =>
            {
                //Assert.AreNotEqual(evt.previousValue, evt.newValue);
                if (evt.newValue)
                {
                    con.selectedCharacteristics.Add(allCharacteristics[i]);
                    // Uncheck all incompatible toggles
                    if (exclusiveChar)
                    {
                        foreach (Type excluded in exclusivenessGroups.SelectMany(g => g).Except(new[] { allCharacteristics[i] }))
                        {
                            con.elements[excluded].Toggle.SetValueWithoutNotify(false);
                            con.selectedCharacteristics.Remove(excluded);
                        }
                    }
                }
                else con.selectedCharacteristics.Remove(allCharacteristics[i]);

                string s = "Characteristics List:\n";
                foreach (var cha in con.selectedCharacteristics)
                    s += "- " + cha.Name + "\n";
                Debug.Log(s);
            };

            charElement.EnableToggleCallback();

            charElement.Toggle.SetValueWithoutNotify(Con.selectedCharacteristics.Contains(allCharacteristics[i]));
        }

        public void UnbindItem(VisualElement item, int i)
        {
            (item as BundleWizardCharacteristicElement).DisableToggleCallback();
        }

        public void Step()
        {
            Builder.mainCharacteristics.AddRange(containers[ContainerKey.Main].selectedCharacteristics);
            Builder.childrenCharacteristics.AddRange(containers[ContainerKey.Children].selectedCharacteristics);
        }

        public void StepBack()
        {

        }

        public void Revert()
        {
            //Debug.Log("Builder data:\n\n" + Builder.ToString());
            containers[ContainerKey.Main].Reset();
            containers[ContainerKey.Children].Reset();
            Builder.mainCharacteristics.Clear();
            Builder.childrenCharacteristics.Clear();
        }
    }
}
