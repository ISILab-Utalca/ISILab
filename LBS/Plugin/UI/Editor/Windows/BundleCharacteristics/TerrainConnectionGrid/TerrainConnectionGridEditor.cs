using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleCharacteristics.TerrainConnectionGrid
{
    
    [LBSCustomEditor("Weights", typeof(LBSTerrainConnectionGrid))]
    /// <summary>
    /// The "entrance" interface class for the <b>Terrain Connection Grid Editor</b>. It ties a visual element to the characteristic to allow modification via
    /// the main <b>Terrain Connection Grid Editor Window</b>. </br>
    /// The <b>Terrain Connection Grid Editor</b> Visual Element contains two functionalities inside: Opening the <b>Terrain Connection Grid Editor Window</b>
    /// and running an internal information test for the <b>LBS Terrain Connection Grid</b>, if available.
    /// </summary>
    public class TerrainConnectionGridEditor : LBSCustomEditor
    {
        private Button testButton;
        private Button openGridEditorWindow;
        private static TerrainConnectionGridEditorWindow gridEditorWindow;

        /// <summary>
        /// Empty constructor. Unused.
        /// </summary>
        public TerrainConnectionGridEditor()
        {

        }

        /// <summary>
        /// Main constructor utilized. </br>
        /// It automatically creates this visual element on configuration.
        /// </summary>
        public TerrainConnectionGridEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        /// <summary>
        /// Connects the visual element to be created with its target object.
        /// </summary>
        /// <param name="_paramTarget">The object picked up by the constructor. Always an <b>LBS Terrain Connection Grid</b> in this case.</param>
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
                    saveReport += "ID: "+ grid.AssetReference.id +" | Asset: " + grid.AssetReference.obj;
                    Debug.Log(saveReport);
                }
            }

            
        }
    }
}