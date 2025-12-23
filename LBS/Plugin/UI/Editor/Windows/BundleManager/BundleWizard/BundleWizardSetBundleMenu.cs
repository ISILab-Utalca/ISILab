using System;
using System.Collections.Generic;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
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
        private List<BundleManagerWindow.BundleContainer> bundleContainersCurrent = new();
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
            Debug.Log("Builder data:\n\n" + Builder.ToString());

            currentBundleFlags = Builder.layerTypeFlag;
            Console.WriteLine(currentBundleFlags.ToString());

            CleanAllLists();
            SearchAllBundles();
            AddCurrentBundles();

            rootVisualElement = panel.visualTree;

            //BUNDLE LISTS INIT

            //BundleList Current Bundles to be added through BundleWizard
            bundleListCurrent = this.Q<BundleManagerListGroup>("CurrentBundles");
            bundleListCurrent.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listCurrent,
                "CurrentBundles",
                bundleContainersCurrent,
                itemHeight: 40
                );
            bundleListCurrent.SetExpandButtonSetting(rootVisualElement, "CurrentBundles", listCurrent);
            
            //BundleList Orphan (Same Layer)
            bundleListSameLayerOrphan = this.Q<BundleManagerListGroup>("SameLayerOrphanBundles");
            bundleListSameLayerOrphan.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listSameLayerOrphan,
                "SameLayerOrphanBundles",
                bundleContainersSameLayerOrphan,
                itemHeight: 40,
                deleteIconBool: false
                );
            bundleListSameLayerOrphan.SetExpandButtonSetting(rootVisualElement, "SameLayerOrphanBundles", listSameLayerOrphan);
            
            //BundleList Same Layer
            bundleListSameLayer = this.Q<BundleManagerListGroup>("SameLayerBundles");
            bundleListSameLayer.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listSameLayer,
                "SameLayerBundles",
                bundleContainersSameLayer,
                itemHeight: 40,
                deleteIconBool: false
                );
            bundleListSameLayer.SetExpandButtonSetting(rootVisualElement, "SameLayerBundles", listSameLayer, false);
            
            //BundleList No Layer
            bundleListNoLayer = this.Q<BundleManagerListGroup>("NoLayerBundles");
            bundleListNoLayer.SetBundleListViewItem<UI.Editor.Windows.BundleManager.BundleWizard.BundleWizardElement>(
                out listNoLayer,
                "NoLayerBundles",
                bundleContainersNoLayer,
                itemHeight: 40,
                deleteIconBool: false
                );
            bundleListNoLayer.SetExpandButtonSetting(rootVisualElement, "NoLayerBundles", listNoLayer, false);
        }

        private void AddCurrentBundles()
        {
            Debug.Log(Builder.newSubBundles.Count.ToString());

            foreach (Bundle b in Builder.newSubBundles)
            {
                bundleContainersCurrent.Add(new BundleManagerWindow.BundleContainer(b));
            }
        }

        private void SearchAllBundles()
        {
            _allBundles = LBSAssetsStorage.Instance.Get<Bundle>();

            // Normal bundles
            foreach (Bundle b in _allBundles)
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
            Debug.Log("BundleManagerWindow updated");
        }

        void CleanAllLists()
        {
            //Clear lists
            _allBundles.Clear();

            bundleContainersCurrent.Clear();
            bundleContainersNoLayer.Clear();
            bundleContainersSameLayer.Clear();
            bundleContainersSameLayerOrphan.Clear();

            if (listCurrent != null) listCurrent.Clear();
            if (listNoLayer != null) listNoLayer.Clear();
            if (listSameLayer != null) listSameLayer.Clear();
            if (listSameLayerOrphan != null) listSameLayerOrphan.Clear();
        }

        public void Step()
        {
            //throw new System.NotImplementedException();
        }

        public void StepBack()
        {

        }

        public void Revert()
        {
            Debug.Log("Builder data:\n\n" + Builder.ToString());
            //throw new System.NotImplementedException();
        }
    }
}

