using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.Commons.VisualElements.Editor;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
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
using Debug = UnityEngine.Debug;
using InfoToolbar = ISILab.LBS.Plugin.UI.Editor.Panel.InfoToolbar;
using LBSSideBarPanel = ISILab.LBS.Plugin.UI.Editor.Panel.LBSSideBarPanel;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;
using ToolBarMain = ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar.ToolBarMain;


namespace ISILab.LBS.Editor.Windows
{

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

        private LBSLevelData backUpData;

        #endregion

        #region DATA & STATE

        // Selected
        public LBSLayer _selectedLayer;
        // Templates
        public List<LayerTemplate> layerTemplates;

        #endregion

        #region MANAGERS

        private ToolKit toolkit;
        private DrawManager drawManager;
        private LBSInspectorPanel inspectorManager;

        #endregion

        #region NOTIFICATIONS

        // Tool notification
        private static Label toolLabel;

        // Warning notification
        private static VisualElement warningNotification;
        private static Label warningLabel;
        public static NotifierViewer notifier;

        #endregion

        #region MAIN VIEW

        // Work canvas
        private MainView mainView;

        // Help overlays
        private static VisualElement helpOverlay;
        private VisualElement noLayerSign;
        private LBSSideBarPanel sideBarPanel;

        // Grid position
        public static Vector2Int _gridPosition;

        #endregion

        #region UI LABELS

        private Label selectedLabel;
        private static Label positionLabel;

        #endregion

        #region PANELS & UI SECTIONS VISUALELEMENTS

        public LayersPanel layerPanel;
        public Generator3DPanel gen3DPanel;
        public QuickAssistantPanel quickAssistantPanel;
        public BlueprintPanel blueprintPanel;
        public VisualElement extraPanel;
        public VisualElement bottomPanel;
        public VisualElement inspectorPanelContainer;

        private VisualElement helpOverlayAnchor;
        private ToolBarMain topToolBar;
        private InfoToolbar infoToolBar;
        private LBSWaitTaskOverlay taskOverlay;

        private ScrollView subPanelScrollView;

        [UxmlAttribute]
        private SplitView splitView;
        //[UxmlAttribute]
        //private LayerInspector layerInspector;


        #endregion

        private bool packageInitialized = false;
        public static bool IsWarpingCursor { get; set; }

        #region EVENTS
        public static Action OnWindowRepaint;
        public static Action OnLayerChange;
        #endregion

        #region STATIC METHODS

        private static LBSMainWindow _instance;
        public static LBSMainWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetWindow<LBSMainWindow>();
                }
                return _instance;
            }
        }
        #endregion

        public LBSMainWindow() : base()
        {
            // UI can't be referenced here because inherit from a scriptable object!
            //Debug.Log("[Main Window] - Constructor");


        }

        private void OnEnable()
        {
            Debug.Log("[Main Window] - OnEnable");
            _instance = this;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            if (!packageInitialized && packageInfo is not null && packageInfo.name.Equals("com.isilab.lbs"))
            {
                LBS_AssetsPostProcessor.InitializeLBSPackage();
                packageInitialized = true;
            }

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
            positionLabel = rootVisualElement.Q<Label>("PositionLabel");

            notifier = rootVisualElement.Q<NotifierViewer>("NotifierViewer");

            inspectorPanelContainer = rootVisualElement.Q<VisualElement>("Inspector");
            inspectorManager = rootVisualElement.Q<LBSInspectorPanel>("InspectorPanel");
            sideBarPanel = rootVisualElement.Q<LBSSideBarPanel>("LBSSideBarPanel");

            subPanelScrollView = rootVisualElement.Q<ScrollView>("SubPanelScrollView");

            extraPanel = rootVisualElement.Q<VisualElement>("ExtraPanel");
            bottomPanel = rootVisualElement.Q<VisualElement>("BottomPanel");
            taskOverlay = rootVisualElement.Q<LBSWaitTaskOverlay>("TaskOverlay");
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        [MenuItem("Window/ISILab/Level Building Sidekick", priority = 0)]
        private static void ShowWindow()
        {
            LBSMainWindow window = GetWindow<LBSMainWindow>();
            Texture icon = AssetMacro.LoadAssetByGuid<Texture>("e3db8d94c144db946ac8dd18f0bb7a9b");
            window.titleContent = new GUIContent("Level Builder", icon);
            window.minSize = new Vector2(800, 400);
        }


        #region METHODS
        public void CreateGUI()
        {
            Debug.Log("[Main Window] - CreateGUI");
            Init();
            rootVisualElement.focusable = true;
            rootVisualElement.Focus();
        }

        private void OnInspectorUpdate()
        {
            OnWindowRepaint?.Invoke();
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
            layerTemplates = DirectoryTools.GetScriptablesByType<LayerTemplate>();
            layerTemplates.Sort((a, b) => a.order.CompareTo(b.order));
            #endregion

            #region MAIN VIEW

            mainView.RegisterCallback<MouseMoveEvent>(HandleInfiniteScrolling, TrickleDown.TrickleDown);

            mainView.OnClearSelection += () =>
            {
                if (_selectedLayer != null)
                {
                    var il = Reflection.MakeGenericScriptable(_selectedLayer);
                    Selection.SetActiveObjectWithContext(il, il);
                }
            };

            #endregion

            #region HELP OVERLAY

            DisplayHelp();

            #endregion

            #region NOTIFIER TOOLBAR

            infoToolBar.Bind(this);

            #endregion
            

            #region MAIN VIEW
            
            mainView.OnClearSelection += () =>
            {
                if (_selectedLayer != null)
                {
                    var il = Reflection.MakeGenericScriptable(_selectedLayer);
                    Selection.SetActiveObjectWithContext(il, il);
                }
            };

            #endregion

            #region TOOLBARS
            topToolBar.Bind(this);
            
            topToolBar.OnLoadLevel += data =>
            {
                LBS.loadedLevel = data;
                RefreshWindow();
                //drawManager.RedrawLevel(levelData);
            };
            
            topToolBar.OnThemeChanged += data => ChangeTheme(data);
            OnLayerChange += topToolBar.LevelChange;

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


            inspectorManager.InitTabs(ref layerTemplates);
            
            subPanelScrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container").pickingMode = PickingMode.Ignore;
            subPanelScrollView.Q<VisualElement>("unity-content-viewport").pickingMode = PickingMode.Ignore;
            subPanelScrollView.Q<VisualElement>("unity-content-container").pickingMode = PickingMode.Ignore;
            
            layerPanel = new LayersPanel(levelData, ref layerTemplates);
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

            gen3DPanel ??= new Generator3DPanel();
            extraPanel.Add(gen3DPanel);
            gen3DPanel.style.display = DisplayStyle.None;

            quickAssistantPanel = new QuickAssistantPanel(layerTemplates);
            extraPanel.Add(quickAssistantPanel);
            quickAssistantPanel.style.display = DisplayStyle.None;

            blueprintPanel ??=  new BlueprintPanel();
            bottomPanel.Add(blueprintPanel);
            blueprintPanel.style.display = DisplayStyle.None;


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
        }


        /// <summary>
        /// Called when changing tabs from the toggle buttons in this class
        /// </summary>
        /// <param name="toggleVe"></param>
        public void ChangeInspectorPanelTab(Toggle toggleVe)
        {
            sideBarPanel.OnToggleButtonClick();
            toggleVe.SetValueWithoutNotify(true);
            if (toggleVe == sideBarPanel.layerDataTab) LBSInspectorPanel.ActivateDataTab();
            if (toggleVe == sideBarPanel.behaviorTab) LBSInspectorPanel.ActivateBehaviourTab();
            if (toggleVe == sideBarPanel.assistantTab) LBSInspectorPanel.ActivateAssistantTab();
        }

        /// <summary>
        /// Activates visually the corresponding toggle button, only call this from inspector panel
        /// </summary>
        /// <param name="panel"></param>
        public void InspectorToggleButtonChange(string panel)
        {
            if (sideBarPanel == null)
            {
                sideBarPanel = rootVisualElement.Q<LBSSideBarPanel>("SideBarPanel");
            }
            Toggle toggleVe = null;
            if (panel == LBSInspectorPanel.DataTab) toggleVe = sideBarPanel.layerDataTab;
            if (panel == LBSInspectorPanel.BehavioursTab) toggleVe = sideBarPanel.behaviorTab;
            if (panel == LBSInspectorPanel.AssistantsTab) toggleVe = sideBarPanel.assistantTab;
            if (toggleVe is null) return;

            sideBarPanel.OnToggleButtonClick();
            toggleVe.SetValueWithoutNotify(true);
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
        public void RefreshWindow()
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
        /// Called when the selected layer is changed.
        /// </summary>
        /// <param name="layer"></param>
        private void OnSelectedLayerChange(LBSLayer layer)
        {
            LBSLayer previousSelected = _selectedLayer;
            _selectedLayer = layer;

            if (previousSelected is not null)
            {
                previousSelected.OnChangeUpdate();
                previousSelected.OnChange -= NotifyChange;
            }
            if (_selectedLayer is not null)
            {
                _selectedLayer.OnChangeUpdate();
                _selectedLayer.OnChange += NotifyChange;
            }

            if (toolkit != null)
            {
                toolkit.Clear();
                inspectorManager.SetTarget(layer);
                toolkit.SetActive(typeof(SelectManipulator));
                toolkit.SetSeparators();
            }

           // gen3DPanel.Init(layer);

            string layerName = layer is not null ? layer.Name : "-";
            selectedLabel.text = "Selected: " + layerName;

        }

        public static void WarningManipulator(string description = null)
        {
            if (warningLabel == null) return;
            warningLabel.text = description;
            warningNotification.visible = description != null;
        }

        private static void NotifyChange()
        {
            OnLayerChange?.Invoke();
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
            {
                //layerPanel.ResetSelection();
                //layerPanel.RefreshUI();
                // The recovered layer must regain its non sereialized references (events, some stuff from its behaviours/assistants/... and its parent
            }

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
            notifier?.SendNotification(
                lbsMessage.message, 
                lbsMessage.type, 
                lbsMessage.duration);
        }

        public void MessageManipulator(string description)
        {
            infoToolBar?.SmallMessage(description);
        }

        public static void GridPosition(Vector2 pos)
        {
            _gridPosition = pos.ToInt();
            if (positionLabel == null) return;
            string text = "Grid Position: " + pos.ToInt();
            positionLabel.text = text;
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
                newScreenX = screenRightLimit - margin - 5;
                needWarp = true;
            }
            else if (currentScreenPos.X >= screenRightLimit - margin)
            {
                newScreenX = screenLeftLimit + margin + 5;
                needWarp = true;
            }

            if (!needWarp) return;

            IsWarpingCursor = true;
            Event fakeUp = new Event();
            fakeUp.type = EventType.MouseUp;
            fakeUp.button = 2;
            fakeUp.mousePosition = evt.mousePosition;
            fakeUp.modifiers = evt.modifiers;

            using (var upEvt = MouseUpEvent.GetPooled(fakeUp))
            {
                upEvt.target = targetElement;
                targetElement.SendEvent(upEvt);
            }
            targetElement.ReleaseMouse();

#if UNITY_EDITOR_WIN
            SetCursorPos(newScreenX, currentScreenPos.Y);
#endif

            targetElement.CaptureMouse();
            float deltaJump = (newScreenX - currentScreenPos.X);
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            float uiDelta = deltaJump / pixelsPerPoint;

            Vector2 newWindowPos = evt.mousePosition + new Vector2(uiDelta, 0);

            Event fakeDown = new Event();
            fakeDown.type = EventType.MouseDown;
            fakeDown.button = 2;
            fakeDown.mousePosition = newWindowPos;
            fakeDown.modifiers = evt.modifiers;

            using (var downEvt = MouseDownEvent.GetPooled(fakeDown))
            {
                downEvt.target = targetElement;
                targetElement.SendEvent(downEvt);
            }

            IsWarpingCursor = false;
            evt.StopImmediatePropagation();
        }

        #endregion
    }

}
