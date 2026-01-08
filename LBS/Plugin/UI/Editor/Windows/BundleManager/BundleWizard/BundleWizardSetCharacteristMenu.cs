using ISILab.Commons.Utility;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
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
            public Dictionary<Type, bool> initializationValues = new();  
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
                initializationValues.Clear();
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

            //return;

            List<Type> mainInteriorChars = new() { typeof(LBSMainInteriorBundle) };
            List<Type> childInteriorChars = new() { typeof(LBSTagsCharacteristic) };
            List<Type> mainExteriorChars = new() { typeof(LBSMainExteriorBundle), typeof(LBSDirectionedGroup), typeof(LBSNavigableTags), typeof(WFCPresetsCharacteristic) };
            List<Type> childExteriorChars = new() { typeof(LBSDirection) };
            List<Type> mainPopulationChars = new() { typeof(LBSMainPopulationBundle) };
            List<Type> childPopulationChars = new() { typeof(LBSTagsCharacteristic) };

            List<Type> mainChars = new();
            List<Type> childChars = new();

            switch (Builder.layerTypeFlag)
            {
                case BundleFlags.Interior:
                    mainChars = mainInteriorChars;
                    childChars = childInteriorChars;
                    break;

                case BundleFlags.Exterior:
                    mainChars = mainExteriorChars;
                    childChars = childExteriorChars;
                    break;

                case BundleFlags.Population:
                    mainChars = mainPopulationChars;
                    childChars = childPopulationChars;
                    break;

                default:

                    break;
            }

            foreach(Type cha in mainChars)
            {
                containers[ContainerKey.Main].initializationValues.Add(cha, true);
                //containers[ContainerKey.Main].elements[cha].Toggle.value = true;
            }
            foreach(Type cha in childChars)
            {
                containers[ContainerKey.Children].initializationValues.Add(cha, true);
                //containers[ContainerKey.Children].elements[cha].Toggle.value = true;
            }
        }

        public void BindItem(VisualElement item, int i)
        {
            int ind = i;
            var charElement = item as BundleWizardCharacteristicElement;
            //if (!Con.elements.ContainsKey(allCharacteristics[ind]))
            //{
            //}
            Con.elements.Clear();
            Con.elements.Add(allCharacteristics[ind], charElement);
            //else
            //{
            //    try
            //    {
            //        Assert.AreEqual(Con.elements[allCharacteristics[ind]], charElement);
            //    }
            //    catch(Exception ex)
            //    {
            //        Debug.LogException(ex);
            //    }
            //}
            //charElement.CharLabel.text = (mainCharListView.itemsSource[i] as Type).Name;
            charElement.CharLabel.text = allCharacteristics[ind].Name;

            bool exclusiveChar = LBSCharacteristic.IsExclusive(allCharacteristics[ind], out List<List<Type>> exclusivenessGroups);
            //exclusiveChar = false;

            var con = Con;

            Action<bool> toggleCallback = value =>
            {
                if (value)
                {
                    con.selectedCharacteristics.Add(allCharacteristics[ind]);
                    // Uncheck all incompatible toggles
                    if (exclusiveChar)
                    {
                        foreach (Type excluded in exclusivenessGroups.SelectMany(g => g).Except(new[] { allCharacteristics[ind] }))
                        {
                            try
                            {
                                string e = "";
                                foreach (var el in con.elements)
                                    e += "> " + el.Key.Name + "\n";

                                Debug.Log(e + "\n" + excluded);
                                
                                con.elements[excluded].Toggle.SetValueWithoutNotify(false);
                                con.selectedCharacteristics.Remove(excluded);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }
                else con.selectedCharacteristics.Remove(allCharacteristics[ind]);

                string s = "Characteristics List:\n";
                foreach (var cha in con.selectedCharacteristics)
                    s += "- " + cha.Name + "\n";
                Debug.Log(s);
            };

            charElement.toggleCallback = evt =>
            {
                //Assert.AreNotEqual(evt.previousValue, evt.newValue);
                toggleCallback.Invoke(evt.newValue);
            };

            charElement.EnableToggleCallback();

            //// Predetermined values
            if (Con.initializationValues.ContainsKey(allCharacteristics[i]))
            {
                bool initValue = Con.initializationValues[allCharacteristics[i]];
                Con.elements[allCharacteristics[i]].Toggle.SetValueWithoutNotify(initValue);
                //toggleCallback.Invoke(initValue);
                //Con.initializationValues.Remove(allCharacteristics[i]);
            }

            //Debug.Log("SetValueWithoutNotify()");
            //EditorApplication.delayCall += () => 
            //charElement.Toggle.SetValueWithoutNotify(con.selectedCharacteristics.Contains(allCharacteristics[ind]));
        }

        public void UnbindItem(VisualElement item, int i)
        {
            var charElement = item as BundleWizardCharacteristicElement;
            //charElement.Toggle.SetValueWithoutNotify(false);
            charElement.DisableToggleCallback();
            //charElement.toggleCallback = null;
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
