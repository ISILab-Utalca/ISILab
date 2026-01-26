using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.Internal.Editor;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.Components.Data;

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
        private VisualElement _containerExterior;
        private VisualElement _containerInterior;

        private LBSCustomEnumField _extType;
        private LBSCustomObjectField _extThemeBundle;
        private LBSCustomTextField _extSeed;
        private LBSCustomIntSlider _extWidth;
        private LBSCustomIntSlider _extHeight;
        private EnumFlagsField _extFlags;

        private LBSCustomTextField _intSeed;
        private LBSCustomIntSlider _intRoomSize;
        private LBSCustomIntSlider _intRoomCount;
        private LBSCustomToggleField _intMultiFloor;
        private LBSCustomToggleField _intOptimized;
        private LBSCustomEnumField _intMode;
        private EnumFlagsField _intFlags;
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
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>(UXML_NAME);
            if (visualTree != null) visualTree.CloneTree(this);
            else Debug.LogError($"[QuickAssistantPanel] No se encontró el UXML: {UXML_NAME}");
            LoadVisualElements();
            InitDefaultState();
        }
        #endregion

        #region INITIALIZATION
        public void Setup(List<LayerTemplate> templates) { _templates = templates; }

        private void LoadVisualElements()
        {
            _layerTypeSelector = this.Q<LBSCustomDropdown>("LayerType");
            _runButton = this.Q<LBSCustomButton>("RunButton");
            _exteriorWarning = this.Q<WarningPanel>("ExteriorWarning");
            _foldoutSettings = this.Q<LBSCustomFoldout>("FoldoutSettings");
            _containerExterior = this.Q<VisualElement>("ContainerExterior");
            _containerInterior = this.Q<VisualElement>("ContainerInterior");

            if (_containerExterior != null)
            {
                _extType = _containerExterior.Q<LBSCustomEnumField>("ExtType");
                _extThemeBundle = _containerExterior.Q<LBSCustomObjectField>("ExtThemeBundle");
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

                _extSeed = _containerExterior.Q<LBSCustomTextField>("ExtSeed");
                if (_extSeed != null) _extSeed.style.display = DisplayStyle.None;

                _extWidth = _containerExterior.Q<LBSCustomIntSlider>("ExtWidth");
                _extHeight = _containerExterior.Q<LBSCustomIntSlider>("ExtHeight");

                _extFlags = _containerExterior.Q<EnumFlagsField>("ExtFlag");
                if (_extFlags != null)
                {
                    if (_extFlags.parent != null) _extFlags.parent.style.display = DisplayStyle.None;
                    else _extFlags.style.display = DisplayStyle.None;
                }
            }

            if (_containerInterior != null)
            {
                _intSeed = _containerInterior.Q<LBSCustomTextField>("IntSeed");
                if (_intSeed != null) _intSeed.style.display = DisplayStyle.None;

                _intRoomSize = _containerInterior.Q<LBSCustomIntSlider>("IntRoomSize");
                _intRoomCount = _containerInterior.Q<LBSCustomIntSlider>("IntRoomCount");

                _intMultiFloor = _containerInterior.Q<LBSCustomToggleField>("IntMultiFloor");
                if (_intMultiFloor != null) _intMultiFloor.style.display = DisplayStyle.None;

                _intFlags = _containerInterior.Q<EnumFlagsField>("IntFlag");
                if (_intFlags != null)
                {
                    if (_intFlags.parent != null) _intFlags.parent.style.display = DisplayStyle.None;
                    else _intFlags.style.display = DisplayStyle.None;
                }

                _intOptimized = _containerInterior.Q<LBSCustomToggleField>("IntOptimized");

                _intMode = _containerInterior.Q<LBSCustomEnumField>("IntMode");
                if (_intMode != null)
                {
                    _intMode.Init(InteriorGenerationMode.GridWalker);
                }
            }

            _layerTypeSelector?.RegisterValueChangedCallback(evt => UpdateVisibility(evt.newValue?.ToString()));
            if (_runButton != null) _runButton.clicked += GenerateLayer;
        }

        private void InitDefaultState()
        {
            if (_layerTypeSelector != null) _layerTypeSelector.index = -1;
            UpdateVisibility(null);
        }
        #endregion

        #region LOGIC METHODS
        private void UpdateVisibility(string mode)
        {
            bool showExterior = mode == "Exterior";
            bool showInterior = mode == "Interior";
            if (_containerExterior != null) _containerExterior.style.display = showExterior ? DisplayStyle.Flex : DisplayStyle.None;
            if (_containerInterior != null) _containerInterior.style.display = showInterior ? DisplayStyle.Flex : DisplayStyle.None;
            if (_exteriorWarning != null) _exteriorWarning.style.display = DisplayStyle.None;
        }

        private void GenerateLayer()
        {
            if (_layerTypeSelector == null || _layerTypeSelector.value == null) return;
            string mode = _layerTypeSelector.value.ToString();
            if (mode == "Exterior") GenerateExteriorProcess();
            else if (mode == "Interior") GenerateInteriorProcess();
        }

        private void GenerateExteriorProcess()
        {
            Bundle selectedBundle = _extThemeBundle.value as Bundle;
            if (selectedBundle == null)
            {
                if (_exteriorWarning != null) _exteriorWarning.style.display = DisplayStyle.Flex;
                return;
            }

            var selectedType = (ConnectedTileMapModule.ConnectedTileType)_extType.value;
            string typeKeyword = (selectedType == ConnectedTileMapModule.ConnectedTileType.VertexBased) ? "Vertex" : "Edge";

            LBSLayer newLayer = CreateBaseLayer("Exterior", typeKeyword);
            if (newLayer == null) return;

            var exteriorBehaviour = newLayer.Behaviours.FirstOrDefault(b => b is ExteriorBehaviour) as ExteriorBehaviour;
            if (exteriorBehaviour != null) exteriorBehaviour.Bundle = selectedBundle;

            List<Vector2Int> generatedPositions = FillLayerWithEmptyTiles(newLayer, _extWidth.value, _extHeight.value);
            RunWFC(newLayer, selectedBundle, generatedPositions);

            FinalizeLayer(newLayer);
            Debug.Log($"[QuickAssistant] Exterior generado.");
        }

        private void GenerateInteriorProcess()
        {
            int roomSize = _intRoomSize.value;
            int maxRooms = _intRoomCount.value;
            InteriorGenerationMode currentMode = (InteriorGenerationMode)_intMode.value;
            bool useOptimization = _intOptimized.value;

            LBSLayer newLayer = CreateBaseLayer("Interior");
            if (newLayer == null) return;

            var schema = newLayer.GetBehaviour<SchemaBehaviour>();
            if (schema == null) return;

            Debug.Log($"[QuickAssistant] Generando Semilla Interior ({currentMode})...");

            switch (currentMode)
            {
                case InteriorGenerationMode.GridWalker:
                    Dictionary<Vector2Int, Zone> gridLayout = new Dictionary<Vector2Int, Zone>();
                    int gridUnitStep = roomSize + 2;
                    PlaceRoomsGridWalker(schema, gridLayout, maxRooms, roomSize, gridUnitStep);
                    schema.RecalculateWalls();
                    ConnectGridNeighbors(newLayer, gridLayout);
                    break;

                case InteriorGenerationMode.Spiral:
                    List<Zone> spiralRooms = new List<Zone>();
                    int separationPadding = 2;
                    GenerateSpiralRooms(schema, spiralRooms, maxRooms, roomSize, separationPadding);
                    schema.RecalculateWalls();
                    ConnectSpiralChain(newLayer, spiralRooms);
                    break;
            }

            FinalizeLayer(newLayer);
            Debug.Log("[QuickAssistant] Semilla generada y guardada.");

            if (useOptimization)
            {
                RunHillClimbingOptimization(newLayer);
            }
        }

        private void FinalizeLayer(LBSLayer layer)
        {
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
        private async void RunHillClimbingOptimization(LBSLayer layer)
        {
            Debug.Log("[QuickAssistant] Iniciando Optimización IA...");
            EditorUtility.DisplayProgressBar("Quick Assistant", "Optimizando Distribución...", 0.3f);

            var optimizer = new HillClimbingAssistant(System.Guid.NewGuid().ToString(), "AutoOptimizer", Color.cyan);
            optimizer.OnAttachLayer(layer);

            try
            {
                await Task.Run(() =>
                {
                    try { optimizer.TryExecute(out string log, out LogType type, null, default); }
                    catch (System.Exception ex) { Debug.LogWarning($"HillClimbing Error: {ex.Message}"); }
                });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            optimizer.OnDetachLayer(layer);
            FinalizeLayer(layer);
            Debug.Log("[QuickAssistant] Optimización Finalizada.");
        }

        private void PlaceRoomsGridWalker(SchemaBehaviour schema, Dictionary<Vector2Int, Zone> grid, int maxRooms, int roomSize, int step)
        {
            List<Vector2Int> potentialSpots = new List<Vector2Int>();
            Vector2Int currentGridPos = Vector2Int.zero;

            AddRoomAtGrid(schema, grid, currentGridPos, roomSize, step);
            UpdatePotentialSpots(potentialSpots, grid, currentGridPos);

            int safety = 0;
            while (grid.Count < maxRooms && safety < 1000 && potentialSpots.Count > 0)
            {
                safety++;
                int index = Random.Range(0, potentialSpots.Count);
                Vector2Int candidate = potentialSpots[index];
                potentialSpots.RemoveAt(index);

                if (!grid.ContainsKey(candidate))
                {
                    AddRoomAtGrid(schema, grid, candidate, roomSize, step);
                    UpdatePotentialSpots(potentialSpots, grid, candidate);
                }
            }
        }

        private void ConnectGridNeighbors(LBSLayer layer, Dictionary<Vector2Int, Zone> grid)
        {
            var graphModule = layer.GetModule<ConnectedZonesModule>();
            if (graphModule == null) return;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right };

            foreach (var kvp in grid)
            {
                Vector2Int currentPos = kvp.Key;
                Zone currentZone = kvp.Value;

                foreach (var dir in directions)
                {
                    Vector2Int neighborPos = currentPos + dir;
                    if (grid.TryGetValue(neighborPos, out Zone neighborZone))
                    {
                        if (!graphModule.EdgesConnected(currentZone, neighborZone))
                            graphModule.AddEdge(currentZone, neighborZone);
                    }
                }
            }
        }

        private void AddRoomAtGrid(SchemaBehaviour schema, Dictionary<Vector2Int, Zone> grid, Vector2Int gridPos, int size, int step)
        {
            Zone newZone = schema.AddZone();
            ApplyStyles(schema, newZone);
            Vector2Int worldPos = gridPos * step;
            Vector2Int startTilePos = worldPos - new Vector2Int(size / 2, size / 2);
            CreateRoomTiles(schema, newZone, startTilePos, size);
            grid.Add(gridPos, newZone);
        }

        private void UpdatePotentialSpots(List<Vector2Int> spots, Dictionary<Vector2Int, Zone> grid, Vector2Int center)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in dirs)
            {
                Vector2Int neighbor = center + dir;
                if (!grid.ContainsKey(neighbor) && !spots.Contains(neighbor)) spots.Add(neighbor);
            }
        }

        private void GenerateSpiralRooms(SchemaBehaviour schema, List<Zone> rooms, int maxRooms, int roomSize, int padding)
        {
            for (int i = 0; i < maxRooms; i++)
            {
                Zone newZone = schema.AddZone();
                ApplyStyles(schema, newZone);

                if (PlaceRoomSpiral(schema, newZone, roomSize, padding))
                {
                    rooms.Add(newZone);
                }
                else
                {
                    schema.RemoveZone(newZone);
                    break;
                }
            }
        }

        private void ConnectSpiralChain(LBSLayer layer, List<Zone> rooms)
        {
            if (rooms.Count < 2) return;
            var graphModule = layer.GetModule<ConnectedZonesModule>();
            if (graphModule == null) return;

            for (int i = 0; i < rooms.Count - 1; i++)
            {
                if (!graphModule.EdgesConnected(rooms[i], rooms[i + 1]))
                    graphModule.AddEdge(rooms[i], rooms[i + 1]);
            }
            if (rooms.Count > 2)
            {
                if (!graphModule.EdgesConnected(rooms[0], rooms[rooms.Count - 1]))
                    graphModule.AddEdge(rooms[0], rooms[rooms.Count - 1]);
            }
        }

        private bool PlaceRoomSpiral(SchemaBehaviour schema, Zone zone, int size, int padding)
        {
            float angleStep = 0.5f; float radiusStep = 0.5f;
            float currentAngle = 0; float currentRadius = 0;
            int maxAttempts = 500;

            for (int i = 0; i < maxAttempts; i++)
            {
                int x = Mathf.RoundToInt(currentRadius * Mathf.Cos(currentAngle));
                int y = Mathf.RoundToInt(currentRadius * Mathf.Sin(currentAngle));
                Vector2Int startPos = new Vector2Int(x, y) - new Vector2Int(size / 2, size / 2);

                if (IsAreaFree(schema, startPos, size, padding))
                {
                    CreateRoomTiles(schema, zone, startPos, size);
                    return true;
                }
                currentAngle += angleStep;
                currentRadius += radiusStep * 0.2f;
            }
            return false;
        }

        private void ApplyStyles(SchemaBehaviour schema, Zone zone)
        {
            if (schema.PressetInsideStyle != null) zone.InsideStyles = new List<string>() { schema.PressetInsideStyle.Name };
            if (schema.PressetOutsideStyle != null) zone.OutsideStyles = new List<string>() { schema.PressetOutsideStyle.Name };
        }

        private bool IsAreaFree(SchemaBehaviour schema, Vector2Int startPos, int size, int padding)
        {
            for (int x = -padding; x < size + padding; x++)
            {
                for (int y = -padding; y < size + padding; y++)
                {
                    if (schema.GetTile(startPos + new Vector2Int(x, y)) != null) return false;
                }
            }
            return true;
        }

        private void CreateRoomTiles(SchemaBehaviour schema, Zone zone, Vector2Int startPos, int size)
        {
            var defaultConnections = new List<string> { "Empty", "Empty", "Empty", "Empty" };
            var defaultMeta = new List<bool> { true, true, true, true };
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    LBSTile newTile = schema.AddTile(startPos + new Vector2Int(x, y), zone);
                    if (newTile != null) schema.AddConnections(newTile, defaultConnections, defaultMeta);
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
            if (!success) Debug.LogWarning($"[QuickAssistant] WFC terminó con advertencias.");
        }

        private LBSLayer CreateBaseLayer(string primaryKeyword, string secondaryKeyword = null)
        {
            if (_templates == null || _templates.Count == 0) return null;
            var candidates = _templates.Where(t => t.templateName.Contains(primaryKeyword)).ToList();
            if (candidates.Count == 0) return null;

            LayerTemplate targetTemplate = null;
            if (!string.IsNullOrEmpty(secondaryKeyword))
                targetTemplate = candidates.FirstOrDefault(t => t.templateName.Contains(secondaryKeyword));
            if (targetTemplate == null) targetTemplate = candidates[0];

            if (targetTemplate.layer.Clone() is LBSLayer newLayer)
            {
                newLayer.Name = targetTemplate.templateName;
                LBSMainWindow.Instance.layerPanel.AddLayer(newLayer);
                return newLayer;
            }
            return null;
        }
        #endregion
    }
}