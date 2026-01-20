using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Bundle Wizard tab for choosing additional existent bundles from the project to use as child bundles.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardSetBundleMenu : LBSComplexVisualElement, IBundleWizardTab
    {
        private List<Bundle> _allBundles = new();

        private BundleFlags currentBundleFlags;

        private ListView listCurrent;
        private ListView listSameLayerOrphan;
        private ListView listSameLayer;
        private ListView listNoLayer;
        private BundleManagerListGroup bundleListCurrent;
        private BundleManagerListGroup bundleListSameLayerOrphan;
        private BundleManagerListGroup bundleListSameLayer;
        private BundleManagerListGroup bundleListNoLayer;
        private List<BundleManagerWindow.BundleContainer> bundleContainersShow = new();
        private List<BundleManagerWindow.BundleContainer> bundleContainersTemp = new();
        private List<BundleManagerWindow.BundleContainer> bundleContainersSameLayerOrphan = new();
        private List<BundleManagerWindow.BundleContainer> bundleContainersSameLayer = new();
        private List<BundleManagerWindow.BundleContainer> bundleContainersNoLayer = new();

        private LBSCustomTextField nameField;
        TabView tabView;

        VisualElement rootVisualElement;

        public BundleBuilder Builder { get; set; }

        public BundleWizardSetBundleMenu() : base()
        {
            GetVisualTreeForThis();

            //nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
            //this.Add(nameField);
        }

        public void Init()
        {
            //Debug.Log("Init: " + GetType().Name);
            //Debug.Log("Builder data:\n\n" + Builder.ToString());

            currentBundleFlags = Builder.layerTypeFlag;
            Console.WriteLine(currentBundleFlags.ToString());

            CleanAllLists();
            SearchAllBundles();
            AddCurrentBundles();
            OnEnable();

            rootVisualElement = panel.visualTree;

            //BUNDLE LISTS INIT

            //BundleList Current Bundles to be added through BundleWizard
            bundleListCurrent = this.Q<BundleManagerListGroup>("CurrentBundles");
            bundleListCurrent.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listCurrent,
                "CurrentBundles",
                bundleContainersShow,
                itemHeight: 40,
                buttonFunc: BundleWizardElement.Func.REMOVE
                );
            bundleListCurrent.SetExpandButtonSetting(rootVisualElement, "CurrentBundles", listCurrent, true, true);
            
            //BundleList Orphan (Same Layer)
            bundleListSameLayerOrphan = this.Q<BundleManagerListGroup>("SameLayerOrphanBundles");
            bundleListSameLayerOrphan.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listSameLayerOrphan,
                "SameLayerOrphanBundles",
                bundleContainersSameLayerOrphan,
                itemHeight: 40,
                buttonFunc: BundleWizardElement.Func.ADD
                );
            bundleListSameLayerOrphan.SetExpandButtonSetting(rootVisualElement, "SameLayerOrphanBundles", listSameLayerOrphan);
            
            //BundleList Same Layer
            bundleListSameLayer = this.Q<BundleManagerListGroup>("SameLayerBundles");
            bundleListSameLayer.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listSameLayer,
                "SameLayerBundles",
                bundleContainersSameLayer,
                itemHeight: 40,
                buttonFunc: BundleWizardElement.Func.ADD
                );
            bundleListSameLayer.SetExpandButtonSetting(rootVisualElement, "SameLayerBundles", listSameLayer, false);
            
            //BundleList No Layer
            bundleListNoLayer = this.Q<BundleManagerListGroup>("NoLayerBundles");
            bundleListNoLayer.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listNoLayer,
                "NoLayerBundles",
                bundleContainersNoLayer,
                itemHeight: 40,
                buttonFunc: BundleWizardElement.Func.ADD
                );
            bundleListNoLayer.SetExpandButtonSetting(rootVisualElement, "NoLayerBundles", listNoLayer, false);
        }

        private void AddCurrentBundles()
        {
            Debug.Log(Builder.newSubBundles.Count.ToString());

            foreach (Bundle b in Builder.newSubBundles)
            {
                bundleContainersShow.Add(new BundleManagerWindow.BundleContainer(b));
                //esta la agregé hace poco, debo testear
                bundleContainersTemp.Add(new BundleManagerWindow.BundleContainer(b));
            }
        }

        private void SearchAllBundles()
        {
            _allBundles = LBSAssetsStorage.Instance.Get<Bundle>();

            // Normal bundles
            foreach (Bundle b in _allBundles)
            {
                AddBundleToCorrectList(b);
            }
            Debug.Log("BundleManagerWindow updated");
        }

        void CleanAllLists()
        {
            //Clear lists
            _allBundles.Clear();

            bundleContainersShow.Clear();
            bundleContainersTemp.Clear();
            bundleContainersNoLayer.Clear();
            bundleContainersSameLayer.Clear();
            bundleContainersSameLayerOrphan.Clear();

            if (listCurrent != null) listCurrent.Clear();
            if (listNoLayer != null) listNoLayer.Clear();
            if (listSameLayer != null) listSameLayer.Clear();
            if (listSameLayerOrphan != null) listSameLayerOrphan.Clear();
        }

        void AddBundleToCorrectList(Bundle b)
        {
            //same layer orphan bundles
            if (b.ChildsBundles.Count <= 0 && (b.Parent() == null) &&
               (b.LayerContentFlags & currentBundleFlags) == currentBundleFlags)
            {
                bundleContainersSameLayerOrphan.Add(new BundleManagerWindow.BundleContainer(b));
            }
            //same layer bundles
            else if (b.ChildsBundles.Count <= 0 && (b.Parent() != null) &&
                (b.LayerContentFlags & currentBundleFlags) == currentBundleFlags)
            {
                bundleContainersSameLayer.Add(new BundleManagerWindow.BundleContainer(b));
            }
            //no layer bundles
            else if (b.ChildsBundles.Count <= 0 &&
                (b.LayerContentFlags & currentBundleFlags) == BundleFlags.None)
            {
                bundleContainersNoLayer.Add(new BundleManagerWindow.BundleContainer(b));
            }
        }

        void OnEnable()
        {
            BundleManagerListGroup.OnRequestMove -= HandleIncomingElement;
            // Nos suscribimos al evento
            BundleManagerListGroup.OnRequestMove += HandleIncomingElement;
        }

        void OnDisable()
        {
            // Importante: desuscribirse para evitar errores de memoria
            BundleManagerListGroup.OnRequestMove -= HandleIncomingElement;
        }

        void HandleIncomingElement(object element)
        {
            if (element is BundleManagerWindow.BundleContainer bundleContainer)
            if(!bundleContainersTemp.Contains(bundleContainer))
            {
                bundleContainersTemp.Add(bundleContainer);
                bundleContainersShow.Add(bundleContainer);
            }
            else if(bundleContainersTemp.Contains(bundleContainer))
            {
                if (Builder.newSubBundles.Contains(bundleContainer.GetMainBundle()))
                    return;
                AddBundleToCorrectList(bundleContainer.GetMainBundle());
            }

            listCurrent.Rebuild();
            listCurrent.RefreshItems();
            listNoLayer.Rebuild();
            listNoLayer.RefreshItems();
            listSameLayer.Rebuild();
            listSameLayer.RefreshItems();
            listSameLayerOrphan.Rebuild();
            listSameLayer.RefreshItems();

        }

        public void Step()
        {
            Builder.newAssignBundles.AddRange(bundleContainersTemp.Select(bc => bc.GetMainBundle()).ToList());
            OnDisable();
        }

        public void StepBack()
        {
            Builder.newAssignBundles.Clear();
            OnEnable();
        }

        public void Revert()
        {
            Debug.Log("Builder data:\n\n" + Builder.ToString());
            CleanAllLists();
            OnDisable();
        }
    }
}

