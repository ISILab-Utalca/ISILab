using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.VisualElements;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

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

        private ISILab.LBS.Components.Blueprint selectedBlueprint;
        private CaptureInArea _captureArea;
        private PrintInArea _printArea;

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

        public PrintInArea PrintArea
        {
            get => _printArea;
            set => _printArea = value;
        }

        public ISILab.LBS.Components.Blueprint SelectedBlueprint { get => selectedBlueprint; }

        #endregion

        #region STATIC METHODS
        private static BlueprintPanel _instance;
        private LBSLayer previewLayer;

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

        public void LoadBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:ISILab.LBS.Components.Blueprint");
            scrollView.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ISILab.LBS.Components.Blueprint bp = AssetDatabase.LoadAssetAtPath<ISILab.LBS.Components.Blueprint>(path);

                if (bp == null)
                    continue;

                BlueprintEntry entry = new BlueprintEntry();
                entry.Blueprint = bp;
                entry.RegisterCallback<MouseDownEvent>(OnEntryMouseDown);

                Debug.Log($"Entry Created: {bp.BlueprintName}  InstanceID: {bp.GetInstanceID()}");

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

        internal void OnActivate(ChangeEvent<bool> evt)
        {

            DisplayStyle display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            style.display = display;

            SetupAreaTool<CaptureInArea>(mani =>
                CaptureManipulator = mani);

            SetupAreaTool<PrintInArea>(mani =>
                PrintArea = mani);

            if (display == DisplayStyle.Flex) LoadBlueprints();

            void SetupAreaTool<T>(System.Action<T> assign) where T : class
            {
                KeyValuePair<Type, (LBSTool, ToolButton)> toolEntry = ToolKit.Instance.GetTool(typeof(T));
                if (toolEntry.Key is null)
                    return;

                LBSTool tool = toolEntry.Value.Item1;
                if (tool?.Manipulator is not T manipulator)
                    return;

                assign(manipulator);

                ToolButton button = toolEntry.Value.Item2;
                if (button == null)
                    return;

                button.style.display = display;
                LBSFocusHighlight.Highlight(button);
            }
        }

        internal void Bind(ToolKit ToolKit, CaptureInArea capture, PrintInArea print)
        {
            DisplayStyle visibility = style.display.value;

            CaptureManipulator = capture;
            PrintArea = print;

            ToolKit.DisplayManipulator(capture.GetType(), visibility);
            ToolKit.DisplayManipulator(print.GetType(), visibility);

            capture.OnManipulationStart += capture.ClearArea;
            print.OnManipulationStart += print.ClearPreview;
            capture.OnManipulationEnd += capture.ClearArea;
            print.OnManipulationEnd += print.ClearPreview;
            print.OnManipulationMove += RedrawSelectedBlueprint;
        }

        private void OnEntryMouseDown(MouseDownEvent evt)
        {
            VisualElement element = evt.currentTarget as VisualElement;
            BlueprintEntry blueprintEntry = element as BlueprintEntry;
            if (blueprintEntry == null || blueprintEntry.Blueprint == null) return;


            selectedBlueprint = blueprintEntry.Blueprint;

            if (previewLayer != null)
            { 
                previewLayer.ClearEvents();
                previewLayer = null; 
            }

            if (_printArea is not null)
            {
                _printArea.BlueprintToPrint = selectedBlueprint;
                ToolKit.Instance.SetActive(_printArea.GetType());
            }

            Debug.Log($"Clicked: {blueprintEntry.Blueprint.BlueprintName}  InstanceID: {blueprintEntry.Blueprint.GetInstanceID()}");

            CreateBlueprintPreviewLayer();

        }

        private void CreateBlueprintPreviewLayer()
        {
            if(selectedBlueprint is null) return;

            previewLayer = new LBSLayer();
            foreach (BlueprintStorable storable in selectedBlueprint.StorableData)
            {
                if (!storable.Data.Any()) continue;
                foreach (BlueprintData entry in storable.Data)
                {
                    if (entry.Object is LBSModule module)
                        previewLayer.AddModule(module);

                    if (entry.Object is LBSBehaviour behaviour)
                        previewLayer.AddBehaviour(behaviour);

                    if (entry.Object is LBSAssistant assistant)
                        previewLayer.AddAssistant(assistant);
                }
            }

            if (PrintArea == null) return;
            RedrawSelectedBlueprint(PrintArea.StartPosition);
        }

        internal void RedrawSelectedBlueprint(Vector2Int Position)
        {
            if (previewLayer is null) return;

            foreach (BlueprintStorable storable in selectedBlueprint.StorableData)
            {
                if (!storable.Data.Any()) continue;
                foreach (BlueprintData entry in storable.Data)
                {
                    DrawManager.Instance.DrawSingleComponent(entry.Object, previewLayer);
                    //Debug.LogWarning("Drawing:" + entry.GetType().Name + "At " + Position.ToString());

                }
            }
        }

        #endregion

    }
}
