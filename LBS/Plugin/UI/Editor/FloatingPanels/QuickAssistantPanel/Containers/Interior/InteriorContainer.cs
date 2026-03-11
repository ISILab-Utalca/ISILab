using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.MapTools.Editor.Templates;
using ISILab.LBS.Plugin.UI.Editor.CustomComponents;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.VisualElements.QuickAssistantPanel;

namespace ISILab.LBS.VisualElements
{
    public class InteriorContainer : QuickAssistantContainer
    {
        public override string PrimaryKeyword { get => _primaryKeyword; }
        private const string _primaryKeyword = "Interior";
        public override string SecondaryKeyword { get => _secondaryKeyword; }
        private const string _secondaryKeyword = null;

        private static VisualTreeAsset visualTree;
        private const string UXML_NAME = "InteriorContainer";
        private List<LayerTemplate> _templates;

        private LBSCustomTextField _intSeed;
        private LBSCustomIntSlider _intRoomSize;
        private LBSCustomIntSlider _intRoomCount;
        private LBSCustomToggleField _intMultiFloor;
        private LBSCustomToggleField _intOptimized;
        private LBSCustomEnumField _intMode;
        private EnumFlagsField _intFlags;

        public InteriorContainer(List<LayerTemplate> relatedTemplates)
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
            _intSeed = this.Q<LBSCustomTextField>("IntSeed");
            _intSeed.style.display = DisplayStyle.None;

            _intRoomSize = this.Q<LBSCustomIntSlider>("IntRoomSize");
            _intRoomCount = this.Q<LBSCustomIntSlider>("IntRoomCount");

            _intMultiFloor = this.Q<LBSCustomToggleField>("IntMultiFloor");
            if (_intMultiFloor != null) _intMultiFloor.style.display = DisplayStyle.None;

            _intFlags = this.Q<EnumFlagsField>("IntFlag");
            if (_intFlags != null)
            {
                if (_intFlags.parent != null) _intFlags.parent.style.display = DisplayStyle.None;
                else _intFlags.style.display = DisplayStyle.None;
            }

            _intOptimized = this.Q<LBSCustomToggleField>("IntOptimized");

            _intMode = this.Q<LBSCustomEnumField>("IntMode");
            if (_intMode != null)
            {
                _intMode.Init(InteriorGenerationMode.GridWalker);
            }
        }

        public override void InitialSetup() { }

        public override async Task GenerateLayerProcess(LBSLayer newLayer)
        {
            if (newLayer == null) return;
            Random.InitState(int.Parse(_intSeed.value));

            int roomSize = _intRoomSize.value;
            int maxRooms = _intRoomCount.value;
            InteriorGenerationMode currentMode = (InteriorGenerationMode)_intMode.value;
            bool useOptimization = _intOptimized.value;

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

            Debug.Log("[QuickAssistant] Semilla generada y guardada.");

            if (useOptimization)
            {
                await RunHillClimbingOptimization(newLayer);
            }
        }


        #endregion

        #region LOGIC METHODS
        private async Task RunHillClimbingOptimization(LBSLayer layer)
        {
            Debug.Log("[QuickAssistant] Iniciando Optimizaci�n IA...");

            Undo.RegisterCompleteObjectUndo(LBSController.CurrentLevel, "Quick Optimization");

            var optimizer = new HillClimbingAssistant(System.Guid.NewGuid().ToString(), "AutoOptimizer", Color.cyan);
            optimizer.OnAttachLayer(layer);

            var tokenSource = new System.Threading.CancellationTokenSource();
            var token = tokenSource.Token;

            ToolBarMain taskBar = LBSMainWindow.Instance.rootVisualElement.Q<ToolBarMain>();

            if (taskBar != null)
            {
                taskBar.EnableProcess(true, "Quick Assistant Optimization");
            }
            else
            {
                EditorUtility.DisplayProgressBar("Quick Assistant", "Iniciando...", 0f);
            }
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        optimizer.TryExecute(out string log, out LogType type, (progress) =>
                        {
                            EditorApplication.delayCall += () =>
                            {
                                if (taskBar != null)
                                {
                                    taskBar.SetProgressPercent(progress);
                                }
                                else
                                {
                                    EditorUtility.DisplayProgressBar("Quick Assistant", $"Optimizando... {progress * 100:F0}%", progress);
                                }
                            };
                        }, token);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"HillClimbing Error: {ex.Message}");
                    }
                });
            }
            finally
            {
                if (taskBar != null)
                {
                    taskBar.EnableProcess(false);
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                }

                tokenSource.Dispose();
                optimizer.OnDetachLayer(layer);

                Debug.Log("[QuickAssistant] Optimizaci�n Finalizada.");
            }
        }
        private void ApplyStyles(SchemaBehaviour schema, Zone zone)
        {
            if (schema.PressetInsideStyle != null) zone.InsideStyles = new List<string>() { schema.PressetInsideStyle.Name };
            if (schema.PressetOutsideStyle != null) zone.OutsideStyles = new List<string>() { schema.PressetOutsideStyle.Name };
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

        #region GRID WALKER METHODS
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
        #endregion

        #region SPIRAL ROOMS MEHTODS
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
        #endregion
        #endregion
    }
}