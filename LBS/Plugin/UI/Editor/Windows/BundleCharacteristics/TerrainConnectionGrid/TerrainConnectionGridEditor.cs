using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleCharacteristics.TerrainConnectionGrid
{
    [LBSCustomEditor("Weights", typeof(LBSTerrainConnectionGrid))]
    public class TerrainConnectionGridEditor : LBSCustomEditor
    {
        private Button testButton;
        private Button openGridEditorWindow;
        private static TerrainConnectionGridEditorWindow gridEditorWindow;

        public TerrainConnectionGridEditor()
        {

        }
        public TerrainConnectionGridEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);

        }

        public override void SetInfo(object _paramTarget)
        {
            this.target = _paramTarget;
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TerrainConnectionGridEditor");
            visualTree.CloneTree(this);


            testButton = this.Q<Button>("TestButton");
            testButton.clicked += TestGridAsset;
            openGridEditorWindow = this.Q<Button>("OpenGridEditorButton");
            openGridEditorWindow.clicked += OpenGridEditorWindow;

            return this;
        }

        private void OpenGridEditorWindow()
        {
            if (gridEditorWindow)
                gridEditorWindow.Close();

            gridEditorWindow = ScriptableObject.CreateInstance<TerrainConnectionGridEditorWindow>();
            gridEditorWindow.connectionGridTarget = target as LBSTerrainConnectionGrid;

            gridEditorWindow.Show();
        }

        private void TestGridAsset()
        {
            var testTarget = target as LBSTerrainConnectionGrid;
            if (target == null)
            {
                Debug.Log("nothing to test"); return;
            }
            else
            {
                string saveReport = "TESTING GRID";
                Debug.Log(saveReport);
                if (testTarget.ColorPalette.Count == 0)
                {
                    Debug.Log("no colors");
                }

                for(int i=0; i<testTarget.ColorPalette.Count; i++)
                {
                    Debug.Log("COLOR for " + testTarget.ColorPaletteID[i] + ": " + testTarget.ColorPalette[i]);
                }

                foreach (AssetConnectionGrid grid in testTarget.GridList)
                {
                    saveReport = "";
                    saveReport += "Saved Grid: ";
                    for (int i = 0; i < grid.TerrainFlag.Length; i++)
                    {
                        saveReport += grid.TerrainFlag[i] + " | ";
                    }
                    saveReport += " | Asset: " + grid.AssetReference.obj;
                    Debug.Log(saveReport);
                }
            }

            
        }
    }
}