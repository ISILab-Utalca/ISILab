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

        public class CharacteristicContainer
        {
            //BundleWizardSetCharacteristMenu menu;

            public LBSCustomListView listView;
            public BundleManagerListGroup listGroup = new();
            public HashSet<Type> selectedCharacteristics = new();
            public Dictionary<Type, bool> initializationValues = new();
            public String titleLable;
            /// <summary>
            /// Dictionary containing every characteristic element displayed, accessed through a characteristic type.
            /// </summary>
            public Dictionary<Type, BundleWizardCharacteristicElement> elements = new();

            public CharacteristicContainer(BundleWizardSetCharacteristMenu menu, ContainerKey key, string titleLable)
            {
                //this.menu = menu;

                listGroup.style.flexShrink = 0;
                listGroup.style.flexGrow = 1;

                listGroup.GetListViewRef(out listView);

                listGroup.TitleText = titleLable;
                
                listView.fixedItemHeight = 32;
                listView.itemsSource = menu.allCharacteristics;
                listView.makeItem = () => new BundleWizardCharacteristicElement();
                listView.bindItem = (item, i) =>
                {
                    // Se pasa la referencia directa del contenedor para evitar problemas con la variable global charKey
                    menu.BindItem(item, i, this);
                };
                listView.unbindItem = (item, i) =>
                {
                    menu.UnbindItem(item, i, this);
                };
                this.titleLable = titleLable;

            }

            public void Reset()
            {
                selectedCharacteristics.Clear();
                initializationValues.Clear();
                elements.Clear(); // Limpiar referencias a elementos visuales al resetear
            }
        }

        public BundleWizardSetCharacteristMenu() : base()
        {
            allCharacteristics = new List<Type>(Reflection.GetAllSubClassOf<LBSCharacteristic>());

            ContainerKey
                mainKey = ContainerKey.Main,
                childrenKey = ContainerKey.Children;
            containers.Add(mainKey, new CharacteristicContainer(this, mainKey, "Main Bundle's Characteristics"));
            containers.Add(childrenKey, new CharacteristicContainer(this, childrenKey, "Sub Bundle's Characteristics"));

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

            foreach (Type cha in mainChars)
            {
                if (!containers[ContainerKey.Main].initializationValues.ContainsKey(cha))
                    containers[ContainerKey.Main].initializationValues.Add(cha, true);

                containers[ContainerKey.Main].selectedCharacteristics.Add(cha);
                //containers[ContainerKey.Main].elements[cha].Toggle.value = true;
            }
            foreach (Type cha in childChars)
            {
                if (!containers[ContainerKey.Children].initializationValues.ContainsKey(cha))
                    containers[ContainerKey.Children].initializationValues.Add(cha, true);

                containers[ContainerKey.Children].selectedCharacteristics.Add(cha);
                //containers[ContainerKey.Children].elements[cha].Toggle.value = true;
            }
        }

        public void BindItem(VisualElement item, int i, CharacteristicContainer currentCon)
        {
            int ind = i;
            Type charType = allCharacteristics[ind];
            var charElement = item as BundleWizardCharacteristicElement;

            // Actualizamos la referencia del elemento visual para el tipo actual
            currentCon.elements[charType] = charElement;

            charElement.CharLabel.text = charType.Name;

            //test Tooltip
            charElement.QuestionTooltip.tooltip = charType.Name;

            bool exclusiveChar = LBSCharacteristic.IsExclusive(charType, out List<List<Type>> exclusivenessGroups);

            Action<bool> toggleCallback = value =>
            {
                if (value)
                {
                    currentCon.selectedCharacteristics.Add(charType);
                    // Uncheck all incompatible toggles
                    if (exclusiveChar)
                    {
                        var toExclude = exclusivenessGroups.SelectMany(g => g).Except(new[] { charType });
                        foreach (Type excluded in toExclude)
                        {
                            if (currentCon.selectedCharacteristics.Contains(excluded))
                            {
                                currentCon.selectedCharacteristics.Remove(excluded);
                                // Solo intentamos actualizar visualmente si el elemento está bindeado actualmente
                                if (currentCon.elements.TryGetValue(excluded, out var excludedElement))
                                {
                                    excludedElement.Toggle.SetValueWithoutNotify(false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    currentCon.selectedCharacteristics.Remove(charType);
                }
            };

            charElement.toggleCallback = evt =>
            {
                toggleCallback.Invoke(evt.newValue);
            };

            charElement.EnableToggleCallback();

            // Sincronizar el estado del toggle con los datos actuales
            bool isSelected = currentCon.selectedCharacteristics.Contains(charType);
            charElement.Toggle.SetValueWithoutNotify(isSelected);
        }

        public void UnbindItem(VisualElement item, int i, CharacteristicContainer currentCon)
        {
            var charElement = item as BundleWizardCharacteristicElement;
            Type charType = allCharacteristics[i];

            charElement.DisableToggleCallback();

            // Limpiamos la referencia al elemento visual ya que va a ser reciclado
            if (currentCon.elements.ContainsKey(charType) && currentCon.elements[charType] == charElement)
            {
                currentCon.elements.Remove(charType);
            }
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