using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
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
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Components.Behaviours;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("SimulationBehaviour", typeof(SimulationBehaviour))]
    public class SimulationBehaviourEditor : LBSCustomEditor, IToolProvider
    {
       #region FIELDS
        // Palletes
        private SimulationTagPallete bundlePallete;
        // PathOS Original Inspector
        private PathOSWindow pathOSOriginalWindow;
        // Target (PathOSBehaviour)
        private SimulationBehaviour behaviour;
        // Manipulators
        AddSimulationTile addPathOSTile;
        RemoveSimulationTile removePathOSTile;
        AddClosedObstacle addClosedObstacle;
        AddOpenedObstacle addOpenedObstacle;

        // Visual Element
        VisualElement warning;
        VisualElement mappingContent;
        LBSCustomObjectField upStairTagField;
        LBSCustomObjectField downStairTagField;

        Toggle autoMapToggle;

        List<LBSLayer> populationLayers = new List<LBSLayer>();
        List<TileBundleGroup> populationGroups = new List<TileBundleGroup>();

        List<LBSLayer> interiorLayers = new List<LBSLayer>();
        Dictionary<string, List<LBSTile>> interiorTiles = new Dictionary<string, List<LBSTile>>();
        List<LBSStair> interiorStairs = new List<LBSStair>();
        #endregion

        #region PROPERTIES
        public PathOSWindow PathOSOriginalWindow { get => pathOSOriginalWindow; set => pathOSOriginalWindow = value; }

        LBSLevelData Data { get => behaviour.OwnerLayer.Parent; }
        #endregion

        #region METHODS
        public SimulationBehaviourEditor(object target) : base(target)
        {
            behaviour = target as SimulationBehaviour;
            //Debug.Log("BEHAVIOUR CONSTRUCTED");
            behaviour.AutoMapCallback = () => { MapToCurrentPopulation(behaviour.OwnerLayer.ActiveFloor); };
            behaviour.RemoveAutoMapCallbacks = () =>
            {
                GetPopulationLayers();
                foreach(LBSLayer layer in populationLayers)
                {
                    layer.OnChange -= () => { MapToCurrentPopulation(behaviour.OwnerLayer.ActiveFloor); };// _target.AutoMapCallback;
                    //Debug.Log($"Removed Auto Map Callback from layer {layer.Name}");
                }
            };
            behaviour.LevelChangedCallback = (int newFloor) =>
            {
                MapToCurrentPopulation(newFloor);
            };

            interiorTiles.Add(SchemaBehaviour.Door, new List<LBSTile>());
            interiorTiles.Add(SchemaBehaviour.LockedDoor, new List<LBSTile>());
            interiorStairs = new List<LBSStair>();
            CreateVisualElement();
            MapToCurrentPopulation(behaviour.OwnerLayer.ActiveFloor);
        }

        public override void SetInfo(object target)
        {
            behaviour = target as SimulationBehaviour;
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
            mapPopulButton.clicked += () => MapToCurrentPopulation(behaviour.OwnerLayer.ActiveFloor);

            var clearButton = this.Q<Button>("Clear");
            clearButton.clicked += () => ClearMapping();



            // Low Stair Tag field
            upStairTagField = new LBSCustomObjectField
            {
                name = "UpStairTagField",
                label = "Up Stair Tag",
                objectType = typeof(LBSTag)
            };
            upStairTagField.style.marginTop = 4;
            // Initialize with current value if available
            if (behaviour != null)
                upStairTagField.SetValueWithoutNotify(behaviour.upStairTag);
            // Register callback for value changes
            upStairTagField.RegisterValueChangedCallback(evt =>
            {
                behaviour.upStairTag = evt.newValue as LBSTag;
                var level = LBSController.CurrentLevel;
                if (level != null)
                    EditorUtility.SetDirty(level);
            });
            // Add to mapping content container
            mappingContent.Add(upStairTagField);
            // -----------------------------------------------
            // High Stair Tag field
            downStairTagField = new LBSCustomObjectField
            {
                name = "DownStairTagField",
                label = "Down Stair Tag",
                objectType = typeof(LBSTag)
            };
            downStairTagField.style.marginTop = 4;
            // Initialize with current value if available
            if (behaviour != null)
                downStairTagField.SetValueWithoutNotify(behaviour.downStairTag);
            // Register callback for value changes
            downStairTagField.RegisterValueChangedCallback(evt =>
            {
                behaviour.downStairTag = evt.newValue as LBSTag;
                var level = LBSController.CurrentLevel;
                if (level != null)
                    EditorUtility.SetDirty(level);
            });
            // Add to mapping content container
            mappingContent.Add(downStairTagField);



            // Add and set Tag Pallete
            bundlePallete = new SimulationTagPallete();
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
            List<Bundle> tempOSBundles = allBundles.Where(
                b => b.GetCharacteristics<LBSSimulationTagsCharacteristic>().Count > 0).ToList();

            // Si no hay PathOS Bundles, abortar.
            if (tempOSBundles.Count == 0) { return; }

            List<Bundle> pathOSBundles = new List<Bundle>();

            foreach (Bundle bundle in tempOSBundles) 
            {
                if (bundle.LayerContentFlags.HasFlag(BundleFlags.Simulation)) pathOSBundles.Add(bundle);
            
            }

            // Generalizacion de Bundles a "object" (ej.: Para usar en el objeto pallete, y los option views)
            object[] options = new object[pathOSBundles.Count];
            for (int i = 0; i < pathOSBundles.Count; i++)
            {
                options[i] = pathOSBundles[i];
            }

            // No mostrar Dropdown por defecto
            bundlePallete.ShowGroups = false;

            // OnSelect event
            bundlePallete.OnSelectOption += (selected) =>
            {
                behaviour.selectedToSet = selected as Bundle;
                ToolKit.Instance.SetActive(typeof(AddSimulationTile));
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
                Bundle bundle = (Bundle)option;
                optionView.Label = "";//bundle.name;
                optionView.Color = bundle.Color;
                optionView.Icon = bundle.Icon;
                
            });

            bundlePallete.OnRepaint += () => { bundlePallete.Selected = behaviour.selectedToSet; };

            bundlePallete.Repaint();
        }

        public void SetTools(ToolKit toolkit)
        {
    
            addPathOSTile = new AddSimulationTile();
            var t1 = new LBSTool(addPathOSTile);
 

            removePathOSTile = new RemoveSimulationTile();
            var t2 = new LBSTool(removePathOSTile);    

            addPathOSTile.SetRemover(removePathOSTile);

            addClosedObstacle = new AddClosedObstacle();
            var t3 = new LBSTool(addClosedObstacle);


            addOpenedObstacle = new AddOpenedObstacle();
            var t4 = new LBSTool(addOpenedObstacle);

            foreach (LBSTool tool in new[] { t1, t2, t3, t4 })
            {
                tool.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
                toolkit.ActivateTool(tool, behaviour.OwnerLayer, behaviour);
            }
        }

        public override void OnFocus()
        {
            base.OnFocus();
            UpdatePopulationGroups(behaviour.OwnerLayer.ActiveFloor);
            UpdateInteriorTiles(behaviour.OwnerLayer.ActiveFloor);
        }

        private void UpdatePopulationGroups(int floor)
        {
            populationGroups.Clear();
            GetPopulationLayers();
            bool layersExist = populationLayers.Count > 0;
            ShowMappingContent(layersExist);
            if (layersExist)
                GetPopulationGroups(floor);
        }

        private void GetPopulationLayers()
        {
            populationLayers.Clear();
            if (Data.LayerCount <= 1)
                return;

            populationLayers = Data.Layers.FindAll(l => l.ID.Equals("Population"));
        }

        private void GetPopulationGroups(int floor)
        {
            var tileMaps = new List<BundleTileMap>();
            foreach (LBSLayer layer in populationLayers)
            {
                var tileMap = layer.GetModule<BundleTileMap>("", floor);
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
            UpdatePopulationGroups(behaviour.OwnerLayer.ActiveFloor);
            return populationGroups;
        }

        private void UpdateInteriorTiles(int floor)
        {
            interiorTiles[SchemaBehaviour.Door].Clear();
            interiorTiles[SchemaBehaviour.LockedDoor].Clear();
            interiorStairs.Clear();
            GetInteriorLayers();
            if (interiorLayers.Count > 0)
                GetInteriorTiles(floor);
        }

        private void GetInteriorLayers()
        {
            interiorLayers.Clear();
            if(Data.LayerCount <= 1)
                return;

            interiorLayers = Data.Layers.FindAll(l => l.ID.Equals("Interior"));
        }

        private void GetInteriorTiles(int floor)
        {
            foreach(LBSLayer interiorLayer in interiorLayers)
            {
                Dictionary<string, List<LBSTile>> lists = interiorLayer.GetModule<SectorizedTileMapModule>("", floor).GetTilesWithConnections(floor, SchemaBehaviour.Door, SchemaBehaviour.LockedDoor);
                interiorTiles[SchemaBehaviour.Door].AddRange(lists[SchemaBehaviour.Door]);
                interiorTiles[SchemaBehaviour.LockedDoor].AddRange(lists[SchemaBehaviour.LockedDoor]);

                foreach (LBSStair stair in interiorLayer.GetModule<StairsModule>("", floor).Stairs)
                {
                    if (stair.Direction < 0) continue;
                    interiorStairs.Add(stair);
                }
            }
        }

        private void MapToCurrentPopulation(int floor)
        {
            UpdatePopulationGroups(floor);
            UpdateInteriorTiles(floor);
            MapToPopulation();
        }

        private void MapToPopulation()
        {
            LoadedLevel level = LBSController.CurrentLevel;

            EditorGUI.BeginChangeCheck();

            behaviour.MapToPopulation(populationGroups, 
                interiorTiles[SchemaBehaviour.Door], interiorTiles[SchemaBehaviour.LockedDoor], 
                interiorStairs);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(level);

            DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
        }

        private void ClearMapping()
        {
            LoadedLevel level = LBSController.CurrentLevel;

            EditorGUI.BeginChangeCheck();

            behaviour.ClearMapping();
            autoMapToggle.value = false;


            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(level);

            DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
        }

        #endregion
    }
}
