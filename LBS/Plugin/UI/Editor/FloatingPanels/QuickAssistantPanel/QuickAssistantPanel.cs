using ISILab.AI.Categorization;
using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.Internal.Editor;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using ToolBarMain = ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar.ToolBarMain;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class QuickAssistantPanel : VisualElement, IBundleFilter
    {
        public enum InteriorGenerationMode
        {
            GridWalker,
            Spiral
        }

        #region VIEW ELEMENTS
        private LBSCustomDropdown _layerTypeSelector;
        private LBSCustomButton _runButton;
        private WarningPanel _exteriorWarning;
        private LBSCustomFoldout _foldoutSettings;
        private List<QuickAssistantContainer> _containerList;
        #endregion

        #region PROPERTIES
        private static VisualTreeAsset visualTree;
        private const string UXML_NAME = "QuickAssistantPanel";
        public LBSButtonListFilter BundlePickerWindow { get; set; }
        private List<LayerTemplate> _templates;
        #endregion

        #region CONSTRUCTORS
        public QuickAssistantPanel()
        {
            Builder(null);
        }
        public QuickAssistantPanel(List<LayerTemplate> templates)
        {
            Builder(templates);
        }
        private void Builder(List<LayerTemplate> templates)
        {

            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QuickAssistantPanel] No se encontr� el UXML: {UXML_NAME}");

            if (templates is null) _templates = new List<LayerTemplate>();
            else _templates = templates;
            LoadVisualElements();
            InitDefaultState();
        }
        #endregion

        #region INITIALIZATION

        private void LoadVisualElements()
        {
            _layerTypeSelector = this.Q<LBSCustomDropdown>("LayerType");
            _runButton = this.Q<LBSCustomButton>("RunButton");
            _exteriorWarning = this.Q<WarningPanel>("ExteriorWarning");
            _foldoutSettings = this.Q<LBSCustomFoldout>("FoldoutSettings");

            if(_foldoutSettings is not null)
            {
                _containerList = new();

                // Exterior container
                var extContainer = new ExteriorContainer(_templates.FindAll(lt => lt.templateName.Contains("Exterior")));
                _containerList.Add(extContainer);
                _foldoutSettings.AddContent(extContainer);

                // Interior container
                var intContainer = new InteriorContainer(_templates.FindAll(lt => lt.templateName.Contains("Interior")));
                _containerList.Add(intContainer);
                _foldoutSettings.AddContent(intContainer);
                
                // Exterior container
                var popContainer = new PopulationContainer(_templates.FindAll(lt => lt.templateName.Contains("Population")));
                _containerList.Add(popContainer);
                _foldoutSettings.AddContent(popContainer);
            }

            _layerTypeSelector?.RegisterValueChangedCallback(evt => UpdateVisibility(evt.newValue?.ToString()));
            if (_runButton != null) _runButton.clicked += GenerateLayer;
        }

        private void InitDefaultState()
        {
            foreach(var container in _containerList)
            {
                container.InitialSetup();
            }
            if (_layerTypeSelector != null) _layerTypeSelector.index = -1;
            UpdateVisibility(null);
        }
        #endregion

        #region LOGIC METHODS
        private void UpdateVisibility(string mode)
        {
            bool showExterior = mode == "Exterior";
            bool showInterior = mode == "Interior";
            bool showPopulation = mode == "Population";

            foreach (var container in _containerList)
            {
                container.style.display = (container.PrimaryKeyword == mode) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_exteriorWarning != null) _exteriorWarning.style.display = DisplayStyle.None;
        }

        private async void GenerateLayer()
        {
            if (_layerTypeSelector == null || _layerTypeSelector.value == null) return;
            LBSLayer newLayer = null;

            string mode = _layerTypeSelector.value.ToString();
            var container = _containerList.FirstOrDefault( c => c.PrimaryKeyword == mode);
            if(container is not null)
            {
                newLayer = CreateBaseLayer(container.PrimaryKeyword, container.SecondaryKeyword);
                await container.GenerateLayerProcess(newLayer);
            }

            FinalizeLayer(newLayer);
        }

        private LBSLayer CreateBaseLayer(string primaryKeyword, string secondaryKeyword = null)
        {
            if (_templates == null || _templates.Count == 0)
            {
                Debug.LogWarning("[QuickAssistantPanle]: Template Layers weren't found in project or weren't assigned to this instance.");
                return null;
            }

            var candidates = _templates.Where(t => t.templateName.Contains(primaryKeyword)).ToList();
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[QuickAssistantPanle]: Couldn't find a template Layer with {primaryKeyword} in its name.");
                return null;
            }

            LayerTemplate targetTemplate = null;
            if (!string.IsNullOrEmpty(secondaryKeyword))
                targetTemplate = candidates.FirstOrDefault(t => t.templateName.Contains(secondaryKeyword));
            if (targetTemplate == null) targetTemplate = candidates[0];

            if (targetTemplate.layer.Clone() is LBSLayer newLayer)
            {
                string safeName = targetTemplate.templateName.Replace("/", " ").Replace("\\", " ");
                newLayer.Name = safeName;
                LBSMainWindow.Instance.layerPanel.AddLayer(newLayer);
                return newLayer;
            }
            Debug.LogWarning($"[QuickAssistantPanle]: Couldn't clone Layer to create a new one.");
            return null;
        }

        private void FinalizeLayer(LBSLayer layer)
        {
            if (layer is null) return;

            if (LBS.loadedLevel != null) EditorUtility.SetDirty(LBS.loadedLevel);
            layer.OnChangeUpdate();
            if (DrawManager.Instance != null) DrawManager.Instance.RedrawLayer(layer);

            LBSMainWindow.Instance._selectedLayer = layer;
            if (LBSInspectorPanel.Instance != null)
            {
                LBSInspectorPanel.Instance.SetTarget(layer);
                LBSInspectorPanel.ActivateBehaviourTab();
            }
        }

        #endregion
    }

}