using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Extensions;
using ISILab.LBS.Plugin.Components.Bundles;
using Samples.Editor.General;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// Bundle Wizard tab for dragging prefabs from the project to create child bundles.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardSetAssetsMenu : VisualElement, IBundleWizardTab
    {
        //SINGLE BUNDLE
        private TemplateContainer dragAndDropContainerSB;
        private VisualElement dragAndDropWindowSB;
        private DragAndDropWindow.DragAndDropManipulator manipulatorSB;

        //MULTPLE BUNDLES
        private TemplateContainer dragAndDropContainerMB;
        private VisualElement dragAndDropWindowMB;
        private DragAndDropWindow.DragAndDropManipulator manipulatorMB;

        //GENERATED BUNDLE'S LIST
        private ListView bundleList;
        private List<BundleManagerWindow.BundleContainer> bundleContainers = new();
        private BundleManagerListGroup bundleListGroup;

        private List<Bundle> TempBundles => bundleContainers.Select(bc => bc.GetMainBundle()).ToList();

        //private List<GameObject> prefabs = new();
        //private List<GameObject> models = new();

        TabView tabView;

        public BundleBuilder Builder { get; set; }

        public BundleWizardSetAssetsMenu() : base()
        {

        }

        /// <summary>
        /// Callback that retrieves Objects to be used as Assets for a new Bundle.
        /// </summary>
        /// <param name="objects"> Objects passed by a provider (e.g., <see cref="DragAndDropWindow.DragAndDropManipulator"/>) </param>
        private void GetSingleObject(List<Object> objects)
        {
            var prefabs = new List<GameObject>(objects.Select(o => o as GameObject)).RemoveEmpties();

            Bundle bundle = SetSingleBundle(prefabs);

            bundleContainers.Add(new BundleManagerWindow.BundleContainer(bundle));

            bundleList.Rebuild();
            bundleList.RefreshItems();

            string s = "";
            prefabs.ForEach(o => s += AssetDatabase.GetAssetPath(o) + "\n");
            Debug.Log(s);
        }

        /// <summary>
        /// Creates a single child bundle instance given a list of prefabs,
        /// sets its Assets and do basic configuration.
        /// </summary>
        /// <param name="prefabs"> Prefabs to add as bundle assets. </param>
        /// <returns> The configured bundle instance. </returns>
        private Bundle SetSingleBundle(List<GameObject> prefabs)
        {
            Bundle bundle = ScriptableObject.CreateInstance<Bundle>();
            prefabs.ForEach(pref => bundle.AddAsset(pref));
            bundle.BundleName = prefabs[0].name;

            Builder.GetBundleConfiguration(ref bundle, Builder.layerType);

            return bundle;
        }

        /// <summary>
        /// Callback that retrieves Objects to be used as Assets for a new Bundle.
        /// </summary>
        /// <param name="objects"> Objects passed by a provider (e.g., <see cref="DragAndDropWindow.DragAndDropManipulator"/>) </param>
        private void GetMultipleObjects(List<Object> objects)
        {
            var prefabs = new List<GameObject>(objects.Select(o => o as GameObject)).RemoveEmpties();

            List<Bundle> bundles = SetMultipleBundles(prefabs);

            foreach (Bundle b in bundles)
            {
                bundleContainers.Add(new BundleManagerWindow.BundleContainer(b));
            }

            bundleList.Rebuild();
            bundleList.RefreshItems();

            /*
            string s = "";
            prefabs.ForEach(o => s += AssetDatabase.GetAssetPath(o) + "\n");
            Debug.Log(s);
            */
        }

        // Ignacio: Need to test
        /// <summary>
        /// Creates a child bundle instance for each element of a given prefab list's,
        /// sets its Assets and do basic configuration.
        /// </summary>
        /// <param name="prefabs"> Prefabs to make into bundle assets. </param>
        /// <returns> The list of configured bundle instances. </returns>
        private List<Bundle> SetMultipleBundles(List<GameObject> prefabs)
        {
            List<Bundle> bundles = new List<Bundle>();
            Bundle tempBundle;

            foreach (var pref in prefabs)
            {
                tempBundle = ScriptableObject.CreateInstance<Bundle>();
                tempBundle.AddAsset(pref);
                tempBundle.BundleName = pref.name;
                Builder.GetBundleConfiguration(ref tempBundle, Builder.layerType);

                bundles.Add(tempBundle);
            }
            
            return bundles;
        }

        public void Init()
        {
            //Debug.Log("Init: " + GetType().Name);
            //Debug.Log("Builder data:\n\n" + Builder.ToString());
            
            //SINGLE BUNDLE
            try
            {
                dragAndDropContainerSB = this.Q<TemplateContainer>("DragAndDropContainerSB");
                dragAndDropWindowSB = dragAndDropContainerSB.Q<VisualElement>();
                manipulatorSB = new DragAndDropWindow.DragAndDropManipulator(dragAndDropContainerSB, DragAndDropWindow.DragAndDropManipulator.DragAndDropMode.SINGLE_BUNDLE, GetSingleObject);

                bundleListGroup = this.Q<BundleManagerListGroup>("NewBundles");
            }
            catch (System.Exception e) { Debug.LogException(e); }

            //MULTIPLE BUNDLES
            try
            {
                dragAndDropContainerMB = this.Q<TemplateContainer>("DragAndDropContainerMB");
                dragAndDropWindowMB = dragAndDropContainerMB.Q<VisualElement>();
                manipulatorMB = new DragAndDropWindow.DragAndDropManipulator(dragAndDropContainerMB, DragAndDropWindow.DragAndDropManipulator.DragAndDropMode.MULTIPLE_BUNDLES, GetMultipleObjects);
            }
            catch (System.Exception e) { Debug.LogException(e); }

            bundleListGroup = this.Q<BundleManagerListGroup>();
            bundleListGroup.SetBundleListViewItem<BundleWizardElement>(
                out bundleList,
                "NewBundles",
                bundleContainers,
                itemHeight: 40
                );
        }

        public void Step()
        {
            //Builder.objects.Add(new List<GameObject>(prefabs));
            //Builder.tempBundles.AddRange(TempBundles);
            Builder.newSubBundles.AddRange(TempBundles);

            //manipulator.target = null;
        }

        public void StepBack()
        {
            //bundleContainers.Clear();
            //TempBundles.Clear();
            //Builder.tempBundles.Clear();
            Builder.newSubBundles.Clear();
        }

        public void Revert()
        {
            manipulatorSB.target = null;
            manipulatorMB.target = null;

            //tempBundles.Clear();
            bundleContainers.Clear();

            //Builder.tempBundles.Clear();
            Builder.newSubBundles.Clear();

            Builder.objects.Clear();
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }
    }
}

