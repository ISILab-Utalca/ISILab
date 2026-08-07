using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.Commons.VisualElements.Editor;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal.Editor;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.AI.Clippy.VisualElements;
using Debug = UnityEngine.Debug;
using InfoToolbar = ISILab.LBS.Plugin.UI.Editor.Panel.InfoToolbar;
using LBSSideBarPanel = ISILab.LBS.Plugin.UI.Editor.Panel.LBSSideBarPanel;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using ToolBarMain = ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar.ToolBarMain;


namespace ISILab.LBS.Editor.Windows
{
    /// <summary>
    /// The General LBS Main Windows
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class LBSMainWindow : ThemeableWindow
    {
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;
#endif


        #region PROPERTIES

        private LBSLevelData levelData
        {
            get => LBS.loadedLevel.data;
            set => LBS.loadedLevel.data = value;
        }

        public LBSLayer SelectedLayer => _selectedLayer;

        #endregion

        #region DATA & STATE

        private LBSLayer _selectedLayer;
        public List<LayerTemplate> LayerTemplates;
        private LBSLevelData backUpData;

        private List<EditorWindow> hangingWindows = new List<EditorWindow>();
        #endregion

        #region MANAGERS

        [NonSerialized]
        private ToolKit toolkit;
        [NonSerialized]
        private DrawManager drawManager;
        [NonSerialized]
        private LBSInspectorPanel inspectorManager;

        #endregion

        #region NOTIFICATIONS

        // Tool notification
        [NonSerialized]
        public Label toolLabel;

        // Warning notification
        public VisualElement WarningNotification => infoToolBar.WarningNotification;
        public Label WarningLabel => infoToolBar.WarningLabel;

        [NonSerialized]
        public NotifierViewer Notifier;
        #endregion

        #region MAIN VIEW

        // Work canvas
        [NonSerialized]
        private MainView mainView;

        // Help overlays
        [NonSerialized]
        private VisualElement helpOverlay;
        [NonSerialized]
        private VisualElement noLayerSign;
        [NonSerialized]
        private LBSSideBarPanel sideBarPanel;

        // Grid position
        public Vector2Int GridPosition;

        #endregion

        #region UI LABELS

        [NonSerialized]
        private Label selectedLabel;
        [NonSerialized]
        public Label PositionLabel;

        #endregion

        #region PANELS & UI SECTIONS VISUALELEMENTS

        [NonSerialized]
        public LayersPanel layerPanel;
        [NonSerialized]
        public Generator3DPanel gen3DPanel;
        [NonSerialized]
        public QuickAssistantPanel quickAssistantPanel;
        [NonSerialized]
        public BlueprintPanel blueprintPanel;
        [NonSerialized]
        public VisualElement extraPanel;
        [NonSerialized]
        public VisualElement bottomPanel;
        [NonSerialized]
        public VisualElement inspectorPanelContainer;

        [NonSerialized]
        private VisualElement helpOverlayAnchor;
        [NonSerialized]
        private ToolBarMain topToolBar;
        [NonSerialized]
        private InfoToolbar infoToolBar;

        [NonSerialized]
        private LBSWaitTaskOverlay taskOverlay;
        public LBSWaitTaskOverlay WaitTaskOverlay => taskOverlay;

        [NonSerialized]
        private ScrollView subPanelScrollView;
        [NonSerialized]
        private Lbesin clippy;

        //[UxmlAttribute]
        [NonSerialized]
        private SplitView splitView;
        //[UxmlAttribute]
        //private LayerInspector layerInspector;


        #endregion

        //private bool isWarpingCursor;

        #region EVENTS

        [NonSerialized]
        public Action onWindowRepaint;
        [NonSerialized]
        public Action onLayerChange;
        #endregion

        #region STATIC METHODS
        public static LBSMainWindow OpenWindow => GetWindow<LBSMainWindow>();
        public static LBSMainWindow Instance => SingletonHelper.Instance;
        #endregion


        private int? randomId;
        private int RandomId => randomId ??= new System.Random().Next(1000, 9999);
        
        public LBSMainWindow() : base()
        {
            //Debug.Log($"[LBSMainWindow] - Constructor - {RandomId}");
        }
        ~LBSMainWindow()
        {
            //Debug.Log($"[LBSMainWindow] - Destructor - {RandomId}");
        }

        private void OnEnable()
        {
            //Debug.Log($"[LBSMainWindow] - OnEnable - {RandomId}");
            if (Instance != null)
            {
                return;
            }
            SingletonHelper.Instance = this;
        }

        private void LoadUITree()
        {
            //Debug.Log($"[LBSMainWindow] - LoadUITree - {RandomId}");
            #region LOAD UI TREE
            //MainWindows UXML 
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSMainWindow");
            visualTree.CloneTree(rootVisualElement);
            #endregion

            splitView = rootVisualElement.Q<SplitView>("SplitView");

            helpOverlayAnchor = rootVisualElement.Q<VisualElement>("HelpOverlayAnchor");

            topToolBar = rootVisualElement.Q<ToolBarMain>("ToolBar");
            infoToolBar = rootVisualElement.Q<InfoToolbar>("InfoToolbar");

            mainView = rootVisualElement.Q<MainView>("MainView");

            noLayerSign = rootVisualElement.Q<VisualElement>("NoLayerSign");
            selectedLabel = rootVisualElement.Q<Label>("SelectedLabel");
            PositionLabel = rootVisualElement.Q<Label>("PositionLabel");

            Notifier = rootVisualElement.Q<NotifierViewer>("NotifierViewer");

            inspectorPanelContainer = rootVisualElement.Q<VisualElement>("Inspector");
            inspectorManager = rootVisualElement.Q<LBSInspectorPanel>("InspectorPanel");
            sideBarPanel = rootVisualElement.Q<LBSSideBarPanel>("LBSSideBarPanel");

            subPanelScrollView = rootVisualElement.Q<ScrollView>("SubPanelScrollView");
            clippy = rootVisualElement.Q<Lbesin>("Lbesin");

            extraPanel = rootVisualElement.Q<VisualElement>("ExtraPanel");
            bottomPanel = rootVisualElement.Q<VisualElement>("BottomPanel");
            taskOverlay = rootVisualElement.Q<LBSWaitTaskOverlay>("WaitOverlay");
        }

        private void OnDisable()
        {
            //Debug.Log($"[LBSMainWindow] - OnDisable - {RandomId}");
            if (Instance == this)
            {
                SingletonHelper.SetInstanceNull();
                
                //desuscribirse de eventos 
                onLayerChange -= OnBlueprintCaptureEnable; 
                levelData!.OnReload -= OnLayerResetSelection;

                if (layerPanel != null)
                    layerPanel.OnSelectLayer -= OnSelectedLayerChange;

                if (levelData != null)
                {
                    levelData.OnChanged -= OnLevelDataChange;
                    levelData!.OnReload -= () => layerPanel.ResetSelection(); // Refactoriza la lambda
                }

                // Cerrar ventanas colgantes
                foreach (var w in hangingWindows)
                {
                    if (w == null) continue;
                    EditorApplication.delayCall += () => { w.Close(); };
                }
            }
        }


        private void OnDestroy()
        {
            if(this != null)
            {
                Close();
            }
        }

        #region METHODS
        protected override void CreateGUI()
        {
            LoadUITree();
            Init();
            rootVisualElement.focusable = true;
            rootVisualElement.Focus();
        }

        private void OnInspectorUpdate()
        {
            onWindowRepaint?.Invoke();
        }

        /// <summary>
        /// Initialize the window.
        /// </summary>
        private void Init()
        {
            #region LOAD & BACKUP LEVEL DATA
            if (LBS.loadedLevel == null)
            {
                if (levelData == null)
                {
                    LBS.loadedLevel = LBSController.CreateNewLevel();
                }
                else
                {
                    backUpData = levelData;
                    LBS.loadedLevel = LBSController.CreateNewLevel();
                    levelData = backUpData;
                }
            }
            levelData!.OnReload += () => layerPanel.ResetSelection();
            #endregion

            #region LOAD SCRIPTABLES TEMPLATE
            LayerTemplates = DirectoryTools.GetScriptablesByType<LayerTemplate>();
            LayerTemplates.Sort((a, b) => a.Order.CompareTo(b.Order));
            #endregion

            #region MAIN VIEW

            mainView.RegisterCallback<MouseMoveEvent>(HandleInfiniteScrolling, TrickleDown.TrickleDown);

            mainView.OnClearSelection += OnClearSelectionSub;

            #endregion

            #region HELP OVERLAY

            DisplayHelp();

            #endregion

            #region NOTIFIER TOOLBAR

            infoToolBar.Bind(this);

            #endregion

            #region TOOLBARS
            topToolBar.Bind(this);
            
            topToolBar.OnLoadLevel += data =>
            {
                LBS.loadedLevel = data;
                RebuildWindow();
                //drawManager.RedrawLevel(levelData);
            };
            
            topToolBar.OnThemeChanged += data => ChangeTheme(data);
            onLayerChange += topToolBar.LevelChange;

            //S = SAVE = Save level
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.S)
                {
                    topToolBar.SaveLevel();
                    evt.StopPropagation();
                }    
            }, TrickleDown.TrickleDown);
            //O = OPEN = Load level
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.O)
                {
                    topToolBar.LoadLevel();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            //N = NEW = New level
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.N)
                {
                    topToolBar.NewLevel();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
            #endregion

            #region PANELS - INSPECTOR, EXTRA, LAYERS, GENERATOR

            // THE ORDER IN WHICH THIS PANELS ARE ADDED DECIDES THEIR VERTICAL ORDER

            inspectorManager.InitTabs(ref LayerTemplates);
            
            subPanelScrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container").pickingMode = PickingMode.Ignore;
            subPanelScrollView.Q<VisualElement>("unity-content-viewport").pickingMode = PickingMode.Ignore;
            subPanelScrollView.Q<VisualElement>("unity-content-container").pickingMode = PickingMode.Ignore;
            
            layerPanel = new LayersPanel(levelData, ref LayerTemplates);
            extraPanel.Add(layerPanel);
            layerPanel.style.display = DisplayStyle.Flex;

            layerPanel.OnLayerVisibilityChange += _ => DrawManager.Instance.RedrawLevel(levelData);
            layerPanel.OnLayerOrderChange += _ => DrawManager.Instance.RedrawLevel(levelData, true);
            layerPanel.OnSelectLayer += OnSelectedLayerChange;
            layerPanel.OnAddLayer += layer => DrawManager.Instance.AddContainer(layer);
            layerPanel.OnRemoveLayer += l =>
            {
                //      drawManager.RemoveContainer(l);
                if (levelData.LayerCount != 0) return;

                //toolkit.Clear(); It already happens in OnSelectedLayerChange
                OnSelectedLayerChange(null);
            };

            quickAssistantPanel = new QuickAssistantPanel(LayerTemplates);
            extraPanel.Add(quickAssistantPanel);
            quickAssistantPanel.style.display = DisplayStyle.None;
            
            gen3DPanel ??= new Generator3DPanel();
            extraPanel.Add(gen3DPanel);
            gen3DPanel.style.display = DisplayStyle.None;

            blueprintPanel ??=  new BlueprintPanel();
            bottomPanel.Add(blueprintPanel);
            blueprintPanel.style.display = DisplayStyle.None;
            onLayerChange += OnBlueprintCaptureEnable;

            #endregion

            #region SIDE TOOLBAR TOGGLES
            sideBarPanel?.Bind(this);
            #endregion

            #region INSPECTOR TOGGLE BUTTON


            var buttonHideInspector = rootVisualElement.Q<Button>("ButtonDisplayInspector");
            buttonHideInspector.RegisterCallback<ClickEvent>(_ =>
            {
                if (inspectorPanelContainer.ClassListContains("lbs_inspectorhide"))
                {
                    inspectorPanelContainer.RemoveFromClassList("lbs_inspectorhide");
                    splitView.fixedPaneInitialDimension = 400f;
                }
                else
                {
                    inspectorPanelContainer.AddToClassList("lbs_inspectorhide");
                    splitView.fixedPaneInitialDimension = 80f;
                }
                splitView.MarkDirtyRepaint();
            });

            #endregion

            #region TOOLKIT

            toolkit = rootVisualElement.Q<ToolKit>("Toolkit");

            #endregion

            #region MAIN INIT & EVENTS

            LBSController.OnLoadLevel += _ => _selectedLayer = null;
            OnLevelDataChange(levelData);
            levelData.OnChanged += OnLevelDataChange;

            drawManager = new DrawManager();
            inspectorManager.CreateContainers(levelData, mainView);

            if (levelData != null && levelData.Layers != null)
            {
                foreach (var layer in levelData.Layers)
                {
                    DrawManager.Instance.AddContainer(layer);
                }
            }

            drawManager.RedrawLevel(levelData);

            #endregion

            #region THEME SET
            ChangeTheme(LBSSettings.Instance.view.LBSTheme);
            #endregion

            //clippy.InitModes();
        }




        /// <summary>
        /// Repaint the window.
        /// </summary>
        public new void Repaint()
        {
            base.Repaint();
            drawManager.RedrawLevel(levelData);
        }

        /// <summary>
        /// Refresh the window.
        /// </summary>
        public void RebuildWindow()
        {
            mainView.Clear();
            this.rootVisualElement.Clear();

            //Repaint();
            OnDisable();
            OnEnable();
            CreateGUI();
        }

        /// <summary>
        /// Called when the level data is changed.
        /// </summary>
        /// <param name="levelData"></param>
        private void OnLevelDataChange(LBSLevelData levelData)
        {
            var layersIsEmpty = levelData.Layers.Count <= 0;
            var questIsEmpty = levelData.Quests.Count <= 0;

            noLayerSign.style.display = (layersIsEmpty && questIsEmpty) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Called when the selected layer is changed. The only way to assign a selected layer is here.
        /// </summary>
        /// <param name="layer"></param>
        private void OnSelectedLayerChange(LBSLayer layer)
        {
            LBSLayer previousSelected = _selectedLayer;
            _selectedLayer = layer;

            if (previousSelected is not null)
            {
                // we update the layer so that the selected visibility function on the drawer gets called
                DrawManager.Instance.UpdateLayer(previousSelected);
                previousSelected.OnChange -= NotifyChange;
            }
            if (_selectedLayer is not null)
            {
                _selectedLayer.OnChange += NotifyChange;
                DrawManager.Instance.UpdateLayer(SelectedLayer);
            }

            string layerName = layer is not null ? layer.Name : "-";
            selectedLabel.text = "Selected: " + layerName;

        }

        public static void WarningManipulator(string description = null)
        {
            if (Instance.WarningLabel == null) return;
            Instance.WarningLabel.text = description;
            Instance.WarningNotification.visible = description != null && description != string.Empty;
        }

        private void NotifyChange()
        {
            onLayerChange?.Invoke();
        }

        public List<LBSLayer> GetLayers()
        {
            List<LBSLayer> layers = new List<LBSLayer>();
            if (layerPanel == null || layerPanel.Data == null) return layers;
            return layerPanel.Data.Layers;
        }

        private void OnFocus()
        {
            Undo.undoRedoPerformed += UNDO;
        }

        private void OnLostFocus()
        {
            Undo.undoRedoPerformed -= UNDO;
        }

        private void UNDO()
        {
            //So for some reason, THIS executes about 3-4 times every time it's executed. I have NO idea why this is and at this point I'm too scared to ask. -Alice
            foreach (var layer in GetLayers())
            {
                if (layer == _selectedLayer) layer.OnChangeUpdate();
                DrawManager.Instance.UpdateLayer(layer);
            }

            //if the undo added/eliminated a layer
            //{
                //layerPanel.ResetSelection();
                //layerPanel.RefreshUI();
                // The recovered layer must regain its non sereialized references (events, some stuff from its behaviours/assistants/... and its parent
            //}

            //if (_selectedLayer is not null)
            //{
            //    _selectedLayer.OnChangeUpdate();
            //    DrawManager.Instance.UpdateLayer(_selectedLayer);
            //}
            //else DrawManager.ReDraw();

            LBSInspectorPanel.ReDraw();
        }

        public static void MessageNotify(LBSLog lbsMessage)
        {
            Instance.Notifier?.SendNotification(
                lbsMessage.message, 
                lbsMessage.type, 
                lbsMessage.duration);
        }

        public void MessageManipulator(string description) => infoToolBar?.SetToolText(description);

        public static void SetGridPosition(Vector2 pos)
        {
            Instance.GridPosition = pos.ToInt();
            if (Instance.PositionLabel == null) return;
            string text = "Grid Position: " + pos.ToInt();
            Instance.PositionLabel.text = text;
        }

        public void DisplayHelp()
        {
            helpOverlay = new HintsController();
            helpOverlay.style.position = Position.Absolute;
            helpOverlayAnchor.Add(helpOverlay);
            if (helpOverlay == null) return;
            helpOverlay.style.display = helpOverlay.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void HandleInfiniteScrolling(MouseMoveEvent evt)
        {
            // Check if middle mouse button is being held (Button 2 / Bitmask 4)
            bool isMiddlePressed = (evt.pressedButtons & 4) != 0;
            if (!isMiddlePressed) return;

            var targetElement = evt.currentTarget as VisualElement;
            if (targetElement == null) return;

            POINT currentScreenPos;
            int screenLeftLimit = 0;
            int screenRightLimit = 0;

#if UNITY_EDITOR_WIN
            GetCursorPos(out currentScreenPos);
            IntPtr hMonitor = MonitorFromPoint(currentScreenPos, MONITOR_DEFAULTTONEAREST);
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf(mi);

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                screenLeftLimit = mi.rcMonitor.Left;
                screenRightLimit = mi.rcMonitor.Right;
            }
            else
            {
                screenLeftLimit = 0;
                screenRightLimit = Screen.currentResolution.width;
            }
#else
    currentScreenPos = new POINT { X = (int)evt.mousePosition.x, Y = (int)evt.mousePosition.y }; 
    screenLeftLimit = 0;
    screenRightLimit = Screen.currentResolution.width;
#endif

            int margin = 5;
            bool needWarp = false;
            int newScreenX = currentScreenPos.X;

            if (currentScreenPos.X <= screenLeftLimit + margin)
            {
                newScreenX = screenRightLimit - margin - 10;
                needWarp = true;
            }
            else if (currentScreenPos.X >= screenRightLimit - margin)
            {
                newScreenX = screenLeftLimit + margin + 10;
                needWarp = true;
            }

            if (!needWarp) return;

            // 1. Mark warping flag active
            //isWarpingCursor = true;

#if UNITY_EDITOR_WIN
            // 2. Warp OS cursor position directly
            SetCursorPos(newScreenX, currentScreenPos.Y);
#endif

            // 3. Defer flag reset to the end of the frame outside ProcessEvent stack
            EditorApplication.delayCall += FalseWarpingCursor;

            // 4. Stop propagation to prevent sudden large delta updates on target views
            evt.StopImmediatePropagation();
        }

        public void ToggleClippy(bool value)
        {
            clippy.SetDisplay(value ? DisplayStyle.Flex : DisplayStyle.None);
        }

        public static void BindHangingWindow(EditorWindow window)
        {
            if (Instance == null) return;
            if (!Instance.hangingWindows.Contains(window))
            {
                Instance.hangingWindows.Add(window);
            }
        }
        #endregion

        #region SUBSCRIBABLE METHODS
        private void OnBlueprintCaptureEnable()
        {
            if (blueprintPanel != null)
            {
                blueprintPanel.UpdateCaptureEnable();
            }
        }

        private void OnLayerResetSelection()
        {
            if (layerPanel != null)
            {
                layerPanel.ResetSelection();
            }
        }

        private void OnClearSelectionSub()
        {
            if (_selectedLayer != null)
            {
                var il = Reflection.MakeGenericScriptable(_selectedLayer);
                Selection.SetActiveObjectWithContext(il, il);
                il.hideFlags = HideFlags.DontSave; // or HideFlags.HideAndDontSave
            }
        }        

        private void FalseWarpingCursor()
        {
            //isWarpingCursor = false;
        }
        #endregion

        private sealed class SingletonHelper
        {

            [MenuItem("Window/ISILab/Level Building Sidekick", priority = 0)]
            private static void ShowWindow()
            {
                LBSMainWindow window = LBSMainWindow.OpenWindow;
                Texture icon = AssetMacro.LoadAssetByGuid<Texture>("e3db8d94c144db946ac8dd18f0bb7a9b");
                window.titleContent = new GUIContent("Level Builder", icon);
                window.minSize = new Vector2(800, 400);
            }

            // MainWindow instance
            private static LBSMainWindow _instance = null;
            public static LBSMainWindow Instance
            {
                get
                {
                    return _instance;
                }
                set
                {
                    if (_instance != null)
                    {
                        Debug.LogError("[LBSEditorWindow] - Instance is already set.");
                        return;
                    }
                    _instance = value;
                }
            }
            public static void SetInstanceNull()
            {
                _instance = null;
            }
        }
    }

}
