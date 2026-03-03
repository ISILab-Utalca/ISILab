using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager;
using LBS.VisualElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Panel
{
    [UxmlElement]
    public partial class LBSSideBarPanel: VisualElement
    {
        private Toggle layerToggle;
        private Toggle gen3DToggle;
        private Toggle qAssisToggle;
        
        
        private static List<Toggle> inspectorToggleTabs = new();
        public Toggle layerDataTab;
        public Toggle assistantTab;
        public Toggle behaviorTab;
        
        private Toggle tagWindowButton;
        private Toggle blueprintWindowButton;
        private Toggle bundleWindowButton;

        private static VisualTreeAsset visualTreeAsset;


        #region EVENTS
        //public LBSBoolEvent toggleEvent; //Experimental!
        #endregion

        #region ACTION EVENTS

        //public event Action<ChangeEvent<bool>> OnTogglePressed;

        #endregion

        public LBSSideBarPanel(): base()
        {
            
            visualTreeAsset = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSSideBarPanel");
            visualTreeAsset.CloneTree(this);
            name = "LBSSideBarPanel";
            
            layerToggle = this.Q<Toggle>("LayerToggle");
            gen3DToggle = this.Q<Toggle>("Gen3DToggle");
            qAssisToggle = this.Q<Toggle>("QAssisToggle");
            
            layerDataTab = this.Q<Toggle>("LayerDataButton");
            assistantTab = this.Q<Toggle>("AssistantButton");
            behaviorTab = this.Q<Toggle>("BehaviourButton");
            inspectorToggleTabs.Clear();
            inspectorToggleTabs.Add(layerDataTab);
            inspectorToggleTabs.Add(assistantTab);
            inspectorToggleTabs.Add(behaviorTab);
            
            tagWindowButton = this.Q<Toggle>("TagButton");
            bundleWindowButton = this.Q<Toggle>("BundlesButton");
            blueprintWindowButton = this.Q<Toggle>("BlueprintButton");            
        }

        public void Bind(LBSMainWindow _mainWindow){
            if (_mainWindow != null)
            {
                layerToggle?.SetValueWithoutNotify(true);
                layerToggle?.RegisterCallback<ChangeEvent<bool>>(_evt =>
                {
                    _mainWindow.layerPanel.style.display = _evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });

                gen3DToggle?.SetValueWithoutNotify(false);
                gen3DToggle?.RegisterCallback<ChangeEvent<bool>>(_evt =>
                {
                    //_mainWindow.gen3DPanel.Init(_mainWindow._selectedLayer);
                    _mainWindow.gen3DPanel.style.display = _evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });

                qAssisToggle?.SetValueWithoutNotify(false);
                qAssisToggle?.RegisterCallback<ChangeEvent<bool>>(_evt =>
                {
                    _mainWindow.quickAssistantPanel.style.display = _evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });

                layerDataTab.RegisterCallback<ClickEvent>(_ => _mainWindow.ChangeInspectorPanelTab(layerDataTab));
                assistantTab.RegisterCallback<ClickEvent>(_ => _mainWindow.ChangeInspectorPanelTab(assistantTab));
                behaviorTab.RegisterCallback<ClickEvent>(_ => _mainWindow.ChangeInspectorPanelTab(behaviorTab));

                tagWindowButton?.RegisterCallback<ClickEvent>(_ =>
                {
                    OnToggleButtonClick();
                    tagWindowButton.SetValueWithoutNotify(true);
                });
                
                bundleWindowButton.RegisterCallback<ClickEvent>(_ =>
                {
                    OnToggleButtonClick();
                    BundleManagerWindow.ShowWindow();
                    bundleWindowButton.SetValueWithoutNotify(false);
                });

                blueprintWindowButton.RegisterCallback<ChangeEvent<bool>>(_evt =>
                {
                    var display = _evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                    _mainWindow.blueprintPanel.style.display = display;
                    var toolEntry = ToolKit.Instance.GetTool(typeof(CaptureInArea));
                    if (toolEntry.Key is null) 
                        return;
                    var captureTool = toolEntry.Value.Item1;
                    if (captureTool is null) 
                        return;
                    var captureMani = captureTool.Manipulator;
                    if (captureMani is null) 
                        return;
                    _mainWindow.blueprintPanel.CaptureManipulator = (CaptureInArea)captureMani;
                    // button change
                    toolEntry.Value.Item2.style.display = display;
                });
            }
        }
        
        
        //TODO: Change this behavior for tabs system.
        public void OnToggleButtonClick()
        {
            foreach (var toggleTab in inspectorToggleTabs)
            {
                if (toggleTab is Toggle toggle)
                {
                    toggle.SetValueWithoutNotify(false); // Deselect
                }
            }
        }
        
    }
}
