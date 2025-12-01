using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard
{
    [UxmlElement]
    public partial class BundleWizardSetBundleMenu : LBSComplexVisualElement, IBundleWizardTab
    {
        private LBSCustomTextField nameField;
        TabView tabView;

        public BundleBuilder Builder { get; set; }

        public BundleWizardSetBundleMenu() : base()
        {
            GetVisualTreeForThis();

            nameField = new LBSCustomTextField("New Bundle Collection’s Name: ");
            this.Add(nameField);

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

