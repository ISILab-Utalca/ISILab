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
        private TemplateContainer dragAndDropContainer;
        private VisualElement dragAndDropWindow;
        private DragAndDropWindow.DragAndDropManipulator manipulator;

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
        private void GetObjects(List<Object> objects)
        {
            var prefabs = new List<GameObject>(objects.Select(o => o as GameObject)).RemoveEmpties();

            Bundle bundle = SetBundle(prefabs);

            bundleContainers.Add(new BundleManagerWindow.BundleContainer(bundle));

            bundleList.Rebuild();
            bundleList.RefreshItems();

            string s = "";
            prefabs.ForEach(o => s += AssetDatabase.GetAssetPath(o) + "\n");
            Debug.Log(s);
        }

        /// <summary>
        /// Creates a child bundle instance, sets its Assets and do basic configuration.
        /// </summary>
        /// <param name="prefabs"> Prefabs to add as bundle assets. </param>
        /// <returns> The configured bundle instance. </returns>
        private Bundle SetBundle(List<GameObject> prefabs)
        {
            Bundle bundle = ScriptableObject.CreateInstance<Bundle>();
            prefabs.ForEach(pref => bundle.AddAsset(pref));
            bundle.BundleName = prefabs[0].name;

            Builder.GetBundleConfiguration(ref bundle, Builder.layerType);

            return bundle;
        }

        public void Init()
        {
            //Debug.Log("Init: " + GetType().Name);
            Debug.Log("Builder data:\n\n" + Builder.ToString());
            try
            {
                dragAndDropContainer = this.Q<TemplateContainer>();
                dragAndDropWindow = dragAndDropContainer.Q<VisualElement>("DragAndDrop");
                manipulator = new DragAndDropWindow.DragAndDropManipulator(dragAndDropContainer, GetObjects);

                bundleListGroup = this.Q<BundleManagerListGroup>("NewBundles");
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
            Builder.tempBundles.AddRange(TempBundles);
            Builder.newSubBundles.AddRange(TempBundles);

            //manipulator.target = null;
        }

        public void StepBack()
        {
            
        }

        public void Revert()
        {
            manipulator.target = null;

            //tempBundles.Clear();
            bundleContainers.Clear();

            Builder.tempBundles.Clear();
            Builder.newSubBundles.Clear();

            Builder.objects.Clear();
            Debug.Log("Builder data:\n\n" + Builder.ToString());
        }
    }
}

