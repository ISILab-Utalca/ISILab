using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
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
        private readonly Dictionary<GraphElement, Vector2> OgPositions = new();

        #endregion

        #region FIELDS

        private ISILab.LBS.Components.Blueprint selectedBlueprint;
        private CaptureInArea _captureArea;
        private PrintInArea _printArea;
        private List<BlueprintEntry> entries = new();
        #endregion

        #region PROPERTIES
        public CaptureInArea CaptureManipulator
        {
            set
            {
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

        public ISILab.LBS.Components.Blueprint SelectedBlueprint
        {
            get => selectedBlueprint;
            set
            {
                selectedBlueprint = value;
                if(selectedBlueprint is null)
                {
                    if(PrintArea is not null)
                    {
                        PrintArea.BlueprintToPrint = null;
                        PrintArea.ClearPreview();
                    }
                  
                }
                else
                {
                    if (_printArea is not null)
                    {
                        ToolKit.Instance.SetActive(_printArea.GetType());
                        _printArea.BlueprintToPrint = SelectedBlueprint;
                    }
                    CreateBlueprintPreviewLayer();
                }
            }
        }

        #endregion

        #region STATIC METHODS
        private static BlueprintPanel _instance;
        private LBSLayer previewLayer;
        private Vector2Int OffsetGrid;

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

            deleteButton.clicked += DeleteSelectedBlueprint;
            captureButton.clicked += CaptureBlueprint;

            pickingMode = PickingMode.Ignore;
            LoadBlueprints();
        }
        #endregion

        #region METHODS

        public void LoadBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:ISILab.LBS.Components.Blueprint");
            scrollView.Clear();
            entries.Clear();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ISILab.LBS.Components.Blueprint bp = AssetDatabase.LoadAssetAtPath<ISILab.LBS.Components.Blueprint>(path);

                if (bp == null)
                    continue;

                BlueprintEntry entry = new BlueprintEntry();
                entry.Blueprint = bp;
                // deselect all entries when one of them is selected
                entry.OnSelect += () =>
                {
                    foreach (var entry in entries) entry.SetSelected(false);
                    SelectedBlueprint = bp;
                };

                Debug.Log($"Entry Created: {bp.BlueprintName}  InstanceID: {bp.GetInstanceID()}");

                scrollView.Add(entry);
                entries.Add(entry);
            }
        }


        private void CaptureBlueprint() => _captureArea?.DoCapture();

        private void DeleteSelectedBlueprint()
        {
            if (selectedBlueprint == null) return;

            string assetPath = AssetDatabase.GetAssetPath(SelectedBlueprint);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("Cannot delete — not an asset on disk");
                return;
            }

            SelectedBlueprint = null;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            LoadBlueprints();
        }

        private void CaptureComplete(
            List<BlueprintStorable> capturedObjects, 
            Texture2D captureImage, 
            Vector2Int size)
        {
            if (capturedObjects is null && !capturedObjects.Any())
            
            {
                LBSMainWindow.MessageNotify(
                    new Core.Settings.LBSLog("There are no valid objects to capture in that area of the graph.", 
                    LogType.Error));
                return;
            }
            

            ISILab.LBS.Components.Blueprint newInstance =
                ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();

            newInstance.StorableData = capturedObjects;
            newInstance.PreviewImage = captureImage;
            newInstance.Size = size;

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

            LBSMainWindow.MessageNotify(
                new Core.Settings.LBSLog("Blueprint capture, a new Blueprint Scriptable Object has been stored.",
                LogType.Log));
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

            print.OnManipulationMove = RedrawSelectedBlueprint;
        }

        internal void CreateBlueprintPreviewLayer()
        {
      
            ClearVisualization();

            if (SelectedBlueprint is null) return;
            previewLayer = new LBSLayer();

            MainView.Instance.AddContainer(previewLayer);
            CloneRefs.Start();
            foreach (BlueprintStorable storable in selectedBlueprint.StorableData)
            {
                if (!storable.Data.Any()) continue;
                foreach (BlueprintData entry in storable.Data)
                {
                    if (entry.Object is LBSModule module)
                    {
                        LBSModule mClone = (LBSModule)module.Clone();
                        previewLayer.AddModule(mClone);
                    }
                    if (entry.Object is LBSBehaviour behaviour)
                    {
                        LBSBehaviour bhClone = (LBSBehaviour)behaviour.Clone();
                        previewLayer.AddBehaviour(bhClone);
                    }
                }
            }
            CloneRefs.End();
        }

        internal void RedrawSelectedBlueprint(Vector2Int offset)
        {
            if (previewLayer is null)
            {
                return;
            }
            if (selectedBlueprint is null)
            {
                return;
            }

            DrawManager.Instance.UpdateLayer(previewLayer);

            Vector2Int gridSpace = previewLayer.ToFixedPosition(offset);
            if (OffsetGrid == gridSpace) return;
            OffsetGrid = gridSpace;

            CreateOffsetLayer();
        }

        private void CreateOffsetLayer()
        {
            CreateBlueprintPreviewLayer();
            // passing coordinates in grid space
            // update all positions on the preview layer data(cloned safe for mutation)
            previewLayer.OffsetLayer(OffsetGrid);
            DrawManager.Instance.RedrawLayer(previewLayer);
        }

        private void ClearVisualization()
        {
            if (previewLayer == null)
                return;

            MainView.Instance.RemoveContainer(previewLayer);
            previewLayer = null;
        }

        #endregion

    }
}
