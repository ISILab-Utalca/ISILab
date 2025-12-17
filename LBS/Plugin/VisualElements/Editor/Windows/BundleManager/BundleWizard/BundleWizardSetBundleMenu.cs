using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleManagerWindow;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard
{
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

            SearchAllBundles();
            AddCurrentBundles();

            bundleListCurrent = this.Q<BundleManagerListGroup>("CurrentBundles");
            bundleListCurrent.SetBundleListViewItem<BundleWizardElement>(
                out listCurrent,
                "CurrentBundles",
                bundleContainersCurrent,
                itemHeight: 40
                );
            bundleListSameLayerOrphan = this.Q<BundleManagerListGroup>("SameLayerOrphanBundles");
            bundleListSameLayerOrphan.SetBundleListViewItem<BundleWizardElement>(
                out listSameLayerOrphan,
                "SameLayerOrphanBundles",
                bundleContainersSameLayerOrphan,
                itemHeight: 40
                );
            bundleListSameLayer = this.Q<BundleManagerListGroup>("SameLayerBundles");
            bundleListSameLayer.SetBundleListViewItem<BundleWizardElement>(
                out listSameLayer,
                "SameLayerBundles",
                bundleContainersSameLayer,
                itemHeight: 40
                );
            bundleListNoLayer = this.Q<BundleManagerListGroup>("NoLayerBundles");
            bundleListNoLayer.SetBundleListViewItem<BundleWizardElement>(
                out listNoLayer,
                "NoLayerBundles",
                bundleContainersNoLayer,
                itemHeight: 40
                );
        }

        private void AddCurrentBundles()
        {
            foreach (Bundle b in Builder.newSubBundles)
            {
                bundleContainersCurrent.Add(new BundleManagerWindow.BundleContainer(b));
            }
        }

        private void SearchAllBundles()
        {
            //Clear list
            _allBundles.Clear();
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

