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
        private readonly LBSCustomButton deleteButton;
        private readonly LBSCustomButton captureButton;
        private readonly LBSCustomEnumField addModeField;
        private readonly ScrollView scrollView;
        private readonly LBSCustomToggleField autoCaptureToggle;
        private readonly LBSCustomToggleField overwriteToggle;
        #endregion

        #region CONSTS

        private const string baseName = "Blueprint_";
        private const string folderPath = "Assets/Blueprints";

        private const string overwriteTooltip =
            "<b>Enabled</b>\n" +
            "    When data overlaps between two layers, the blueprint data overwrites the existing layer.\n\n" +
            "<b>Disabled</b>\n" +
            "    When data overlaps between two layers, that data is omited.\n\n";

        private const string autoCaptureTooltip =
            "<b>Enabled</b>\n" +
            "    Select an area in the graph to create a Blueprint.\n\n" +
            "<b>Disabled</b>\n" +
            "    1. Select an area in the graph\n" +
            "    2. Press the <Capture Button>";

        private const string addModeTooltip =
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

        private Dictionary<blueprintAddMode, BlueprintGenerator> generators; 

        private readonly List<BlueprintEntry> entries = new();
        private readonly List<LBSLayer> previewLayers = new();

        private Vector2Int OffsetGrid;

        [SerializeField]
        private blueprintAddMode activeAddMode = blueprintAddMode.ByName;
        [SerializeField]
        private bool overwrite = false;
        [SerializeField]
        private bool autoCapture = false;

        #endregion

        #region PROPERTIES
        public static BlueprintPanel Instance => _instance;
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
        private static VisualTreeAsset visualTreeAsset;
        private static BlueprintPanel _instance;
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
            autoCaptureToggle.SetValueWithoutNotify(autoCapture);

            overwriteToggle = this.Q<LBSCustomToggleField>("OverwriteToggle");
            overwriteToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (_captureArea != null) overwrite = evt.newValue;
            });
            overwriteToggle.tooltip = overwriteTooltip;
            overwriteToggle.SetValueWithoutNotify(overwrite);

            addModeField = this.Q<LBSCustomEnumField>("AddMode");
            addModeField.tooltip = addModeTooltip;
            addModeField.RegisterCallback<ChangeEvent<Enum>>(evt =>
            {
                if (evt.newValue is blueprintAddMode mode) activeAddMode = mode;
            });
            addModeField.SetValueWithoutNotify(activeAddMode);


            deleteButton.clicked += DeleteSelectedBlueprint;
            captureButton.clicked += CaptureBlueprint;

            generators = new()
            {
                { blueprintAddMode.ByName, new BlueprintGeneratorByName() },
                { blueprintAddMode.ByType, new BlueprintGeneratorByType() },
                { blueprintAddMode.New, new BlueprintGeneratorNew() }
            };

            LoadBlueprints();

            pickingMode = PickingMode.Ignore;
 
        }
        #endregion

        #region METHODS

        #region BINDINGS

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
            captureTool.tool.OnDeselect += ClearPreviews;
            captureTool.tool.OnSelect += capture.ClearArea;

            printTool.tool.OnDeselect += print.ClearPreview;
            printTool.tool.OnDeselect += ClearPreviews;
            printTool.tool.OnSelect += print.ClearPreview;

            print.OnManipulationMove = RedrawSelectedBlueprint;
            print.DoPrintBlueprint = CreateBlueprintLayers;

            autoCaptureToggle.SetValueWithoutNotify(_captureArea.AutoCapture);
        }

        public void LoadBlueprints()
        {
            scrollView.Clear();
            entries.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ISILab.LBS.Components.Blueprint");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ISILab.LBS.Components.Blueprint bp = AssetDatabase.LoadAssetAtPath<ISILab.LBS.Components.Blueprint>(path);

                if (bp == null || !bp.IsValid()) continue;

                BlueprintEntry bpEntry = new BlueprintEntry();
                bpEntry.Blueprint = bp;
                // deselect all entries when one of them is selected
                bpEntry.OnSelect += () =>
                {
                    foreach (var entry in entries) entry.SetSelected(false);
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
        #endregion

        #region BLUEPRINT MANIPULATOR METHODS

        private void CaptureBlueprint() => _captureArea?.DoCapture();
        private void CreateBlueprintLayers()
        {
            CreateBlueprintPreviewLayer();
            if (previewLayers.Count == 0) return;
            LoadedLevel loadedLevel = LBSController.CurrentLevel;
            generators[activeAddMode].CreateBlueprint(new List<LBSLayer>(previewLayers), loadedLevel);
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
            if (AssetDatabase.DeleteAsset(assetPath)) LoadBlueprints();

        }

        private void CaptureComplete(
            List<LBSLayer> capturedLayers,
            Texture2D captureImage,
            Vector2Int size)
        {
            if (capturedLayers == null || capturedLayers.Count == 0)
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


        #endregion

        #region Preview

        internal void CreateBlueprintPreviewLayer()
        {
            ClearPreviews();

            if (SelectedBlueprint == null) return;
            Vector2Int mainAnchor = GetBlueprintAnchor();

            foreach (var layer in SelectedBlueprint.Layers)
            {
                if (layer.Clone() is LBSLayer newPreview)
                {
                    if (newPreview is IBlueprintable blueprintable)
                    {
                        blueprintable.SetPosition(mainAnchor, OffsetGrid);
                    }
                    previewLayers.Add(newPreview);
                    DrawManager.Instance.RedrawLayer(newPreview);
                }
            }

        }

        private Vector2Int GetBlueprintAnchor()
        {
            Vector2Int mainAnchor = new Vector2Int(int.MaxValue, int.MinValue);
            foreach (var layer in SelectedBlueprint.Layers)
            {
                if (layer is IBlueprintable blueprintable)
                {
                    Vector2Int anchor = blueprintable.GetAnchor();
                    if (anchor.x < mainAnchor.x) mainAnchor.x = anchor.x;
                    if (anchor.y > mainAnchor.y) mainAnchor.y = anchor.y;
                }

            }

            return mainAnchor;
        }

        internal void RedrawSelectedBlueprint(Vector2Int offset)
        {
            if (previewLayers.Count == 0 || selectedBlueprint == null) return;

            var selectedLayer = LBSMainWindow.Instance._selectedLayer;
            if(selectedLayer == null) return;

            Vector2Int gridSpace = selectedLayer.ToFixedPosition(offset);
            if (OffsetGrid == gridSpace) return;
            
            OffsetGrid = gridSpace;
            CreateBlueprintPreviewLayer();
        }

        private void ClearPreviews()
        {
            if (previewLayers.Count == 0) return;
            foreach (var previewLayer in previewLayers) 
                DrawManager.Instance.ClearLayer(previewLayer);
            previewLayers.Clear();
        }
        #endregion

        #endregion

    }
}
