using System;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar
{
    [UxmlElement]
    public partial class ToolBarMain : VisualElement
    {
        public string defaultLabel = "Unsaved File *";
        
        public event Action<LoadedLevel> OnLoadLevel;
        public event Action<LoadedLevel> OnNewLevel;
        public event Action<LoadedLevel> OnSaveLevel;
        public event Action<LoadedLevel> OnLevelChange;
        public event Action<LBSSettings.Interface.InterfaceTheme> OnThemeChanged;
        
        //public event Action OnProgressCompleted;
        public event Action OnProgressCancelled;

        #region  Visual Elements
        private LBSCustomButton HelpToggle;
        private VisualElement taskInfo;
        private LBSCustomProgressBar taskProgressBar;
        private LBSCustomButton taskStopButton;
        private LBSCustomButton settingMenu;
        #endregion

        public ToolBarMain()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ToolBarMain");
            visualTree.CloneTree(this);

            // File menu option
            LBSToolbarMenu fileMenu = this.Q<LBSToolbarMenu>("ToolBarMenu");
            fileMenu.menu.AppendAction("New", NewLevel);
            fileMenu.menu.AppendAction("Load", LoadLevel);
            fileMenu.menu.AppendAction("Save", SaveLevel);
            fileMenu.menu.AppendAction("Save as", SaveAsLevel);

            //Button
            settingMenu = this.Q<LBSCustomButton>("OptionButton");
            HelpToggle = this.Q<LBSCustomButton>("HelpButton");

            LBSCustomButton bundManBtn = this.Q<LBSCustomButton>("BundleManagerButton");
            bundManBtn.clickable.clicked += BundleManagerWindow.ShowWindow;

            // file name label
            var label = this.Q<Label>("IsSavedLabel"); 
            if(LBS.loadedLevel?.FileInfo!=null) { label.text = LBS.loadedLevel.FileInfo.Name; }
            else { label.text = defaultLabel; }

            LBSCustomEnumField ThemeSelector = this.Q<LBSCustomEnumField>("ThemeSelector");
            ThemeSelector.RegisterValueChangedCallback(_evt =>
            {
                OnThemeChanged?.Invoke((LBSSettings.Interface.InterfaceTheme)_evt.newValue);
            });
            
            OnSaveLevel += (level) => { label.text = LBS.loadedLevel?.FileInfo?.Name; };
            OnLevelChange += (level) => { label.text = LBS.loadedLevel?.FileInfo != null ? LBS.loadedLevel.FileInfo.Name +" *" : defaultLabel; };
            
            
            taskInfo = this.Q<VisualElement>("TaskInfo");
            taskProgressBar = this.Q<LBSCustomProgressBar>("TaskProgressBar");
            taskStopButton = this.Q<LBSCustomButton>("TaskStop");
            
            taskStopButton.clicked += () =>
            {
                EnableProcess(false);
                OnProgressCancelled?.Invoke();
            };
            
            taskInfo.style.display = DisplayStyle.None;
            
        }

        public void EnableProcess(bool enable, string assistantName = "Assistant")
        {
            LBSMainWindow.Instance.WaitTaskOverlay.ShowRect = enable;
            
            taskProgressBar.ProgressTextLabel = assistantName;
            var percent = enable ? 0 : 1;
            SetProgressPercent(percent);
            taskInfo.style.display = enable ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public void SetProgressPercent(float percent)
        {
            taskProgressBar.title = $"{Mathf.RoundToInt(percent * 100)}%";
            taskProgressBar.value = percent;    
        }
        
        public void Bind(LBSMainWindow _mainWindow)
        {
            HelpToggle.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("[Display Help]: Redirecting to the web]");
                Application.OpenURL("https://isilab-utalca.github.io/isilab-website/documentation/tutorials/lbs/shortcuts/");
            });
            
            OnNewLevel += (_loadedLevel) =>
            {
                LBS.loadedLevel = _loadedLevel;
                _mainWindow.RebuildWindow();
            };
            
            settingMenu.RegisterCallback<ClickEvent>(OpenConfiguration);
        }
        
        
        public void UnBind(LBSMainWindow _mainWindow)
        {
            Debug.LogWarning("[ToolBarMain]: UnBind not implemented.");
        }

        public void NewLevel(DropdownMenuAction dma)
        {
            NewLevel();
        }
        public void NewLevel()
        {
            var answer = EditorUtility.DisplayDialog(
                   "Current File Not Saved",
                   "Progress in the current level will be lost. Proceed?",
                   "confirm",
                   "cancel");
            switch (answer)
            {
                case true:
                    var data = LBSController.CreateNewLevel("new file");
                    if (data != null)
                    {
                        OnNewLevel?.Invoke(data);
                     
                        LBSMainWindow.MessageNotify(new LBSLog("New level created."));
                    }
                    return;
                case false:
                    return;
            }

            
        }

        public void LoadLevel(DropdownMenuAction dma)
        {
            LoadLevel();
        }
        public void LoadLevel()
        {
            var data = LBSController.LoadFile();
            if (data != null)
            {
                OnLoadLevel?.Invoke(data);
                LBSMainWindow.MessageNotify(new LBSLog("The level has been loaded successfully."));
            }

        }

        public void LevelChange()
        {
            OnLevelChange?.Invoke(LBS.loadedLevel);
        }

        public void SaveLevel(DropdownMenuAction dma)
        {
            SaveLevel();
        }
        public void SaveLevel()
        {
            LBSController.SaveFile();
            OnSaveLevel?.Invoke(LBS.loadedLevel);
            AssetDatabase.Refresh();
        }

        public void SaveAsLevel(DropdownMenuAction dma)
        {
            if (LBSController.SaveFileAs()) { 
                OnSaveLevel?.Invoke(LBS.loadedLevel);
            }
            AssetDatabase.Refresh();
        }

        public static void OpenConfiguration(ClickEvent evt)
        {
            // Open the Project Settings window
            SettingsService.OpenProjectSettings("LBS");
        }
        
        public void CancelProgress()
        {
            OnProgressCancelled?.Invoke();
            OnProgressCancelled = null;
            EnableProcess(false);
        }
    }
}