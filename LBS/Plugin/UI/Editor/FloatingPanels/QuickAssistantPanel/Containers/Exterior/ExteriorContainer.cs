using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class ExteriorContainer : QuickAssistantContainer
    {
        public override string PrimaryKeyword { get => _primaryKeyword; }
        private const string _primaryKeyword = "Exterior";
        public override string SecondaryKeyword { get => 
                ((ConnectedTileMapModule.ConnectedTileType)_extType.value == 
                ConnectedTileMapModule.ConnectedTileType.VertexBased) ? "Vertex" : "Edge";
        }

        private static VisualTreeAsset visualTree;
        private const string UXML_NAME = "ExteriorContainer";
        private List<LayerTemplate> _templates;

        private LBSCustomEnumField _extType;
        private LBSCustomObjectField _extThemeBundle;
        private LBSCustomTextField _extSeed;
        private LBSCustomIntSlider _extWidth;
        private LBSCustomIntSlider _extHeight;
        private EnumFlagsField _extFlags;

        public ExteriorContainer(List<LayerTemplate> relatedTemplates)
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
            _extType = this.Q<LBSCustomEnumField>("ExtType");
            _extType?.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != null)
                {
                    AutoAssignExteriorBundle((ConnectedTileMapModule.ConnectedTileType)evt.newValue);
                }
            });
            _extThemeBundle = this.Q<LBSCustomObjectField>("ExtThemeBundle");
            if (_extThemeBundle != null)
            {
                _extThemeBundle.objectType = typeof(Bundle);
                _extThemeBundle.UseCustomFilter = true;
                _extThemeBundle.CustomFilter = pick =>
                {
                    var bundles = BundleQueryUtility.FindBundlesWithCharacteristic<LBSMainExteriorBundle>(includeChildren: true);
                    (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
                };
            }

            _extSeed = this.Q<LBSCustomTextField>("ExtSeed");
            _extSeed.style.display = DisplayStyle.None;

            _extWidth = this.Q<LBSCustomIntSlider>("ExtWidth");
            _extHeight = this.Q<LBSCustomIntSlider>("ExtHeight");

            _extFlags = this.Q<EnumFlagsField>("ExtFlag");
            if (_extFlags != null)
            {
                if (_extFlags.parent != null) _extFlags.parent.style.display = DisplayStyle.None;
                else _extFlags.style.display = DisplayStyle.None;
            }
        }

        public override void InitialSetup()
        {
            if (_extType != null)
            {
                AutoAssignExteriorBundle((ConnectedTileMapModule.ConnectedTileType)_extType.value);
            }
        }

        public override async Task GenerateLayerProcess(LBSLayer newLayer)
        {
            Bundle selectedBundle = _extThemeBundle.value as Bundle;
            if (selectedBundle == null)
            {
                //if (_exteriorWarning != null) _exteriorWarning.style.display = DisplayStyle.Flex;
                return;
            }

            if (newLayer == null) return;
            Random.InitState(int.Parse(_extSeed.value));

            var exteriorBehaviour = newLayer.Behaviours.FirstOrDefault(b => b is ExteriorBehaviour) as ExteriorBehaviour;
            if (exteriorBehaviour != null) exteriorBehaviour.Bundle = selectedBundle;

            List<Vector2Int> generatedPositions = FillLayerWithEmptyTiles(newLayer, _extWidth.value, _extHeight.value);
            RunWFC(newLayer, selectedBundle, generatedPositions);

            Debug.Log($"[QuickAssistant] Exterior generado.");
        }

        #endregion

        #region LOGIC METHODS
        private void AutoAssignExteriorBundle(ConnectedTileMapModule.ConnectedTileType type)
        {
            if (_templates == null || _templates.Count == 0 || _extThemeBundle == null) return;

            string typeKeyword = (type == ConnectedTileMapModule.ConnectedTileType.VertexBased) ? "Vertex" : "Edge";

            var match = _templates.FirstOrDefault(t =>
            t.templateName.Contains("Exterior") &&
            t.templateName.Contains(typeKeyword));

            if (match == null)
            {
                match = _templates.FirstOrDefault(t => t.templateName.Contains("Exterior"));
            }

            if (match != null)
            {
                var exteriorBehaviour = match.layer.Behaviours.FirstOrDefault(b => b is ExteriorBehaviour) as ExteriorBehaviour;

                if (exteriorBehaviour != null && exteriorBehaviour.Bundle != null)
                {
                    _extThemeBundle.value = exteriorBehaviour.Bundle;
                }
            }
        }

        private List<Vector2Int> FillLayerWithEmptyTiles(LBSLayer layer, int width, int height)
        {
            var tileMap = layer.GetModule<TileMapModule>();
            var connectedMap = layer.GetModule<ConnectedTileMapModule>();
            var positions = new List<Vector2Int>();
            if (tileMap == null || connectedMap == null) return positions;

            var emptyConnections = new List<string> { "", "", "", "" };
            var emptyMeta = new List<bool> { false, false, false, false };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    positions.Add(pos);
                    LBSTile newTile = new LBSTile(pos);
                    tileMap.AddTile(newTile);
                    connectedMap.AddPair(newTile, emptyConnections, emptyMeta);
                }
            }
            return positions;
        }

        private void RunWFC(LBSLayer layer, Bundle bundle, List<Vector2Int> positions)
        {
            AssistantWFC wfc = new AssistantWFC(System.Guid.NewGuid().ToString(), "QuickWFC", Color.white, bundle);
            wfc.OwnerLayer = layer;
            wfc.Positions = positions;
            wfc.OverrideValues = true;
            wfc.SafeMode = true;
            bool hasChanceRules = bundle.GetCharacteristics<LBSDirectionedChance>().Count > 0;
            bool success = hasChanceRules ? wfc.ExecuteChance() : wfc.TryExecute(out string log, out LogType type);
            if (!success) Debug.LogWarning($"[QuickAssistant] WFC termin� con advertencias.");
        }

        #endregion
    }
}