using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
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

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public enum blueprintAddMode
    {
        ByType,
        ByName,
        New
    }

    [UxmlElement]
    public partial class BlueprintPanel : VisualElement
    {
        #region VIEW ELEMENTS
        LBSCustomButton deleteButton;
        LBSCustomButton captureButton;
        LBSCustomEnumField addModeField;
        ScrollView scrollView;
        LBSCustomToggleField autoCaptureToggle;
        #endregion

        #region CONSTS

        const string baseName = "Blueprint_";
        const string folderPath = "Assets/Blueprints";
        const string autoCaptureTooltip =
            "<b>Enabled</b>\n" +
            "    Select an area in the graph to create a Blueprint.\n\n" +
            "<b>Disabled</b>\n" +
            "    1. Select an area in the graph\n" +
            "    2. Press the <Capture Button>";

        private string addModeTooltip =
            "How Blueprints are added to the level.\n\n" +

            "<b>By Type</b>\n" +
            "    Adds the Blueprint to a layer with the same TYPE.\n" +
            "    • if a layer is found → Add the blueprint data on it\n" +
            "    • else → create new layer\n\n" +

            "<b>By Name</b>\n" +
            "    Adds the Blueprint to a layer with the same NAME and TYPE.\n" +
            "    • if a layer is found → Add the blueprint data on it\n" +
            "    • else → create new layer\n\n" +

            "<b>New</b>\n" +
            "    Always creates a NEW layer of the same TYPE.\n"
            ;

        #endregion

        #region FIELDS

        private ISILab.LBS.Components.Blueprint selectedBlueprint;
       
        private CaptureInArea _captureArea;
        private PrintInArea _printArea;

        private List<BlueprintEntry> entries = new();
        private List<LBSLayer> previewLayers = new();
        
        private Vector2Int OffsetGrid;

        [SerializeField]
        private blueprintAddMode activeAddMode = blueprintAddMode.ByName;
        
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
                    ClearPreviews();
                    if (PrintArea is not null)
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
                    // reset offset when picknig a new blueprint so when the first preview is made its not visible
                    OffsetGrid = new Vector2Int(int.MaxValue, int.MinValue);
                    CreateBlueprintPreviewLayer();
                }
            }
        }

        #endregion

        #region STATICS
        static VisualTreeAsset visualTreeAsset;
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

            autoCaptureToggle = this.Q<LBSCustomToggleField>("AutoCaptureToggle");
            autoCaptureToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if(_captureArea!=null) _captureArea.AutoCapture = evt.newValue;
            });
            autoCaptureToggle.tooltip = autoCaptureTooltip;

            addModeField = this.Q<LBSCustomEnumField>("AddMode");
            addModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
            {
                if (evt.newValue is blueprintAddMode mode)
                {
                    activeAddMode = mode;
                }
            });
            addModeField.tooltip = addModeTooltip;

            addModeField.SetValueWithoutNotify(activeAddMode);
            deleteButton.clicked += DeleteSelectedBlueprint;
            captureButton.clicked += CaptureBlueprint;

            pickingMode = PickingMode.Ignore;
            LoadBlueprints();
        }
        #endregion

        #region METHODS

        private void CaptureBlueprint() => _captureArea?.DoCapture();

        public void LoadBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:ISILab.LBS.Components.Blueprint");
            scrollView.Clear();
            entries.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ISILab.LBS.Components.Blueprint bp = AssetDatabase.LoadAssetAtPath<ISILab.LBS.Components.Blueprint>(path);

                if (bp == null || !bp.IsValid())
                    continue;

                BlueprintEntry bpEntry = new BlueprintEntry();
                bpEntry.Blueprint = bp;
                // deselect all entries when one of them is selected
                bpEntry.OnSelect += () =>
                {
                    foreach (var entry in entries)
                    {
                        entry.SetSelected(false);
                    }
       
                    SelectedBlueprint = bpEntry.Blueprint;
                };

                scrollView.Add(bpEntry);
                entries.Add(bpEntry);
            }

            // empty entry the one indicating instruction
            BlueprintEntry defaultEntry = new BlueprintEntry();
            scrollView.Add(defaultEntry);
            defaultEntry.pickingMode = PickingMode.Ignore;

            ClearPreviews();
        }

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

            if (AssetDatabase.DeleteAsset(assetPath))
            {
                LoadBlueprints();  
            }

        }

        private void CaptureComplete(
            List<LBSLayer> capturedLayers, 
            Texture2D captureImage, 
            Vector2Int size)
        {
            if (capturedLayers == null || !capturedLayers.Any())
            {
                LBSMainWindow.MessageNotify(
                    new Core.Settings.LBSLog("There are no valid objects to capture in that area of the graph.", 
                    LogType.Error));
                return;
            }


            ISILab.LBS.Components.Blueprint newInstance =
                ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();

            newInstance.Layers = new List<LBSLayer>(capturedLayers);
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

        internal void Bind(ToolKit ToolKit, (LBSManipulator mani, LBSTool tool) captureTool, (LBSManipulator mani, LBSTool tool) printTool)
        {
            DisplayStyle visibility = style.display.value;

            var capture = captureTool.mani as CaptureInArea;
            var print = printTool.mani as PrintInArea;

            CaptureManipulator = capture;
            PrintArea = print;

            ToolKit.DisplayManipulator(capture.GetType(), visibility);
            ToolKit.DisplayManipulator(print.GetType(), visibility);

            captureTool.tool.OnDeselect += capture.ClearArea; 
            captureTool.tool.OnDeselect+= ClearPreviews;
            captureTool.tool.OnSelect += capture.ClearArea;

            printTool.tool.OnDeselect += print.ClearPreview;
            printTool.tool.OnDeselect += ClearPreviews;
            printTool.tool.OnSelect += print.ClearPreview;

            print.OnManipulationMove = RedrawSelectedBlueprint;

            autoCaptureToggle.SetValueWithoutNotify(_captureArea.AutoCapture);
        }

        internal void CreateBlueprintPreviewLayer()
        {
      
            ClearPreviews();

            if (SelectedBlueprint is null) return;

            var mainAnchor = new Vector2Int(int.MaxValue, int.MinValue);


            Vector2Int layesAnchor = new Vector2Int(int.MaxValue, int.MinValue);
            foreach (var layer in SelectedBlueprint.Layers)
            {
                if (layer is IBlueprintable blueprintable)
                {
                    Vector2Int anchor = blueprintable.GetAnchor();
                    if (anchor.x < mainAnchor.x) mainAnchor.x = anchor.x;
                    if (anchor.y > mainAnchor.y) mainAnchor.y = anchor.y;
                }
            
            }

            foreach (var layer in SelectedBlueprint.Layers)
            {
                LBSLayer newPreview = layer.Clone() as LBSLayer;
                if(newPreview != null)
                {
                    if (newPreview is IBlueprintable blueprintable)
                    {
                        blueprintable.SetPosition(mainAnchor, OffsetGrid);
                    }
                    previewLayers.Add(newPreview);
                }
               
            }

            foreach (var layer in previewLayers) 
            {
                DrawManager.Instance.RedrawLayer(layer);
            }
        }

        internal void RedrawSelectedBlueprint(Vector2Int offset)
        {
            if (!previewLayers.Any() || selectedBlueprint == null) return;

            var selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if(selectedLayer == null) return;

            Vector2Int gridSpace = selectedLayer.ToFixedPosition(offset);
            if (OffsetGrid != gridSpace)
            {
                OffsetGrid = gridSpace;

                CreateBlueprintPreviewLayer();
            }

        }

        private void ClearPreviews()
        {
            if (!previewLayers.Any()) return;

            foreach (var previewLayer in previewLayers)
                DrawManager.Instance.ClearLayer(previewLayer);

            previewLayers.Clear();
        }

        #endregion

    }
}
