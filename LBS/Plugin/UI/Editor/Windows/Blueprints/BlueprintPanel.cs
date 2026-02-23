using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintPanel : VisualElement
    {
        #region VIEW ELEMENTS
        LBSCustomButton deleteButton;
        LBSCustomButton captureButton;
        ScrollView scrollView;
        
        static VisualTreeAsset visualTreeAsset;
        #endregion

        #region CONSTS

        const string baseName = "Blueprint_";
        const string folderPath = "Assets/Blueprints";

        #endregion

        #region FIELDS

        public object selectedArea;
        public ISILab.LBS.Components.Blueprint selectedBlueprint;
        private CaptureInArea _captureArea;

        #endregion

        #region PROPERTIES
        public CaptureInArea CaptureManipulator
        {
            set
            {

                if (_captureArea == value)
                {
                    _captureArea.CaptureComplete = CaptureComplete;
                }

                _captureArea = value;
                if (_captureArea != null) 
                {
                    _captureArea.CaptureComplete = CaptureComplete;
            }
            }
        }

        public ISILab.LBS.Components.Blueprint SelectedBlueprint { get => selectedBlueprint; }

        #endregion

        #region STATIC METHODS
        private static BlueprintPanel _instance;
        public static BlueprintPanel Instance => _instance;

        #endregion

        #region CONSTRUCTORS
        public BlueprintPanel() : base()
        {
            _instance = this;

            visualTreeAsset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("BlueprintPanel");
            visualTreeAsset.CloneTree(this);
            name = "BlueprintPanel";

            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            captureButton = this.Q<LBSCustomButton>("CaptureButton");
            scrollView = this.Q<ScrollView>("BlueprintScrollView");

            deleteButton.clicked += OnDeleteButtonClicked;
            captureButton.clicked += OnCaptureButtonClicked;

            pickingMode = PickingMode.Ignore;
            LoadBlueprints();
        }
        #endregion

        #region METHODS

        void LoadBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:ISILab.LBS.Components.Blueprint");
            scrollView.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                var bp = AssetDatabase.LoadAssetAtPath<ISILab.LBS.Components.Blueprint>(path);

                if (bp == null)
                    continue;

                BlueprintEntry entry = new BlueprintEntry();
                entry.Blueprint = bp;
                entry.RegisterCallbackOnce<MouseDownEvent>((evt) =>
                {
                    selectedBlueprint = entry.Blueprint;
                    Debug.Log("BP:" + selectedBlueprint.BlueprintName);
                });


                scrollView.Add(entry);
            }
        }

        public void OnCaptureButtonClicked()
        {
            bool validCapture = _captureArea != null && _captureArea.DoCapture();
            if (validCapture)
            {
                LBSMainWindow.MessageNotify(new Core.Settings.LBSLog("Blueprint capture, a new Blueprint Scriptable Object has been stored.", LogType.Log));
            }
            else
            {
                LBSMainWindow.MessageNotify(new Core.Settings.LBSLog("There are no valid objects to capture in that area of the graph.", LogType.Error));  
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (selectedBlueprint == null) return;

            UnityEngine.Object.DestroyImmediate(selectedBlueprint);
            selectedBlueprint = null;
        }

        private void CaptureComplete()
        {
            List<BlueprintStorable> capturedObjects = _captureArea?.CapturedBlueprintData;
            if (capturedObjects == null || !capturedObjects.Any()) return;

            ISILab.LBS.Components.Blueprint newInstance =
                ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();

            newInstance.StorableData = capturedObjects;
            newInstance.PreviewImage = _captureArea.CaptureBlueprintImage;

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Blueprints");
            }

            int index = 0;
            string assetPath;

            do
            {
                string fileName = baseName + index;
                assetPath = $"{folderPath}/{fileName}.asset";
                index++;
            }
            while (System.IO.File.Exists(assetPath));

            newInstance.BlueprintName = baseName + (index - 1);
            
            AssetDatabase.CreateAsset(newInstance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"New Blueprint <{newInstance.BlueprintName}> created at: {assetPath}");
            
            LoadBlueprints();
        }


        #endregion

    }
}
