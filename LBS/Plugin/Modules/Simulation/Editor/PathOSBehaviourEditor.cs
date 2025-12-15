using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.Modules.Simulation.Editor.Manipulators;
using PathOS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("PathOSBehaviour", typeof(PathOSBehaviour))]
    public class PathOSBehaviourEditor : LBSCustomEditor, IToolProvider
    {
       #region FIELDS
        // Palletes
        private PathOSTagPallete bundlePallete;
        // PathOS Original Inspector
        private PathOSWindow pathOSOriginalWindow;
        // Target (PathOSBehaviour)
        private PathOSBehaviour pathOS;
        // Manipulators
        AddPathOSTile addPathOSTile;
        RemovePathOSTile removePathOSTile;
        AddClosedObstacle addClosedObstacle;
        AddOpenedObstacle addOpenedObstacle;

        // Visual Element
        VisualElement warning;
        VisualElement mappingContent;

        Toggle autoMapToggle;

        List<LBSLayer> populationLayers = new List<LBSLayer>();
        List<TileBundleGroup> populationGroups = new List<TileBundleGroup>();
        #endregion

        #region PROPERTIES
        public PathOSWindow PathOSOriginalWindow { get => pathOSOriginalWindow; set => pathOSOriginalWindow = value; }

        LBSLevelData Data { get => pathOS.OwnerLayer.Parent; }
        #endregion

        #region METHODS
        public PathOSBehaviourEditor(object target) : base(target)
        {
            pathOS = target as PathOSBehaviour;
            Debug.Log("BEHAVIOUR CONSTRUCTED");
            pathOS.AutoMapCallback = MapToCurrentPopulation;
            pathOS.RemoveAutoMapCallbacks = () =>
            {
                GetPopulationLayers();
                foreach(LBSLayer layer in populationLayers)
                {
                    layer.OnChange -= MapToCurrentPopulation;// _target.AutoMapCallback;
                    Debug.Log($"Removed Auto Map Callback from layer {layer.Name}");
                }
            };


            CreateVisualElement();
            MapToCurrentPopulation();
        }

        public override void SetInfo(object target)
        {
            pathOS = target as PathOSBehaviour;
        }

        protected override VisualElement CreateVisualElement()
        {

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("SimulationBehaviourEditor");
            visualTree.CloneTree(this);
            
            warning = this.Q<VisualElement>("Warning");
            mappingContent = this.Q<VisualElement>("SimulationMappingContent");

            autoMapToggle = this.Q<Toggle>("AutoMap");
            /*
            autoMapToggle.RegisterValueChangedCallback(evt => 
            {
                bool value = evt.newValue;
                if (value)
                    MapToCurrentPopulation();
                else
                    GetPopulationLayers();
                pathOS.ToggleAutoMap(value, populationLayers);
            });
            autoMapToggle.SetValueWithoutNotify(pathOS.AutoMap);
            */
            var mapPopulButton = this.Q<Button>("MapPopulation");
            mapPopulButton.clicked += () => MapToCurrentPopulation();

            var clearButton = this.Q<Button>("Clear");
            clearButton.clicked += () => ClearMapping();

            // Add and set Tag Pallete

            bundlePallete = new PathOSTagPallete();
            Add(bundlePallete);
            bundlePallete.SetName("[LEGACY] PathOS+ Tags");
            SetBundlePallete();

            return this;
        }
        
        private void SetBundlePallete()
        {
            bundlePallete.name = "Bundles";
            Texture2D icon = Resources.Load<Texture2D>("Icons/TinyIconPathOSModule16x16");
            bundlePallete.SetIcon(icon, Color.white);

            // Obtener Bundles PathOS
            List<Bundle> allBundles = LBSAssetsStorage.Instance.Get<Bundle>();
            List<Bundle> pathOSBundles = allBundles.Where(
                b => b.GetCharacteristics<LBSPathOSTagsCharacteristic>().Count > 0).ToList();

            // Si no hay PathOS Bundles, abortar.
            if (pathOSBundles.Count == 0) { return; }

            // Generalizacion de Bundles a "object" (ej.: Para usar en el objeto pallete, y los option views)
            var options = new object[pathOSBundles.Count];
            for (int i = 0; i < pathOSBundles.Count; i++)
            {
                options[i] = pathOSBundles[i];
            }

            // No mostrar Dropdown por defecto
            bundlePallete.ShowGroups = false;

            // OnSelect event
            bundlePallete.OnSelectOption += (selected) =>
            {
                pathOS.selectedToSet = selected as Bundle;
                ToolKit.Instance.SetActive(typeof(AddPathOSTile));
            };

            // OnAdd option event
            bundlePallete.OnAddOption += () =>
            {
                Debug.LogWarning("Por ahora esta herramienta no permite agregar nuevos tipos de bundles");
            };

            // Tooltip event (texto al mantener mouse sobre opcion)
            bundlePallete.OnSetTooltip += (option) =>
            {
                var b = option as Bundle;

                var tooltip = "Tags:";
                if (b.Characteristics.Count > 0)
                {
                    b.Characteristics.ForEach(c => tooltip += "\n- " + c?.GetType().ToString());
                }
                else
                {
                    tooltip += "\n[None]";
                }
                return tooltip;
            };

            // Init options para el Pallete mismo
            bundlePallete.SetOptions(options, (optionView, option) =>
            {
                var bundle = (Bundle)option;
                optionView.Label = bundle.name;
                optionView.Color = bundle.GetCharacteristics<LBSPathOSTagsCharacteristic>()[0].Value.Color;
                optionView.Icon = bundle.Icon;
            });

            bundlePallete.OnRepaint += () => { bundlePallete.Selected = pathOS.selectedToSet; };

            bundlePallete.Repaint();
        }

        public void SetTools(ToolKit toolkit)
        {
    
            addPathOSTile = new AddPathOSTile();
            var t1 = new LBSTool(addPathOSTile);

            removePathOSTile = new RemovePathOSTile();
            var t2 = new LBSTool(removePathOSTile);         

      
            addClosedObstacle = new AddClosedObstacle();
            var t3 = new LBSTool(addClosedObstacle);


            addOpenedObstacle = new AddOpenedObstacle();
            var t4 = new LBSTool(addOpenedObstacle);

            foreach (LBSTool tool in new[] { t1, t2, t3, t4 })
            {
                tool.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
                toolkit.ActivateTool(tool, pathOS.OwnerLayer, pathOS);
            }
        }

        public override void OnFocus()
        {
            base.OnFocus();
            UpdatePopulationGroups();
        }

        private void UpdatePopulationGroups()
        {
            populationGroups.Clear();
            GetPopulationLayers();
            bool layersExist = populationLayers.Count > 0;
            ShowMappingContent(layersExist);
            if (layersExist)
                GetPopulationGroups();
        }

        private void GetPopulationLayers()
        {
            populationLayers.Clear();
            if (Data.LayerCount <= 1)
                return;

            populationLayers = Data.Layers.FindAll(l => l.ID.Equals("Population"));
        }

        private void GetPopulationGroups()
        {
            var tileMaps = new List<BundleTileMap>();
            foreach (LBSLayer layer in populationLayers)
            {
                var tileMap = layer.GetModule<BundleTileMap>();
                if (tileMap != null)
                    tileMaps.Add(tileMap);
            }

            foreach(BundleTileMap tileMap in tileMaps)
            {
                populationGroups.AddRange(tileMap.Groups);
            }
        }

        private void ShowMappingContent(bool show)
        {
            mappingContent  .style.display = (DisplayStyle)( show ? 0 : 1);
            warning         .style.display = (DisplayStyle)(!show ? 0 : 1);
        }

        public List<TileBundleGroup> GetCurrentPopulationGroups()
        {
            UpdatePopulationGroups();
            return populationGroups;
        }

        private void MapToCurrentPopulation()
        {
            UpdatePopulationGroups();
            MapToPopulation();
        }

        private void MapToPopulation()
        {
            LoadedLevel level = LBSController.CurrentLevel;

            EditorGUI.BeginChangeCheck();

            pathOS.MapToPopulation(populationGroups);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(level);

            DrawManager.Instance.RedrawLayer(pathOS.OwnerLayer);
        }

        private void ClearMapping()
        {
            LoadedLevel level = LBSController.CurrentLevel;

            EditorGUI.BeginChangeCheck();

            pathOS.ClearMapping();
            autoMapToggle.value = false;


            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(level);

            DrawManager.Instance.RedrawLayer(pathOS.OwnerLayer);
        }

        #endregion
    }
}
