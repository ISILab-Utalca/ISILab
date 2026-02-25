using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using UnityEditor;
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

        private WarningPanel warningPanel;
        private LBSCustomLabelItem CLIPath, CLITBundles, CLINBundles, CLIABundles;
        private LBSCustomButton goToPath;

        public void Init()
        {
            warningPanel = this.Q<WarningPanel>("warningPanel");

            CLIPath = this.Q<LBSCustomLabelItem>("LBSCustomLabelItemPath");
            CLITBundles = this.Q<LBSCustomLabelItem>("LBSCustomLabelItemTBundles");
            CLINBundles = this.Q<LBSCustomLabelItem>("LBSCustomLabelItemNBundles");
            CLIABundles = this.Q<LBSCustomLabelItem>("LBSCustomLabelItemABundles");

            goToPath = this.Q<LBSCustomButton>("goToPath");
            if (goToPath != null)
            {
                goToPath.clicked += () => { OpenPathInProject(); };
            }

            newBundles = Builder.newSubBundles.Count;
            assignedBundles = Builder.newAssignBundles.Count;

            SetInfoSummary();
        }

        private void SetInfoSummary()
        {
            warningPanel.Text = "New main bundle \"" + Builder.bundleName + "\" succesfully created";

            CLIPath.TextR = LBSSettings.Instance.paths.bundleFolderPath + "/" + Builder.bundleName + ".asset";
            CLITBundles.TextR = (assignedBundles).ToString();
            CLINBundles.TextR = newBundles.ToString();
            CLIABundles.TextR = (assignedBundles - newBundles).ToString();
        }

        private void OpenPathInProject()
        {
            //string folderPath = LBSSettings.Instance.paths.bundleFolderPath + "/" + Builder.bundleName + ".asset";
            string folderPath = LBSSettings.Instance.paths.bundleFolderPath;

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);

            if (obj != null)
            {
                // Focus the Project window
                EditorUtility.FocusProjectWindow();

                // Select the object, which makes the project window jump to that folder
                Selection.activeObject = obj;

                // Optional: Ping the object to highlight it visually
                EditorGUIUtility.PingObject(obj);

                //OPEN FOLDER
                AssetDatabase.OpenAsset(obj);
            }
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

