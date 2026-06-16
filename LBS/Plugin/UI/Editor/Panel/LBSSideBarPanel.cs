using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager;
using ISILab.LBS.Plugin.UI.Editor.Windows.TagManager;
using ISILab.LBS.VisualElements;
using LBS.VisualElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

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


        #region STATIC METHODS

        private static LBSSideBarPanel instance;
        public static LBSSideBarPanel Instance
        {
            get => instance ?? (instance = new LBSSideBarPanel());
        }
        #endregion

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

            instance = this;

            name = "LBSSideBarPanel";
            
            layerToggle = this.Q<Toggle>("LayerToggle");
            gen3DToggle = this.Q<Toggle>("Gen3DToggle");
            qAssisToggle = this.Q<Toggle>("QAssisToggle");
            
            layerDataTab = this.Q<Toggle>("LayerDataButton");
            layerDataTab.RegisterCallback<ChangeEvent<bool>>(evt => 
            {
                if (evt.newValue == true)
                    LBSInspectorPanel.ActivateDataTab();
            });

            assistantTab = this.Q<Toggle>("AssistantButton");
            assistantTab.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (evt.newValue == true)
                    LBSInspectorPanel.ActivateAssistantTab();
            });

            behaviorTab = this.Q<Toggle>("BehaviourButton");
            behaviorTab.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if(evt.newValue == true)
                    LBSInspectorPanel.ActivateBehaviourTab();
            });
            
            inspectorToggleTabs.Clear();
            inspectorToggleTabs.Add(layerDataTab);
            inspectorToggleTabs.Add(assistantTab);
            inspectorToggleTabs.Add(behaviorTab);
            
            tagWindowButton = this.Q<Toggle>("TagsButton");
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

                layerDataTab.RegisterCallback<ClickEvent>(_ => ChangeInspectorPanelTab(layerDataTab));
                assistantTab.RegisterCallback<ClickEvent>(_ => ChangeInspectorPanelTab(assistantTab));
                behaviorTab.RegisterCallback<ClickEvent>(_ => ChangeInspectorPanelTab(behaviorTab));

                tagWindowButton?.RegisterCallback<ClickEvent>(_ =>
                {
                    DeactiveToggles();
                    switch(tagWindowButton.value)
                    {
                        case true:
                            TagManagerWindow.ShowWindow();
                            TagManagerWindow.OnClosed += () => { tagWindowButton.SetValueWithoutNotify(false); };
                            break;
                        case false:
                            TagManagerWindow.CloseWindow();
                            break;
                    }
                });
                
                bundleWindowButton?.RegisterCallback<ClickEvent>(_ =>
                {
                    DeactiveToggles();
                    switch (bundleWindowButton.value)
                    {
                        case true:
                            BundleManagerWindow.ShowWindow();
                            BundleManagerWindow.OnClosed += () => { bundleWindowButton.SetValueWithoutNotify(false); };
                            break;
                        case false:
                            BundleManagerWindow.CloseWindow();
                            break;
                    }
                });

                blueprintWindowButton.RegisterCallback<ChangeEvent<bool>>((evt) =>
                {
                    _mainWindow.blueprintPanel.OnActivate(evt);
                });
            }
        }
        
        

        /// <summary>
        /// Deactivates all the toggles buttons
        /// </summary>
        private void DeactiveToggles()
        {
            foreach (var toggleTab in inspectorToggleTabs)
            {
                if (toggleTab is Toggle toggle)
                {
                    toggle.value = (false); // Deselect
                }
            }
        }

        /// <summary>
        /// Called when changing tabs from the toggle buttons in this class
        /// </summary>
        /// <param name="toggleVe"></param>
        public void ChangeInspectorPanelTab(Toggle toggleVe)
        {
            DeactiveToggles();
            if (toggleVe is null) 
                return;
            
            toggleVe.value = (true);
        }

        /// <summary>
        /// Activates visually the corresponding toggle button, only call this from inspector panel
        /// </summary>
        /// <param name="panel"></param>
        public void InspectorToggleButtonChange(string panel)
        {
            Toggle toggleVe = null;
            if (panel == LBSInspectorPanel.DataTab) 
                toggleVe = layerDataTab;
            if (panel == LBSInspectorPanel.BehavioursTab) 
                toggleVe = behaviorTab;
            if (panel == LBSInspectorPanel.AssistantsTab) 
                toggleVe = assistantTab;
            ChangeInspectorPanelTab(toggleVe);
        }
    }
}
