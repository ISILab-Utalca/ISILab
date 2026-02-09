using ISI_Lab.LBS.Plugin.Components.Bundles;
using ISILab.Commons.Extensions;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents;
using System;
using System.Collections.Generic;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager;


namespace ISILab.LBS.Plugin.UI.Editor.Windows
{
    /// <summary>
    /// Provides a quick way for <see cref="Bundle"/> creation and basic configuration. Displayed as a pop-up in the <see cref="BundleManagerWindow"/>.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardPopup: VisualElement
    {
        private readonly LBSCustomTabView tabView;
    
        private readonly LBSCustomBreadcrumbs breadcrumbs;

        private readonly LBSCustomButton OKButton;
        private readonly LBSCustomButton nextButton;
        private readonly LBSCustomButton backButton;
        private readonly LBSCustomButton cancelButton;

        private int currentStep = 0;
        /// <summary>
        /// Visible names for each Wizard tab.
        /// </summary>
        private readonly string[] breadcrumbLabels = new string []
        {
            "New Main Bundle",
            "Convert Prefabs",
            "Assign Bundles",
            "Add Characteristics",
            "Summary"
        };

        /// <summary>
        /// Wizard tabs as visual elements. Uses breadcrumbs labels as keys.
        /// </summary>
        private Dictionary<string, VisualElement> tabs = new Dictionary<string, VisualElement>();
    

        private int CurrentStep 
        { 
            get => currentStep; 
            set 
            { 
                currentStep = value;
                if (tabView is not null) 
                    tabView.selectedTabIndex = value; 
            } 
        }

        /// <summary>
        /// Is the Wizard on the last tab?
        /// </summary>
        private bool InFinalStep => CurrentStep == breadcrumbLabels.Length - 1;

        /// <summary>
        /// Current tab's breadcrumb name.
        /// </summary>
        private string CurrentBreadcrumb => breadcrumbLabels[CurrentStep]; 

        /// <summary>
        /// Current tab as an interface.
        /// </summary>
        private IBundleWizardTab CurrentWizardTab => tabs[CurrentBreadcrumb] as IBundleWizardTab;
    
        /// <summary>
        /// Called when the Bundle Manager window is created.
        /// </summary>
        public BundleWizardPopup()
        {
            VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(nameof(BundleWizardPopup));
            vta?.CloneTree(this);

            tabView = this.Q<LBSCustomTabView>("TabView");
            breadcrumbs = this.Q<LBSCustomBreadcrumbs>("WizardBreadcrumbs");
        
            OKButton = this.Q<LBSCustomButton>("OK");
            OKButton.clicked += OK;
            OKButton.clicked += () => OnAnyButtonClicked(OKButton);

            nextButton = this.Q<LBSCustomButton>("Next");
            nextButton.clicked += Next;
            nextButton.clicked += () => OnAnyButtonClicked(nextButton);

            backButton = this.Q<LBSCustomButton>("Back");
            backButton.clicked += Back;
            backButton.clicked += () => OnAnyButtonClicked(backButton);

            cancelButton = this.Q<LBSCustomButton>("Cancel");
            cancelButton.clicked += Cancel;
            cancelButton.clicked += () => OnAnyButtonClicked(cancelButton);

            ToggleNextButton(true);

            #region Local functions

            void Back()
            {
                CurrentWizardTab.Revert();

                if (CurrentStep <= 0)
                {
                    Cancel();
                    return;
                }

                CheckIfLeavingFinalStep();

                CurrentStep--;
                breadcrumbs.PopItem();

                CurrentWizardTab.StepBack();
            }

            void OK()
            {
                CheckIfLeavingFinalStep();
                CurrentWizardTab.Step();
                CurrentWizardTab.Builder.TryBuild();
                this.SetDisplay(false);
                CleanUp();
            }

            void Next()
            {
                CurrentWizardTab.Step();
                CurrentStep++;
                breadcrumbs.PushItem(CurrentBreadcrumb);
                CurrentWizardTab.Init();
                if (currentStep == 1)
                    CurrentWizardTab.Builder.SaveBundleFlag();
                if (InFinalStep)
                    ToggleNextButton(false);
            }

            void Cancel()
            {
                CheckIfLeavingFinalStep();
                this.SetDisplay(false);
                CleanUp();
            }

            void ToggleNextButton(bool showNext)
            {
                nextButton.SetDisplay(showNext);
                OKButton.SetDisplay(!showNext);
            }

            void CheckIfLeavingFinalStep()
            {
                if (InFinalStep)
                    ToggleNextButton(true);
            }

            void OnAnyButtonClicked(Button button)
            {
                //Debug.Log(button.text);
                //Debug.Log("Current Step: " + CurrentStep);
            }

            #endregion
        }

        /// <summary>
        /// Called when 'New Main Bundle' option is selected from the Bundle Manager.
        /// </summary>
        public void Init()
        {
            // Names of each tab visual element in PopUp UXML file
            var tabNames = new[] { "SelectBundleTypeMenu", "SetAssetsMenu", "SetBundleMenu", "SetCharacteristicsMenu", "SummaryMenu" };
            // There is the same number of  display names and names visual element.
            Assert.IsTrue(breadcrumbLabels.Length == tabNames.Length);

            // The Bundle Builder is created and its reference is assigned to each Wizard tab.
            BundleBuilder builder = new BundleBuilder();
            for (int i = 0; i < tabNames.Length; i++)
            {
                VisualElement value = this.Q<VisualElement>(tabNames[i]);
                tabs.Add(breadcrumbLabels[i], value);
                (value as IBundleWizardTab).Builder = builder;
            }

            breadcrumbs.PushItem(CurrentBreadcrumb);
            CurrentWizardTab.Init();
        }

        //void OnTabChanged()
        //{
        //    foreach (VisualElement tab in tabs.Values)
        //    {
        //        tab.SetDisplay(false);
        //    }
        //    tabs[CurrentBreadcrumb].SetDisplay(true);
        //    
        //    string s = "Tabs Display:\n\n";
        //    foreach (VisualElement tab in tabs.Values)
        //    {
        //        s += tab.GetDisplay() + "\n";
        //    }
        //    Debug.Log(s);
        //}

        /// <summary>
        /// Cleans up the Wizard when its closed.
        /// </summary>
        void CleanUp()
        {
            try
            {
                while (CurrentStep > 0)
                {
                    CurrentWizardTab.Revert();
                    CurrentStep--;
                    breadcrumbs.PopItem();
                }
                breadcrumbs.PopItem();
            }
            finally
            {
                tabs.Clear();
            }
        }
    }

    /// <summary>
    /// Class meant to collect data entered by the user through the Wizard, and create a main <see cref="Bundle"/> asset.
    /// </summary>
    public class BundleBuilder
    {
        public string bundleName { get; set; }
        public string layerType { get; set; }
        public BundleFlags layerTypeFlag { get; set; }

        public List<List<GameObject>> objects { get; private set; } = new();
    
        //public List<Bundle> tempBundles { get; private set; } = new();
        public List<Bundle> newSubBundles { get; private set; } = new();

        public List<Bundle> newAssignBundles { get; private set; } = new();
     
        public List<Type> mainCharacteristics { get; private set; } = new();
        public List<Type> childrenCharacteristics { get; private set; } = new();

        public BundleBuilder() { }

        /// <summary>
        /// Sets base bundle parameters based on the selected layer type.
        /// </summary>
        /// <param name="bundle"> Reference of the bundle whose parameters are setted. </param>
        /// <param name="layerType"> Layer type selected for the bundle. </param>
        public void GetBundleConfiguration(ref Bundle bundle, string layerType)
        {
            switch (layerType)
            {
                case "Interior Layer":
                    bundle.LayerContentFlags    = BundleFlags.Interior;
                    //bundle.Type                 = Bundle.TagType.Structural;
                    bundle.Color                = default;
                    break;

                case "Exterior Layer":
                    bundle.LayerContentFlags    = BundleFlags.Exterior;
                    //bundle.Type                 = Bundle.TagType.Structural;
                    bundle.Color                = default;
                    break;

                case "Population Layer":
                    bundle.LayerContentFlags    = BundleFlags.Population;
                    //bundle.Type                 = Bundle.TagType.Element;
                    bundle.Color                = new Color().RandomColorHSV();
                    break;

                default:
                    bundle.LayerContentFlags    = default;
                    //bundle.Type                 = default;
                    bundle.Color                = default;
                    break;
            }
        }

        /// <summary>
        /// Creates a main <see cref="Bundle"/> asset and its children, assigning their corresponding assets and characteristics.
        /// </summary>
        public void TryBuild()
        {
            Bundle main = ScriptableObject.CreateInstance<Bundle>();
            GetBundleConfiguration(ref main, layerType);
            main = BundleMenuItem.CreateBundleWithInstance(main, bundleName);
            for(int i = 0; i < mainCharacteristics.Count; i++)
            {
                main.AddCharacteristic(Activator.CreateInstance(mainCharacteristics[i]) as LBSCharacteristic);
            }
            for(int i = 0; i < newSubBundles.Count; i++)
            {
                newSubBundles[i] = BundleMenuItem.CreateBundleWithInstance(newSubBundles[i], newSubBundles[i].BundleName);
                for(int j = 0; j < childrenCharacteristics.Count; j++)
                {
                    newSubBundles[i].AddCharacteristic(Activator.CreateInstance(childrenCharacteristics[j]) as LBSCharacteristic);
                }
                main.AddChild(newSubBundles[i]);
            }

            TryAssign(main);
        }

        public void TryAssign(Bundle mainBundle)
        {
            foreach (Bundle b in newAssignBundles)
            {
                mainBundle.AddChild(b); 
            }
        }

        public override string ToString()
        {
            string s = "> Bundle Name:\t" + bundleName + "\n";
            s += "> Layer type:\t" + layerType + "\n\n";

            s += "> Objects:\n";
            objects.ForEach(obj => obj.ForEach(o => s += "\t" + o.ToString() + " | "));
            s += "\n\n";

            s += "> Sub Bundles:\n";
            newSubBundles.ForEach(sb => s += "\t" + sb.BundleName + " | ");
            s += "\n\n";

            s += "> Main Characteristics:\n";
            mainCharacteristics.ForEach(c => s += "\t" + c.Name + " | ");
            s += "\n\n";

            s += "> Children Characteristics:\n";
            childrenCharacteristics.ForEach(c => s += "\t" + c.Name + " | ");
            s += "\n\n";

            return s;
        }

        public void SaveBundleFlag()
        {
            switch (layerType)
            {
                case "Interior Layer":
                    layerTypeFlag = BundleFlags.Interior;
                    break;

                case "Exterior Layer":
                    layerTypeFlag = BundleFlags.Exterior;
                    break;

                case "Population Layer":
                    layerTypeFlag = BundleFlags.Population;
                    break;

                default:
                    layerTypeFlag = default;
                    break;
            }
        }
    }
}
