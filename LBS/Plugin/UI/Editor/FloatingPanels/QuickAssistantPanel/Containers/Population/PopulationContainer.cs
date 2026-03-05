using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class PopulationContainer : QuickAssistantContainer
    {
        private LBSLevelData Data => LBS.loadedLevel.data;
        public override string PrimaryKeyword { get => _primaryKeyword; }
        private const string _primaryKeyword = "Population";
        public override string SecondaryKeyword { get => _secondaryKeyword; }
        private const string _secondaryKeyword = null;

        private static VisualTreeAsset visualTree;
        private const string UXML_NAME = "PopulationContainer";
        private List<LayerTemplate> _templates;

        private LBSCustomObjectField _popMainBundle;
        private LBSCustomTextField _popSeed;
        private List<KeyValuePair<LBSTag, BoolIntPair>> _popTagList;
        private ListView _popToggleView;
        private Button _popSelectContextButton;
        private ListView _popContextView;

        public PopulationContainer(List<LayerTemplate> relatedTemplates)
        {
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QuickAssistantPanel] No se encontr� el UXML: {UXML_NAME}");

            _templates = relatedTemplates;

            LoadVisualElements();
        }

        #region OVERRIDE METHODS
        public override void LoadVisualElements()
        {
            _popSeed = this.Q<LBSCustomTextField>("PopSeed");

            _popMainBundle = this.Q<LBSCustomObjectField>("PopMainBundle");
            if (_popMainBundle != null)
            {
                _popMainBundle.objectType = typeof(Bundle);
                _popMainBundle.UseCustomFilter = true;
                _popMainBundle.CustomFilter = pick =>
                {
                    var bundles = BundleQueryUtility.FindBundlesWithCharacteristic<LBSMainPopulationBundle>(includeChildren: true);
                    (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
                };
            }
            _popMainBundle.RegisterValueChangedCallback(
               (evt) => {
                   Bundle newBundle = evt.newValue as Bundle;
                   UpdateTagList(newBundle);
               }
            );

            _popToggleView = this.Q<ListView>("TagList");
            _popTagList = new();
            _popToggleView.itemsSource = _popTagList;
            _popToggleView.reorderable = false;
            _popToggleView.makeItem += () => new QuickAssistantToggle();
            _popToggleView.bindItem = (element, index) =>
            {
                var toggle = element as QuickAssistantToggle;
                if (toggle == null) return;

                toggle.Label = _popTagList[index].Key.Label;
                toggle.Value = _popTagList[index].Value.boolean;
                toggle.Quantity = _popTagList[index].Value.integer;
                toggle.SetToggleAction((value) =>
                {
                    _popTagList[index] = new (_popTagList[index].Key, new (value, _popTagList[index].Value.integer));
                });
            };

            _popSelectContextButton = this.Q<Button>("AddLayerButton");
            _popSelectContextButton.clicked += AddLayerMenu;

            _popContextView = this.Q<ListView>("LayerList");
            _popContextView.itemsSource = Data.contextLayers;
            _popContextView.reorderable = false;
            _popContextView.makeItem += () => new LayerContextEntry();
            _popContextView.bindItem = (element, index) =>
            {
                var layerContextVE = element as LayerContextEntry;
                if (layerContextVE == null) return;

                layerContextVE.UpdateData(Data.ContextLayers[index]);
                LBSLayer layer = layerContextVE.LayerReference;
                Data.OnContextChanged += (_) =>
                {
                    _popContextView.Rebuild();
                };
                layerContextVE.EvaluateOverlap(Data.ContextLayers);
                layerContextVE.OnRemoveButtonClicked = null;
                layerContextVE.OnRemoveButtonClicked += () =>
                {
                    ToggleLayerContext(layer);
                };
            };
        }

        public override void InitialSetup()
        {
            var match = _templates.FirstOrDefault(t => t.templateName.Contains("Population"));
            if (match != null)
            {
                var populationBehaviour = match.layer.Behaviours.FirstOrDefault(b => b is PopulationBehaviour) as PopulationBehaviour;

                if (populationBehaviour != null && populationBehaviour.MainBundle != null)
                {
                    _popMainBundle.value = populationBehaviour.MainBundle;
                    UpdateTagList(populationBehaviour.MainBundle);
                }
            }
        }

        public override async Task GenerateLayerProcess(LBSLayer newLayer)
        {
            if (newLayer == null) return;

            Random.InitState(int.Parse(_popSeed.value));

            Bundle selectedBundle = _popMainBundle.value as Bundle;
            if (selectedBundle == null)
            {
                //if (_exteriorWarning != null) _exteriorWarning.style.display = DisplayStyle.Flex;
                return;
            }

            // Get list of behaviours by layer
            LBSLayer[] contextLayers = Data.contextLayers.ToArray();
            List<SchemaBehaviour> interiorContexts = new ();
            List<ExteriorBehaviour> exteriorContexts = new();

            foreach(var layer in contextLayers)
            {
                var schema = layer.GetBehaviour<SchemaBehaviour>();
                if (schema is not null)
                {
                    interiorContexts.Add(schema);
                    continue;
                }

                var exterior = layer.GetBehaviour<ExteriorBehaviour>();
                if (exterior is not null)
                {
                    exteriorContexts.Add(exterior);
                    continue;
                }
            }

            // Get avaliable positions
            List<Vector2Int> avaliablePositions = new ();
            foreach(var schema in interiorContexts)
            {
                foreach (var zone in schema.Zones)
                {
                    var zoneTiles = schema.GetTiles(zone).ToArray();
                    foreach (var tile in zoneTiles)
                    {
                        if (avaliablePositions.Contains(tile.Position)) continue;
                        avaliablePositions.Add(tile.Position);
                    }
                }
            }
            foreach (var exterior in exteriorContexts)
            {
                foreach (var tile in exterior.Tiles)
                {
                    if (avaliablePositions.Contains(tile.Position)) continue;
                    avaliablePositions.Add(tile.Position);
                }
            }

            // Instance objects by tag
            var tagList = _popTagList.ToArray();
            for (int i = 0; i < tagList.Length; i++)
            {
                LBSTag tag = tagList[i].Key;
                BoolIntPair pair = tagList[i].Value;
                if (!pair.boolean) continue;

                List<Bundle> items = new();
                foreach (Bundle b in selectedBundle.ChildsBundles)
                {
                    if (b.GetHasTagCharacteristic(tag.label))
                    {
                        items.Add(b);
                    }
                }
                var itemPool = items.ToArray();

                for (int j = 0; j < pair.integer; j++)
                {
                    newLayer = PopulationPlaceItem(newLayer, tag, avaliablePositions, itemPool);
                }
            }
            return;
        }

        #endregion

        #region LOGIC METHODS
        private void UpdateTagList(Bundle newBundle)
        {
            _popTagList.Clear();
            if (newBundle == null)
            {
                _popToggleView.Rebuild();
                return;
            }

            var children = newBundle.GetChildrenCharacteristics<LBSTagsCharacteristic>();
            foreach (var cTags in children)
            {
                foreach (var tag in cTags.Tags)
                {
                    if (_popTagList.Exists(t => t.Key == tag)) continue;
                    _popTagList.Add(new KeyValuePair<LBSTag, BoolIntPair>(tag, new(true, 1)));
                }
            }
            _popToggleView.Rebuild();
        }

        private void AddLayerMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (LBSLayer layer in Data.Layers)
            {
                // It only takes InteriorLayers as context, but can be adapted to consider others
                if (layer.GetBehaviour<SchemaBehaviour>() is null && layer.GetBehaviour<ExteriorBehaviour>() is null) continue;
                menu.AddItem(new GUIContent(layer.Name), Data.ContextLayers.Contains(layer), ToggleLayerContext, layer);
            }
            menu.ShowAsContext();
        }

        private void ToggleLayerContext(object layer)
        {
            LBSLayer objectLayer = layer as LBSLayer;
            if (objectLayer == null)
            {
                Debug.LogError("Object Layer was null.");
                return;
            }

            if (Data.ContextLayers.Contains(layer))
            {
                Data.RemoveLayerFromContext(objectLayer);
            }
            else
            {
                Data.AddLayerToContext(objectLayer);
            }
            _popContextView.Rebuild();
        }

        private LBSLayer PopulationPlaceItem(LBSLayer populationLayer, LBSTag tag, List<Vector2Int> allPositions, Bundle[] itemPool)
        {
            // Get population Behaviour
            var popBehaviour = populationLayer.GetBehaviour<PopulationBehaviour>();
            if (popBehaviour is null)
            {
                Debug.LogWarning("[QuickAssistantPanel]: PopulationPlaceItem couldn't find a PopulationBehaviour on input layer.");
                return populationLayer;
            }

            // Choose random position
            int n = Random.Range(0, allPositions.Count);
            Vector2Int randomPos = allPositions[n];

            // Add item
            var tileGroup = popBehaviour.AddTileGroup(
                randomPos,
                new BundleData(itemPool[Random.Range(0, itemPool.Length)]),
                RandomRotation(),
                null).TileGroup;

            // Remove positions from list
            foreach (var tile in tileGroup)
            {
                allPositions.Remove(tile.Position);
            }

            return populationLayer;
        }

        private Vector2 RandomRotation()
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    return new Vector2(1, 0);
                case 1:
                    return new Vector2(-1, 0);
                case 2:
                    return new Vector2(0, 1);
                case 3:
                    return new Vector2(0, -1);
            }
            return new Vector2(1, 0);
        }
        #endregion
    }

    [SerializeField]
    public struct BoolIntPair
    {
        public bool boolean;
        public int integer;
        public BoolIntPair(bool b, int i)
        {
            boolean = b; integer = i;
        }
    }
}