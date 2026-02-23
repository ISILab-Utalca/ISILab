using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    [UxmlElement]
    public partial class BundleWizardSummaryMenu : LBSComplexVisualElement, IBundleWizardTab
    {
        public BundleBuilder Builder { get; set; }

        //SUMMARY DATA
        private int newBundles;
        private int assignedBundles;
        private string infoSummary;
        private string path;

        private Label toChange;

        public void Init()
        {
            toChange = this.Q<Label>("LabelInfo");

            newBundles = Builder.newSubBundles.Count;
            assignedBundles = Builder.newAssignBundles.Count;
            //path = 

            SetInfoSummary();

            toChange.text = infoSummary;
        }

        private void SetInfoSummary()
        {
            infoSummary = LBSSettings.Instance.paths.bundleFolderPath +" \n\n" + (assignedBundles).ToString()+ "\n\n" + newBundles.ToString()+ "\n\n" + (assignedBundles-newBundles).ToString();
        }

        public void Revert()
        {
            
        }

        public void Step()
        {
            
        }

        public void StepBack()
        {
            
        }
    }
}

