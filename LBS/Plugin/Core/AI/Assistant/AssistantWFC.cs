using ISILab.Commons;
using ISILab.Commons.Extensions;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Assistants;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using static ISILab.LBS.Characteristics.LBSDirectionedChance;
using static UnityEngine.GraphicsBuffer;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [System.Serializable]
    [RequieredModule(typeof(TileMapModule), typeof(ConnectedTileMapModule))]
    public class AssistantWFC : LBSAssistant, IAssistantThreaded
    {
        #region FIELDS
        [SerializeField, JsonRequired, HideInInspector]
        private bool overrideValues;

        [JsonProperty, SerializeReference, SerializeField, JsonRequired]
        private Bundle targetBundleRef;
        
        /***
         * Use asset's GUID; current bundle:
         * - "Exterior_Plains" 
         */
        private string defaultBundleGuid = "9d3dac0f9a486fd47866f815b4fefc29";

        private ConnectedTileMapModule.ConnectedTileType? gridType;

        private bool safeMode;
        private List<Vector2Int> originalPositions = new();

        private Vector2Int bottomLeft = Vector2Int.zero;
        
        // run execute fields
        private LBSDirectionedGroup group;
        private LBSDirectionedChance chance;
        private TileMapModule map;
        private ConnectedTileMapModule connected;
        private List<LBSModule> og;
        private ConnectedTileMapModule originalTM;

        const int MAX_MEMORY = 3, MAX_RETRIES = 5;
        const int SAVE_STATE_INTERVAL = 10;
        const int MAX_SIZE_X = 10;
        const int MAX_SIZE_Y = 10;

        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public bool OverrideValues
        {
            get => overrideValues;
            set => overrideValues = value;
        }

        [JsonIgnore]
        public List<Vector2Int> Positions { get; set; }

        [JsonIgnore]
        public Bundle Bundle
        {
            get => GetBundleRef();
            set => targetBundleRef = value;
        }

        [JsonIgnore]
        private List<Vector2Int> Dirs
        {
            get
            {
                switch(GridType)
                {
                    case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                        return Directions.Bidimencional.Edges;
                    case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                        return Directions.Bidimencional.All;
                }
                return new List<Vector2Int>();
            }
        }

        [JsonIgnore]
        public ConnectedTileMapModule.ConnectedTileType GridType
        {
            get
            {
                if(!gridType.HasValue)
                {
                    gridType = OwnerLayer.GetModule<ConnectedTileMapModule>().GridType;
                }

                return gridType.Value;
            }
        }

        [JsonIgnore]
        public bool SafeMode
        {
            get => safeMode;
            set => safeMode = value;
        }

        #endregion

        #region EVENTS

        public Action OnRefreshInspector;

        #endregion

        #region CONSTRUCTORS

        public AssistantWFC(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
            SafeMode = true;
            OnGUI();
        }

        public AssistantWFC(string IconGuid, string name, Color colorTint, Bundle targetBundleRef = null) : base(IconGuid, name, colorTint)
        {
            if(targetBundleRef is not null)
                this.targetBundleRef = targetBundleRef;
            SafeMode = true;
            OnGUI(); 
        }
        
        #endregion

        #region METHODS

        public sealed override void OnGUI()
        {
            GetBundleRef();
        }

        public override object Clone()
        {
            return new AssistantWFC(IconGuid, Name, ColorTint, targetBundleRef);
        }

        public bool ExecuteTest(bool overrideValues, out string log)
        {
            Positions = new List<Vector2Int>();
            this.overrideValues = overrideValues;
            Rect bounds = OwnerLayer.GetModule<TileMapModule>().GetBounds();
            for (int i = (int)bounds.x; i < (int)(bounds.x + bounds.width); i++)
            {
                for (int j = (int)bounds.y; j < (int)(bounds.y + bounds.height); j++)
                {
                    Positions.Add(new Vector2Int(i, j));
                }
            }

            return TryExecute(out log, out _);
        }

        public bool TryExecute(out string log, out LogType logType, int limit = 5, Action<float> onProgress = null, CancellationToken token = default)
        {
            log = "";
            logType = LogType.Log;

            if (targetBundleRef == null)
            {
                log = "No bundle selected.";
                logType = LogType.Warning;
                return false;
            }
            
            Bundle bundle = targetBundleRef;

            // get values for generation: 
            var g = bundle.GetCharacteristics<LBSDirectionedGroup>();
            group = g.Count > 0 ? g[0] : null;
            var c = bundle.GetCharacteristics<LBSDirectionedChance>();
            chance = c.Count > 0 ? c[0] : null;

            if(group is null && chance is null)
            {
                log = $"{bundle.BundleName} is missing 'Directioned' characteristic.";
                logType = LogType.Warning;
                return false;
            }

            map = OwnerLayer.GetModule<TileMapModule>();
            connected = OwnerLayer.GetModule<ConnectedTileMapModule>();
            og = new List<LBSModule>() { OwnerLayer.GetModule<ConnectedTileMapModule>() };
            originalTM = og.Clone()[0] as ConnectedTileMapModule;
            

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Func<double> getSeconds = () =>
            {
                sw.Stop();
                long ticks = sw.ElapsedTicks;
                return (double)ticks / System.Diagnostics.Stopwatch.Frequency;
            };

            originalPositions = new List<Vector2Int>(Positions);

            if (safeMode)
            {
                int xStart = Positions.OrderBy(p => p.x).First().x;
                int yStart = Positions.OrderBy(p => p.y).First().y;
                bottomLeft = new Vector2Int(xStart, yStart);
                int xEnd = Positions.OrderBy(p => -p.x).First().x;
                int yEnd = Positions.OrderBy(p => -p.y).First().y;
                int width = xEnd - xStart + 1;
                int height = yEnd - yStart + 1;
                RectInt rect = new RectInt(new Vector2Int(xStart, yStart), new Vector2Int(width, height));
                int xSectors = Mathf.CeilToInt((float)rect.width / (float)MAX_SIZE_X);
                int ySectors = Mathf.CeilToInt((float)rect.height / (float)MAX_SIZE_Y);
                int sectorSizeX = Mathf.CeilToInt((float)width / (float)xSectors);
                int sectorSizeY = Mathf.CeilToInt((float)height / (float)ySectors);
                Vector2Int sectorSize = new Vector2Int(sectorSizeX, sectorSizeY);
                List<RectInt> sectors = new List<RectInt>();
                for(int i = 0; i < xSectors; i++)
                {
                    for(int j = 0; j < ySectors; j++)
                    {
                        Vector2Int offset = new Vector2Int(sectorSizeX * i, sectorSizeY * j);
                        RectInt sector = new RectInt(rect.position + offset, new Vector2Int(sectorSizeX, sectorSizeY));
                        sectors.Add(sector);
                    }
                }
                
                int totalSectors = sectors.Count;
                
                for (int i = 0; i < limit; i++)
                {
                    int sectorSuccessCount = 0;
                    onProgress?.Invoke(0);
                    for (int s = 0; s < sectors.Count; s++)
                    {
                        var sector = sectors[s];
                        
                        float baseProgress = (float)sectorSuccessCount / totalSectors;

                        // Each sector contributes an equal portion
                        float stepSize = 1f / totalSectors;

                        Action<float> scaledProgress = localProgress =>
                        {
                            float totalProgress = baseProgress + localProgress * stepSize;
                            onProgress?.Invoke(Mathf.Clamp01(totalProgress));
                        };


                        // Build positions as before
                        List<Vector2Int> positions = new List<Vector2Int>();
                        for (int j = sector.position.x; j < sector.position.x + sector.width; j++)
                        {
                            for (int k = sector.position.y; k < sector.position.y + sector.height; k++)
                            {
                                positions.Add(new Vector2Int(j, k));
                            }
                        }

                        Positions = positions;
                        bool sectorSuccess = false;
                        if (chance is not null) sectorSuccess = ExecuteChance(ref log, scaledProgress, token);
                        else if (group is not null) sectorSuccess = Execute(ref log, scaledProgress, token);

                        // exit
                        if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                        {
                            log = "Generation was cancelled.";
                            logType = LogType.Warning;
                            return false;
                        }
                        
                        if (sectorSuccess) sectorSuccessCount++;
                        else break;
                        
                        if (sectorSuccessCount >= sectors.Count)
                        {
                            onProgress?.Invoke(1f);
                            log = $"Safely generated after {i + 1} attempts.";
                            Thread.Sleep(1);
                            return true;
                        }
                    }
                    
                    Thread.Sleep(1);
                    
                    if (sectorSuccessCount >= sectors.Count)
                    {
                        log = $"Safely generated after {i + 1} attempts. ({getSeconds()} s)";
                        return true;
                    }
                }
                
                // exit
                if(((IAssistantThreaded)this).CheckPendingCancel(this, token))
                {
                    log = "Generation was cancelled.";
                    logType = LogType.Warning;
                    return false;
                }
                
                OnTaskCancelled();
                log = $"Could not safely generate after {limit} attempts. ({getSeconds()} s)";
                logType = LogType.Warning;
                return false;
            }
            else
            {
                bool success = false;
                if (chance is not null) success = ExecuteChance(ref log, onProgress, token);
                else if (group is not null) success = Execute(ref log, onProgress, token);

                if (!success)
                {
                    //Restore();
                    if (log == string.Empty)
                    {
                        //log = $"Could not safely generate after {limit} attempts. ({getSeconds()} s)";
                        log = "Unsafely generated";
                    }
                    logType = LogType.Warning;
                    return false;
                }
                log = $"Generated. ({getSeconds()} s)";
                return true;
            }
        }

        public void RequestRepaint()
        {
            var connected = OwnerLayer.GetModule<ConnectedTileMapModule>();
            ExteriorBehaviour exterior = OwnerLayer.GetBehaviour<ExteriorBehaviour>();

            var ogPairs = originalPositions.Select(pos => connected.GetPair(pos)).ToList().RemoveEmpties();
            exterior.RequestTilesRepaint(ogPairs.Select(p => p.Tile));
            var others = new List<Vector2Int>();
            int minX = originalPositions.Min(pos => pos.x) - 1;
            int maxX = originalPositions.Max(pos => pos.x) + 1;
            int minY = originalPositions.Min(pos => pos.y) - 1;
            int maxY = originalPositions.Max(pos => pos.y) + 1;
            for(int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if(x == minX || x == maxX || y == minY || y == maxY)
                    {
                        others.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            var pairs = others.Select(pos => connected.GetPair(pos)).ToList().RemoveEmpties();
            exterior.RequestTilesRepaint(pairs.Select(p => p.Tile));
            //originalPositions.ForEach(pos => RequestTilePaint(connected.GetPair(pos).Tile));
        }
        
        /// <summary>
        /// This new version, is similar but it constraints where the wave function collapse is applied, to the selected tiles only
        /// </summary>
        public bool Execute(ref string log, Action<float> onProgress = null, CancellationToken token = default)
        {
            bool success = false;

            int initialRetryBonus = 10;
            (int, int) retryCount = (MAX_MEMORY, MAX_RETRIES + initialRetryBonus);
            int step = 0, maxStep = 0;

            // Paso 1
            // Get tiles to change
            List<LBSTile> toCalc = GetTileToCalc(map, connected);

            // Build whitelist (positions + direct neighbors)
            // and selection area neighbourhood
            var whitelist = new HashSet<Vector2Int>();
            var areaNeighbours = new List<(LBSTile, int)>();
            bool implemented = true;
            foreach (LBSTile tile in toCalc)
            {
                whitelist.Add(tile.Position);
                List<LBSTile> neighbours = map.GetTileNeighbors(tile, Dirs);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i] == null) continue;

                    bool isAreaNeighbour = !toCalc.Contains(neighbours[i]);
                    bool haveEmpties = connected.GetConnections(neighbours[i]).Contains("");

                    if (isAreaNeighbour && haveEmpties)
                        continue;

                    whitelist.Add(neighbours[i].Position);

                    if (isAreaNeighbour)
                    {
                        switch (GridType)
                        {
                            case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                                areaNeighbours.Add((neighbours[i], (i + 2) % 4));
                                break;
                            case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                                implemented = false;
                                break;
                        }
                    }
                }
            }
            if (implemented)
            {
                foreach ((LBSTile, int) areaNeighbour in areaNeighbours)
                {
                    connected.SetConnection(areaNeighbour.Item1, areaNeighbour.Item2, "", false);
                    toCalc.Add(areaNeighbour.Item1);
                }
            }
            else Debug.LogWarning("Unhandled case for Vertex-based grid. Could not build area neighbourhood.");

            //Lista que guarda los tiles ya generados
            var closed = new List<LBSTile>();

            //Tiles que tienen que recalcular los candidatos despu�s de generar un tile
            //generalmente van los tiles vecinos del que se gener�
            var reCalc = new List<LBSTile>();

            //Diccionario que guarda los candidatos posibles para cada tile
            var currentCalcs = new Dictionary<LBSTile, List<Candidate>>();

            //Paso 2
            foreach (LBSTile tile in toCalc)
            {
                List<Candidate> candidates = CalcCandidates(tile, group);
                currentCalcs.Add(tile, candidates);
            }

            List<WFCState> states = new List<WFCState>();
            if(safeMode)
            {
                states.Add(new WFCState(0, connected, toCalc, closed, currentCalcs));
            }
            bool stepSuccess = true;
            int tryCount = 0;
            int toCalcSize = toCalc.Count;
            
            /// MAIN LOOP
            while (toCalc.Count > 0)
            {
                if(((IAssistantThreaded)this).CheckPendingCancel(this, token))
                {
                    log = "Generation cancelled.";
                    return false;
                }
                
                tryCount++;

                //Paso 3

                List<KeyValuePair<LBSTile, List<Candidate>>> xx = safeMode ? 
                    currentCalcs.Where(e => !closed.Contains(e.Key)).ToList() :
                    currentCalcs.Where(e => e.Value.Count > 1).ToList();

                if (xx.Count <= 0)
                    break;

                // Paso 3.5

                KeyValuePair<LBSTile, List<Candidate>> current = xx.OrderBy(e => e.Value.Count).First();

                // If cannot generate next tile
                if(safeMode && (!stepSuccess || current.Value.Count <= 0))
                {
                    if (Backtrack(states, ref retryCount, connected, originalTM, ref step, maxStep, out _, ref toCalc, ref closed, ref currentCalcs))
                    {
                        stepSuccess = true;
                        //Debug.Log($"TRY: {tryCount}\tSTEP {step}\tMAX STEP {maxStep}\tRETRY COUNT {retryCount}");
                        continue;
                    }

                    return false;

                }

                stepSuccess = true;

                //Paso 4

                //a
                Candidate selected = current.Value.RandomRullete(c => c.weigth);

                //b
                LBSDirection d = selected.bundle;
                List<string> connections = d.GetConnection(selected.rotation).ToList();
                string center = d.Center;
                
                //c
                connected.SetConnections(current.Key, connections, new List<bool>() { false, false, false, false });
                connected.SetCenter(current.Key, center);
                
                //d
                currentCalcs[current.Key] = new List<Candidate>() { selected };
                closed.Add(current.Key);

                var _closed = new List<LBSTile>(closed);

                //Paso 5
                List<LBSTile> neigth = map.GetTileNeighbors(current.Key, Dirs);
                SetConnectionNei(current.Key, neigth.ToArray(), closed, whitelist);

                List<LBSTile> neigthCalcs = neigth.RemoveEmpties()
                                         .Where(n => currentCalcs.ContainsKey(n) && whitelist.Contains(n.Position))
                                         .ToList();
                reCalc.AddRange(neigthCalcs);

                //bool noCandidatesFlag = false;
                
                //Paso 6
                while (reCalc.Count > 0)
                {
                    if(((IAssistantThreaded)this).CheckPendingCancel(this, token))
                    {
                        log = "Generation cancelled.";
                        return false;
                    }
                    LBSTile tile = reCalc.First();

                    if (!whitelist.Contains(tile.Position))
                    {
                        reCalc.Remove(tile);
                        continue;
                    }

                    currentCalcs.TryGetValue(tile, out List<Candidate> lastCandidates);
                    List<Candidate> newCandidates = CalcCandidates(tile, group);

                    if (safeMode && newCandidates.Count == 0)
                    {
                        // No possible candidates: must revert step in next iteration
                        stepSuccess = false;
                        reCalc.Clear();
                        break;
                    }

                    if (lastCandidates == null || newCandidates.Count < lastCandidates.Count)
                    {
                        currentCalcs[tile] = newCandidates;

                        List<LBSTile> neighs = map.GetTileNeighbors(tile, Dirs).RemoveEmpties();
                        //foreach (LBSTile nei in neighs)
                        for(int i = 0; i < neighs.Count; i++)
                        {
                            if (_closed.Contains(neighs[i]) || reCalc.Contains(neighs[i]))
                                continue;

                            if (whitelist.Contains(neighs[i].Position))
                                reCalc.Add(neighs[i]);
                        }
                    }

                    reCalc.Remove(tile);
                    _closed.Add(tile);
                }

                toCalc.Remove(current.Key);

                step++;
                // Restore retry limit if further progress
                if(step > maxStep)
                {
                    maxStep = step;
                    if(maxStep > SAVE_STATE_INTERVAL)
                        initialRetryBonus = 0;
                    retryCount = (MAX_MEMORY, MAX_RETRIES + initialRetryBonus);
                }

                if(((IAssistantThreaded)this).CheckPendingCancel(this, token))
                {
                    log = "Generation cancelled.";
                    return false;
                }
                
                onProgress?.Invoke(1f - (float)toCalc.Count / toCalcSize);
                Thread.Sleep(1);
                
                if(safeMode)
                {
                    //Debug.Log($"TRY: {tryCount}\tSTEP {step}\tMAX STEP {maxStep}\tRETRY COUNT {retryCount}");
                    if(step % SAVE_STATE_INTERVAL == 0)
                    {
                        // Save state
                        states.Add(new WFCState(step, connected, toCalc, closed, currentCalcs));
                        if (states.Count > MAX_MEMORY + 1)
                        {
                            states.RemoveAt(0);
                        }
                    }
                }
            }

            success = toCalc.Count == 0;
            if (safeMode && !success)
            {
                OnTaskCancelled();
                log = "Could not generate.";
            }
            
            onProgress?.Invoke(1f);
            Thread.Sleep(1);
            return success;
        }

        private void Restore()
        {
            connected.Rewrite(originalTM);
            originalPositions = Positions;
        }

        public bool ExecuteChance(ref string log, Action<float> onProgress = null, CancellationToken token = default)
        {
            bool success = false;

            int initialRetryBonus = 10;
            (int, int) retryCount = (MAX_MEMORY, MAX_RETRIES + initialRetryBonus);
            int step = 0, maxStep = 0;

            Bundle bundle = targetBundleRef;

            //var group = bundle.GetCharacteristics<LBSDirectionedChance>()[0];
            //var map = OwnerLayer.GetModule<TileMapModule>();
            //var connected = OwnerLayer.GetModule<ConnectedTileMapModule>();

            ////guarda el mapa original para restaurarlo en caso de fallo
            //var og = new List<LBSModule>() { OwnerLayer.GetModule<ConnectedTileMapModule>() };
            //var originalTM = og.Clone()[0] as ConnectedTileMapModule;

            // Get tiles to change
            List<LBSTile> toCalc = GetTileToCalc(map, connected);

            // Build whitelist (positions + direct neighbors)
            // and selection area neighbourhood
            var whitelist = new HashSet<Vector2Int>();
            var areaNeighbours = new List<(LBSTile, int)>();
            bool implemented = true;
            foreach (LBSTile tile in toCalc)
            {
                whitelist.Add(tile.Position);
                List<LBSTile> neighbours = map.GetTileNeighbors(tile, Dirs);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i] == null) continue;

                    bool isAreaNeighbour = !toCalc.Contains(neighbours[i]);
                    bool haveEmpties = connected.GetConnections(neighbours[i]).Contains("");

                    if (!(isAreaNeighbour && haveEmpties)) // In the previous version of WFC, to modify an area neighbour, it would need to be already completed.
                        continue;                     // Now it's the opposite. As we can't determine the probabilities from a partial tile, we'll consider it as empty.

                    whitelist.Add(neighbours[i].Position);

                    if (isAreaNeighbour)
                    {
                        switch (GridType)
                        {
                            case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                                areaNeighbours.Add((neighbours[i], (i + 2) % 4));
                                break;
                            case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                                implemented = false;
                                break;
                        }
                    }
                }
            }
            if (implemented)
            {
                foreach ((LBSTile, int) areaNeighbour in areaNeighbours)
                {
                    connected.SetConnection(areaNeighbour.Item1, areaNeighbour.Item2, "", false);
                    //toCalc.Add(areaNeighbour.Item1); // Possibly not necessary anymore
                }
            }
            //else Debug.LogError("Unhandled case for Vertex-based grid. Could not build area neighbourhood.");

            var closed = new List<LBSTile>();
            var reCalc = new List<LBSTile>();
            var currentCalcs = new Dictionary<LBSTile, List<Candidate>>();

            foreach (LBSTile tile in toCalc)
            {
                List<Candidate> candidates = CalcCandidates(tile, chance);
                currentCalcs.Add(tile, candidates);
            }

            List<WFCState> states = new List<WFCState>();
            if (safeMode)
            {
                states.Add(new WFCState(0, connected, toCalc, closed, currentCalcs));
            }
            bool stepSuccess = true;
            int tryCount = 0;
            int toCalcSize = toCalc.Count;

            List<string> genOrder = new();

            /// MAIN LOOP
            while (toCalc.Count > 0)
            {
                if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                {
                    log = "Generation cancelled.";
                    return false;
                }

                tryCount++;

                List<KeyValuePair<LBSTile, List<Candidate>>> xx = safeMode ?
                    currentCalcs.Where(e => !closed.Contains(e.Key)).ToList() :
                    currentCalcs.Where(e => e.Value.Count > 1).ToList();

                if (xx.Count <= 0)
                    break;

                int minEntropy = xx.Min(e => e.Value.Count);
                var collapseOptions = xx.Where(e => e.Value.Count == minEntropy).ToList();
                KeyValuePair<LBSTile, List<Candidate>> current = collapseOptions.Random();
                //KeyValuePair<LBSTile, List<Candidate>> current = xx.OrderBy(e => e.Value.Count).First();

                // If cannot generate next tile
                if (safeMode && (!stepSuccess || current.Value.Count <= 0))
                {
                    if (Backtrack(states, ref retryCount, connected, originalTM, ref step, maxStep, out int revertedSteps, ref toCalc, ref closed, ref currentCalcs))
                    {
                        stepSuccess = true;
                        //Debug.Log($"TRY: {tryCount}\tSTEP {step}\tMAX STEP {maxStep}\tRETRY COUNT {retryCount}");
                        genOrder.RemoveRange(genOrder.Count - revertedSteps, revertedSteps);
                        continue;
                    }
                    else return false;
                }

                stepSuccess = true;

                Candidate selected = current.Value.RandomRullete(c => c.weigth);
                List<string> connections = selected.bundle.GetConnection(selected.rotation).ToList();
                string center = selected.bundle.Center;
                connected.SetConnections(current.Key, connections, new List<bool>() { false, false, false, false });
                connected.SetCenter(current.Key, center);
                currentCalcs[current.Key] = new List<Candidate>() { selected };
                closed.Add(current.Key);

                var _closed = new List<LBSTile>(closed);

                genOrder.Add(current.Key.Position.ToString());

                List<LBSTile> neigth = map.GetTileNeighbors(current.Key, Dirs);
                SetConnectionNei(current.Key, neigth.ToArray(), closed, whitelist);

                List<LBSTile> neigthCalcs = neigth.RemoveEmpties()
                                         .Where(n => currentCalcs.ContainsKey(n) && whitelist.Contains(n.Position))
                                         .ToList();
                reCalc.AddRange(neigthCalcs);

                //bool noCandidatesFlag = false;

                while (reCalc.Count > 0)
                {
                    if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                    {
                        log = "Generation cancelled.";
                        return false;
                    }
                    LBSTile tile = reCalc.First();

                    if (!whitelist.Contains(tile.Position)
                        || !toCalc.Contains(tile)) // Added to this version. Area neighbour should not recalculate, as it could collapse a full tile that was not included in the first place.
                                                   // Even so they are part of the whitelist just to change a single connection.
                    {
                        reCalc.Remove(tile);
                        continue;
                    }

                    currentCalcs.TryGetValue(tile, out List<Candidate> lastCandidates);
                    List<Candidate> newCandidates = CalcCandidates(tile, chance);

                    if (safeMode && newCandidates.Count == 0)
                    {
                        // No possible candidates: must revert step in next iteration
                        stepSuccess = false;
                        reCalc.Clear();
                        break;
                    }

                    if (lastCandidates == null || newCandidates.Count < lastCandidates.Count)
                    {
                        currentCalcs[tile] = newCandidates;

                        List<LBSTile> neighs = map.GetTileNeighbors(tile, Dirs).RemoveEmpties();
                        //foreach (LBSTile nei in neighs)
                        for (int i = 0; i < neighs.Count; i++)
                        {
                            if (_closed.Contains(neighs[i]) || reCalc.Contains(neighs[i]))
                                continue;

                            if (whitelist.Contains(neighs[i].Position))
                                reCalc.Add(neighs[i]);
                        }
                    }

                    reCalc.Remove(tile);
                    _closed.Add(tile);
                }

                toCalc.Remove(current.Key);

                step++;
                // Restore retry limit if further progress
                if (step > maxStep)
                {
                    maxStep = step;
                    if (maxStep > SAVE_STATE_INTERVAL)
                        initialRetryBonus = 0;
                    retryCount = (MAX_MEMORY, MAX_RETRIES + initialRetryBonus);
                }

                if (((IAssistantThreaded)this).CheckPendingCancel(this, token))
                {
                    log = "Generation cancelled.";
                    return false;
                }

                onProgress?.Invoke(1f - (float)toCalc.Count / toCalcSize);
                Thread.Sleep(1);

                if (safeMode)
                {
                    //Debug.Log($"TRY: {tryCount}\tSTEP {step}\tMAX STEP {maxStep}\tRETRY COUNT {retryCount}");
                    if (step % SAVE_STATE_INTERVAL == 0)
                    {
                        // Save state
                        states.Add(new WFCState(step, connected, toCalc, closed, currentCalcs));
                        if (states.Count > MAX_MEMORY + 1)
                        {
                            states.RemoveAt(0);
                        }
                    }
                }
            }

            string s = "";
            foreach(string gen in genOrder)
            {
                s += gen + " -> ";
            }
            s += "END";
            //Debug.Log(s);

            success = toCalc.Count == 0;
            if (safeMode && !success)
            {
                OnTaskCancelled();
                log = "Could not generate.";
            }

            onProgress?.Invoke(1f);
            Thread.Sleep(1);
            return success;
        }

        private bool Backtrack(
            List<WFCState> states, ref (int, int) retryCount, 
            ConnectedTileMapModule currentTM, ConnectedTileMapModule originalTM, 
            ref int currentStep, int maxStep, out int stepsToRevert,
            ref List<LBSTile> toCalc, ref List<LBSTile> closed, ref Dictionary<LBSTile, List<Candidate>> currentCalcs)
        {
            stepsToRevert = 0;

            // Decrease step retries
            retryCount.Item2--;
            // If step retries run out, it rollbacks to previous state
            if (retryCount.Item2 <= 0)
            {
                retryCount.Item2 = MAX_RETRIES;
                retryCount.Item1--;
                // If it reaches maximum number of reverts allowed, it cancels generation
                if (retryCount.Item1 <= 0)
                {
                    currentTM.Rewrite(originalTM);
                    return false;
                }
            }
            // Determines target step and number of steps to revert
            int offset = (MAX_MEMORY - retryCount.Item1) * SAVE_STATE_INTERVAL + (maxStep % SAVE_STATE_INTERVAL);
            int targetStep = maxStep - offset;
            /*int*/ stepsToRevert = currentStep - targetStep;
            currentStep = targetStep;
            if (currentStep < 0)
            {
                currentTM.Rewrite(originalTM);
                return false;
            }

            int statesToRevert = stepsToRevert / SAVE_STATE_INTERVAL;

            states.Reverse();
            for (int i = 0; i < statesToRevert; i++)
                states.RemoveAt(0);
            WFCState prevState = states[0];
            currentTM.Rewrite(prevState.tileMap);
            toCalc = prevState.toCalc.Clone();
            closed = prevState.closed.Clone();
            //currentCalcs = prevState.currentCalcs.Clone(); //revisar clonacion
            currentCalcs = new Dictionary<LBSTile, List<Candidate>>(prevState.currentCalcs);
            states.Reverse();

            return true;
        }

        public void SetConnectionNei(LBSTile origin, LBSTile[] neis, List<LBSTile> closed, HashSet<Vector2Int> whitelist)
        {
            var connected = OwnerLayer.GetModule<ConnectedTileMapModule>();
            List<string> originConnections = connected.GetConnections(origin);

            for (int i = 0; i < neis.Length; i++)
            {
                LBSTile nei = neis[i];
                if (nei == null || closed.Contains(nei))
                    continue;

                if (!whitelist.Contains(nei.Position))
                    continue;

                List<int> indices = new List<int>();
                switch(GridType)
                {
                    case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                        indices.Add(Dirs[i].GetEdge(Dirs));
                        connected.SetConnection(nei, indices[0], originConnections[i], false);
                        break;
                    case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                        indices.AddRange(Dirs[i].GetVertices(out List<int> originIndices));
                        bool invert = !(originIndices.SequenceEqual(new[] { 0, 3 }) || originIndices.SequenceEqual(new[] { 1, 2 }));
                        for (int j = 0; j < indices.Count; j++)
                        {
                            int dirIndex = indices[j];
                            int k = invert ? indices.Count - 1 - j : j;
                            int originInd = originIndices[k];
                            connected.SetConnection(nei, dirIndex, originConnections[originInd], false);
                        }
                        break;
                }
            }
        }

        private List<LBSTile> GetTileToCalc(TileMapModule map, ConnectedTileMapModule connected)
        {
            var toR = new List<LBSTile>();
            foreach (var position in Positions)
            {
                // Get tile information
                var tile = map.GetTile(position);

                // Check if tile is null
                if (tile == null)
                    continue;

                // Get connections
                //var connection = connected.GetConnections(tile);

                if (overrideValues)
                {
                    //Clear prev connection
                    connected.SetConnections(tile,
                        new List<string>() { "", "", "", "" },
                        new List<bool>() { false, false, false, false });
                }

                toR.Add(tile);
            }
            return toR;
        }

        private List<Candidate> CalcCandidates(LBSTile tile, LBSDirectionedGroup group)
        {
            // Get modules
            var connectedMod = OwnerLayer.GetModule<ConnectedTileMapModule>();
            List<string> connections = connectedMod.GetConnections(tile);

            var candidates = new List<Candidate>();
            for (int i = 0; i < group.Weights.Count; i++)
            {
                // Get characteristics and weigh
                float weigth = group.Weights[i].weight;
                if(weigth == 0f) continue;
                LBSDirection sBundle = group.Weights[i].target.GetCharacteristics<LBSDirection>()[0];

                for (int j = 0; j < 4; j++)
                {
                    // Get connection rotated
                    string[] array = sBundle.GetConnection(j); //(!)

                    // Check if is valid rotated connection
                    if (Compare(connections.ToArray(), array/*, !group.UsesEmpties*/))
                    {
                        var candidate = new Candidate()
                        {
                            bundle = sBundle,
                            weigth = weigth,
                            rotation = j,
                        };

                        candidates.Add(candidate);
                    }
                }
            }
            return candidates;
        }

        private List<Candidate> CalcCandidates(LBSTile tile, LBSDirectionedChance chanceGroup)
        {
            List<Candidate> candidates = new();
            ConnectedTileMapModule connectedMod = OwnerLayer.GetModule<ConnectedTileMapModule>();
            List<LBSTile> neighbors = map.GetTileNeighbors(tile, Dirs);

            List<string> connections = connectedMod.GetConnections(tile);

            //List<string>[] neighsConns = new List<string>[4];

            Dictionary<TileDirectionChance, float> optionsChance = new();

            bool emptyNeighs = true;

            for(int j = 0; j < 4; j++)
            {
                if (neighbors[j] is null)
                    continue;

                List<string> neighConns = connectedMod.GetConnections(neighbors[j]);
                string opp = neighConns[(j + 2) % 4];
                if (opp.Equals("Empty") || opp.Equals(""))
                    continue;

                emptyNeighs = false;

                int neighRot = 0;
                TileDirection neighTD = chanceGroup.tileDirections.Find(td => td.Connections.Rotate(td.rotation).SequenceEqual(neighConns)/*.IsSameRotated(neighConns, out neighRot)*/); // neighTD es el vecino en el mapa, rotado "rotation" veces
                if(neighTD is null)                                                                                                    // neighRot es cuantas veces se rota neightTD para obtener al vecino en el mapa
                    continue;

                List<TileDirectionChance> tileOptions = neighTD.chances[(j + 2/* + neighRot*/) % 4].list;
                if(optionsChance.Count == 0)
                {
                    foreach (TileDirectionChance newTileOption in tileOptions)
                    {
                        optionsChance.Add(newTileOption, newTileOption.chance);
                    }
                }
                else
                {
                    HashSet<TileDirectionChance> safeTiles = new(); // Los tiles que son validos para el vecino actual Y los anteriores
                    foreach (TileDirectionChance newTileOption in tileOptions)
                    {
                        if (optionsChance.ContainsKey(newTileOption))
                        {
                            // Se conserva. Se debe actualizar el peso.
                            float chance1 = optionsChance[newTileOption];
                            float chance2 = newTileOption.chance;
                            float newChance = chance1 * chance2; // Probar si funciona sin necesidad de normalizar
                            optionsChance[newTileOption] = newChance;

                            safeTiles.Add(newTileOption);
                        }
                    }
                    Dictionary<TileDirectionChance, float> newOptions = new();
                    foreach(TileDirectionChance safe in safeTiles)
                    {
                        newOptions.Add(safe, optionsChance[safe]);
                    }
                    optionsChance = newOptions;
                    if (optionsChance.Count == 0) break; // Significa que hay dos o más vecinos incompatibles, y no debe seguir consultando los otros vecinos
                }
            }

            HashSet<TileDirectionChance> options = new(optionsChance.Keys.Select(tdc => new TileDirectionChance()
            {
                target = tdc.target,
                rotation = tdc.rotation,
                chance = optionsChance[tdc] // Aca actualiza realmente la probabilidad combinada
            }));

            if (options.Count == 0 && emptyNeighs && connections.ContainsOnly("Empty", ""))
                candidates = chanceGroup.tileDirections
                    .Select(td => new Candidate()
                    {
                        bundle = td.mainTarget.GetCharacteristics<LBSDirection>()[0],
                        rotation = td.rotation,
                        weigth = 1f / chanceGroup.tileDirections.Count
                    }).ToList();
            else
                candidates = options
                    .Where(tdc => Compare(connections.ToArray(), tdc.Connections.Rotate(tdc.rotation).ToArray()/*, !chance.UsesEmpties*/))
                    .Select(tdc => new Candidate() 
                { 
                    bundle = tdc.target.GetCharacteristics<LBSDirection>()[0], 
                    rotation = tdc.rotation, 
                    weigth = tdc.chance 
                }).ToList();

            return candidates;
        }

        public bool CaptureWeights(out string errMsg)
        {
            errMsg = null;

            List<TileConnectionsPair> pairs = OwnerLayer.GetModule<ConnectedTileMapModule>().Pairs;
            if(pairs.Count == 0)
            {
                errMsg = "Empty map! Could not capture its weights.";
                return false;
            }

            var group = targetBundleRef.GetCharacteristics<LBSDirectionedGroup>()[0];

            var currentBundles = new List<Bundle>();
            group.Weights.ForEach(ws => currentBundles.Add(ws.target));

            var bundleFrequency = new Dictionary<Bundle, int>();
            int maxFreq = 0;
            currentBundles.ForEach(b => bundleFrequency.Add(b, 0));

            for(int i = 0; i < pairs.Count; i++)
            {
                bool matchFound = false;
                List<string> tileConns = pairs[i].Connections;
                for(int j = 0; j < currentBundles.Count; j++)
                {
                    Bundle bundle = currentBundles[j];
                    LBSDirection directionChar = bundle.GetCharacteristics<LBSDirection>()[0];
                    //List<string> bundleConns = directionChar.Connections;
                    for (int k = 0; k < 4; k++)
                    {
                        List<string> rotatedBundleConns = directionChar.GetConnection(k).ToList();//bundleConns.Rotate(k);

                        if(Compare(tileConns.ToArray(), rotatedBundleConns.ToArray(), false))
                        {
                            bundleFrequency[bundle]++;
                            if(bundleFrequency[bundle] > maxFreq)
                                maxFreq = bundleFrequency[bundle];
                            matchFound = true;
                            j = currentBundles.Count;
                            break;
                        }
                    }
                }

                if (!matchFound)
                    Debug.LogWarning($"Tile {pairs[i].Tile.Position} has no matching bundle");
            }

            if(maxFreq == 0)
            {
                errMsg = "Empty map! Could not capture its weights.";
                return false;
            }
            
            for (int i = 0; i < currentBundles.Count; i++) 
            {
                //Debug.Log($"{currentBundles[i]} Frequency: {bundleFrequency[currentBundles[i]]}");
                group.Weights[i].weight = maxFreq != 0 ? (float)bundleFrequency[currentBundles[i]] / (float)maxFreq : 1;
            }

            //Selection.activeObject = targetBundleRef;
            RefreshInspector(targetBundleRef);

            return true;
        }

        //The replacement for CaptureWeights. Captures the tiles from surrounding tiles too, to create chances of apparition.
        public bool CaptureRules(out string errMsg)
        {
            errMsg = "TODO: Add message.";

            Selection.activeObject = targetBundleRef;

            //errMsg = null;

            Dictionary<TileConnectionsPair, List<TileChance>> tileChances = new();

            LBSDirectionedChance group = targetBundleRef.GetCharacteristics<LBSDirectionedChance>()[0];
            // Se llena con todos los bundles hijos antes de filtrar
            group._Update();

            List<TileConnectionsPair> pairs = OwnerLayer.GetModule<ConnectedTileMapModule>().Pairs;

            if (pairs.Count == 0)
            {
                errMsg = "Empty map! Could not capture its weights.";
                return false;
            }

            pairs = pairs.OrderBy(t => -t.Tile.Position.y).ThenBy(t => t.Tile.Position.x).ToList();

            List<Bundle> currentBundles = new();
            group.tileDirections.ForEach(td => currentBundles.Add(td.mainTarget));

            // Check all tiles in map
            foreach (TileConnectionsPair p in pairs)
            {
                bool found = false;
                List<TileChance> adjacent = GetAdjacentFromCurrent(pairs, p);

                // For each tile, compare with registered tiles
                foreach (TileConnectionsPair key in tileChances.Keys)
                {
                    // Check if each connection from key and p are equal

                    // If tile was previously registered
                    if (key.Connections.SequenceEqual(p.Connections))
                    {
                        // Check neighbours
                        foreach (TileChance tca in adjacent)
                        {
                            if (tca.count == -1)
                            {
                                continue;
                            }
                            // Checks if tile had registered this neighbour before
                            TileChance existing = tileChances[key].FirstOrDefault(tc => tc.Equals(tca));
                            // If it had, increase counter
                            if (existing != null)
                            {
                                existing.count++;
                            }
                            else // If not, register a new neighbour chance for this tile, including a new counter (1) and its direction from origin
                            {
                                tileChances[key].Add(new TileChance(tca.tile, tca.count, tca.direction));
                            }
                        }
                        //If it finds anything, it gets marked as found.
                        found = true;
                    }
                }

                //If it doesnt find anything, that means the tile is empty, and needs a tilechance.
                if (!found)
                {
                    tileChances.Add(p, adjacent);
                }
            }

            

            // For each kvp
            foreach (KeyValuePair<TileConnectionsPair, List<TileChance>> rule in tileChances)
            {
                Bundle mainTarget = FindEqualConnection(currentBundles, rule.Key.Connections, rule.Key.Center, out int mainRot);
                if (mainTarget == null)
                    continue;

                // Create a TileDirection. Search amongst exiTileChancestent bundles
                TileDirection td = new()
                {
                    // If no rotated bundle match the tile, mainTarget will be null
                    mainTarget = mainTarget,
                    rotation = mainRot, // Cuantas veces rotas el bundle 'mainTarget' para obtener el tile en el mapa
                    chances = new List<NestedList<TileDirectionChance>>()
                    {
                        new(),
                        new(),
                        new(),
                        new()
                    }
                };

                //For every neighbour tile registered for this tile
                foreach (TileChance pair in rule.Value)
                {
                    Bundle target = FindEqualConnection(currentBundles, pair.tile.Connections, rule.Key.Center, out int rot);
                    if (target == null)
                        continue;

                    TileDirectionChance tileDirectionChance = new()
                    {
                        target = target,
                        rotation = rot, // Cuantas veces rotas el bundle 'target' para obtener el tile en el mapa
                        chance = (float)pair.count / rule.Value.Where(t => t != null && t.direction == pair.direction).Sum(t => t.count)
                    };
                    td.chances[pair.direction].Add(tileDirectionChance);
                }

                group.tileDirections.Add(td);
            }
            
            group.tileDirections.RemoveAll(td => !td.chances.Any());
            
            /*
            foreach (var item in group.tileDirections)
            {
                Debug.Log("- " + item.mainTarget.BundleName);

                for (int i = 0; i < item.chances.Count; i++)
                {
                    Debug.Log("-> " + i);
                    foreach (var item2 in item.chances)
                    {
                        for(int j = 0; j < item2.Count; j++)
                        {
                            Debug.Log(item2[j].target + " " + item2[j].chance.ToString());
                        }
                    }
                }
            }
            */

            RefreshInspector(targetBundleRef);

            return true;
        }

        private Bundle FindEqualConnection(List<Bundle> bundle, List<string> tileConnection, string center, out int rot)
        {
            int count = 0;

            // For each bundle
            for (int i = 0; i < bundle.Count; i++)
            {
                LBSDirection dirChar = bundle[i].GetCharacteristics<LBSDirection>()[0];
                // For each rotation
                for (int j = 0; j < 4; j++)
                {
                    count = 0;
                    // Compare all 4 connections
                    for (int k = 0; k < 4; k++)
                    {
                        if (dirChar.GetConnection(j)[k] == tileConnection[k])
                        {
                            count++;
                        }
                        else break;

                        if (count == 4)
                        {
                            //if(dirChar.Center == "" || center == dirChar.Center)
                            //{
                                rot = j;
                                return bundle[i];
                            //}
                        }
                    }
                }
            }

            rot = -1;
            return null;
        }


        private void ArrangeListByPosition(List<TileConnectionsPair> tiles)
        {
            tiles = tiles.OrderBy(t => t.Tile.Position.x).ThenBy(t => t.Tile.Position.y).ToList();
        }

        private List<TileChance> GetAdjacentFromCurrent(List<TileConnectionsPair> tiles, TileConnectionsPair current)
        {
            List<TileChance> adjacent = new();

            for (int i = 0; i < 4; i++)
            {
                TileConnectionsPair adj = OwnerLayer.GetModule<ConnectedTileMapModule>()
                    .GetPair(current.Tile.Position + Directions.Bidimencional.Edges[i]);

                if (adj != null)
                {
                    //0 => "Right",  
                    //1 => "Up",  
                    //2 => "Left",  
                    //3 => "Down",  

                    TileChance t = new(adj, i);
                    adjacent.Add(t);
                }
            }

            // Check for missing adjacent tiles  


            return adjacent;
        }


        public bool SaveWeights(string presetName, string folder, out string endName, out WFCPreset newPreset, out string errMsg)
        {
            endName = null;
            newPreset = null;
            errMsg = null;

            //if(string.IsNullOrEmpty(folder))
            //{
            //    errMsg = "Cannot save preset. You need to specify a Save Folder.";
            //    return false;
            //}

            var presetCharArr = targetBundleRef.GetCharacteristics<WFCPresetsCharacteristic>();
            WFCPresetsCharacteristic presetChar;
            if(presetCharArr is null || presetCharArr.Count == 0)
            {
                presetChar = new WFCPresetsCharacteristic();
                targetBundleRef.AddCharacteristic(presetChar);
            }
            else presetChar = presetCharArr[0];
            UnityEngine.Assertions.Assert.IsNotNull(presetChar, "Characteristic was null.");
            UnityEngine.Assertions.Assert.IsNotNull(presetChar.Presets, "Presets List was null.");

            endName = presetName;
            if (endName.Length == 0)
            {
                endName = "New WFC Preset";
            }
            if(endName == "New WFC Preset")
            {
                //int count = AssetDatabase.FindAssets(endName).Length;
                int count = presetChar.Presets.Where(p => p.Name.Equals(presetName)).Count();
                if(count > 0)
                {
                    endName += $" ({count})";
                }
            }


            //string path = folder + "/" + endName + ".asset";
            //bool overwrite = AssetDatabase.FindAssets(endName, new[] { folder })
            //    .Count(guid => AssetDatabase.GUIDToAssetPath(guid).Equals(path)) > 0;
            string n = endName;
            bool overwrite = presetChar.Presets.Find(p => p.Name.Equals(n)) is not null;
            if (overwrite)
            {
                bool confirmOverwrite = EditorUtility.DisplayDialog("Overwrite?", $"You are about to overwrite the WFC preset from Bundle {targetBundleRef.BundleName}. Continue?", "Yes", "No");
                if (!confirmOverwrite) return false;
                presetChar.Presets.RemoveAll(p =>
                {
                    if (p.Name.Equals(n))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(p));
                        //ScriptableObject.DestroyImmediate(p, p is WFCPreset);
                        return true;
                    }
                    return false;
                });
            }

            var group = targetBundleRef.GetCharacteristics<LBSDirectionedGroup>()[0];
            newPreset = ScriptableObject.CreateInstance<WFCPreset>();
            newPreset.Name = endName;
            newPreset.SetWeights(group.Weights);

            presetChar.Presets.Add(newPreset);

            RefreshInspector(targetBundleRef);

            string path = LBSSettings.Instance.paths.WFCpresetsFolderPath + $"/{endName}.asset";
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            newPreset.SetAssetGUID(AssetDatabase.AssetPathToGUID(path));
            //
            //EditorUtility.FocusProjectWindow();
            //
            //Selection.activeObject = newPreset;

            return true;
        }

        public void LoadWeights(WFCPreset preset)
        {
            var group = targetBundleRef.GetCharacteristics<LBSDirectionedGroup>()[0];
            for (int i = 0; i < group.Weights.Count; i++)
            {
                bool found = false;
                foreach (var presetWS in preset.GetWeights()) 
                {
                    if (group.Weights[i].target.Equals(presetWS.target))
                    {
                        group.Weights[i].weight = presetWS.weight;
                        found = true;
                        break;
                    }
                }
                // Testear cambiando los bundles hijos
                if(!found)
                    Debug.LogWarning($"Bundle '{group.Weights[i].target}' was not in preset '{preset.Name}'");
            }

            // Refresh bundle on inspector. Works inconsistently.
            RefreshInspector(targetBundleRef);
            //Selection.activeObject = null;
            //EditorApplication.delayCall += () => Selection.activeObject = targetBundleRef;
        }

        public bool SaveRules(string rulesetName, out string endName, out WFCRuleset newRuleset, out string errMsg)
        {
            endName = null;
            newRuleset = null;
            errMsg = null;

            var ruleSetCharArr = targetBundleRef.GetCharacteristics<WFCRulesetsCharacteristic>();
            WFCRulesetsCharacteristic rulesetChar;
            if(ruleSetCharArr is null || ruleSetCharArr.Count == 0)
            {
                rulesetChar = new WFCRulesetsCharacteristic();
                targetBundleRef.AddCharacteristic(rulesetChar);
            }
            else rulesetChar = ruleSetCharArr[0];

            endName = rulesetName;
            if(endName.Length == 0)
            {
                endName = "New WFC Ruleset";
            }
            if(endName == "New WFC Preset")
            {
                int count = rulesetChar.Rulesets.Where(r => r.Name.Equals(rulesetName)).Count();
                if(count > 0)
                {
                    endName += $" ({count})";
                }
            }

            string n = endName;
            bool overwrite = rulesetChar.Rulesets.Find(r => r.Name.Equals(n)) is not null;
            if (overwrite)
            {
                bool confirmOverwrite = EditorUtility.DisplayDialog("Overwrite?", $"You are about to overwrite the WFC ruleset from Bundle {targetBundleRef.BundleName}. Continue?", "Yes", "No");
                if (!confirmOverwrite) return false;
                rulesetChar.Rulesets.RemoveAll(r =>
                {
                    if (r.Name.Equals(n))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(r));
                        return true;
                    }
                    return false;
                });
            }

            var chance = targetBundleRef.GetCharacteristics<LBSDirectionedChance>()[0];
            newRuleset = ScriptableObject.CreateInstance<WFCRuleset>();
            newRuleset.Name = endName;
            newRuleset.SetRules(chance.tileDirections);

            rulesetChar.Rulesets.Add(newRuleset);

            RefreshInspector(targetBundleRef);

            string path = LBSSettings.Instance.paths.WFCpresetsFolderPath + $"/{endName}.asset";
            AssetDatabase.CreateAsset(newRuleset, path);
            AssetDatabase.SaveAssets();
            //newRuleset.SetAssetGUID(AssetDatabase.AssetPathToGUID(path));

            return true;
        }

        public void LoadRules(WFCRuleset ruleset)
        {
            var chance = targetBundleRef.GetCharacteristics<LBSDirectionedChance>()[0];
            chance.maxLimit = 1f;
            for(int i = 0; i < chance.tileDirections.Count; i++)
            {
                bool found = false;
                foreach(TileDirection rulesetTD in ruleset.GetRules())
                {
                    if (chance.tileDirections[i].mainTarget.Equals(rulesetTD.mainTarget))
                    {
                        chance.tileDirections[i].rotation = rulesetTD.rotation;
                        chance.tileDirections[i].chances = new List<NestedList<TileDirectionChance>>(rulesetTD.chances);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    Debug.LogWarning($"Bundle '{chance.tileDirections[i].mainTarget}' was not in ruleset '{ruleset.Name}'");
            }

            RefreshInspector(targetBundleRef);
        }

        private void RefreshInspector(UnityEngine.Object target)
        {
            Action makeNull = () => Selection.activeObject = null;
            Action set = () => Selection.activeObject = target;

            EditorApplication.delayCall += () => 
            { 
                makeNull();
                EditorApplication.delayCall += () =>
                {
                    set();
                    OnRefreshInspector?.Invoke();
                };
            };
        }

        public bool Compare(string[] a, string[] b, bool ignoreEmpties = true)
        {
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    if (!a[i].Equals(b[i]))
                    {
                        //bool empties = string.IsNullOrEmpty(a[i]) || string.IsNullOrEmpty(b[i]); // TODO: Vertex-based exterior layer empty connection is not "" but "Empty" for some reason. 
                        bool empties = string.IsNullOrEmpty(a[i]) || string.IsNullOrEmpty(b[i])
                            || a[i].Equals("Empty") || b[i].Equals("Empty");
                        if (ignoreEmpties && empties)
                            continue;
                        else return false;
                    }
                }
            }
            return true;
        }

        public Bundle GetBundle(string bundleID)
        {
            // Get Target bundle
            var bundles = LBSAssetsStorage.Instance.Get<Bundle>();
            foreach (var bundle in bundles)
            {
                if (bundle.name == bundleID)
                {
                    return bundle;
                }
            }
            return null;
        }

        public override bool Equals(object obj)
        {
            var other = obj as AssistantWFC;

            if (other == null) return false;

            if (!other.Name.Equals(Name)) return false;

            if (!Equals(other.targetBundleRef, targetBundleRef))
                return false;


            if (!other.overrideValues.Equals(overrideValues)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
       
        public Bundle GetBundleRef()
        {
            // if null assign default
            targetBundleRef ??= AssetMacro.LoadAssetByGuid<Bundle>(defaultBundleGuid);
            return targetBundleRef;
        }
        #endregion

        #region DEPRECATED

        //public void OLDExecute()
        //{
        //    // Get Bundle
        //    OnGUI();
        //    var bundle = targetBundleRef;// GetBundle(targetBundle);

        //    // Cheack if can execute
        //    if (bundle == null)
        //    {
        //        Debug.LogWarning("No bundle selected.");
        //        return;
        //    }

        //    // Get bundles posible tiles
        //    var cGroup = bundle.GetCharacteristics<LBSDirectionedGroup>()[0];

        //    // Get modules
        //    var map = OwnerLayer.GetModule<TileMapModule>();
        //    var connected = OwnerLayer.GetModule<ConnectedTileMapModule>();

        //    // Get tiles to change
        //    var toCalc = GetTileToCalc(Positions, map, connected);

        //    // Create auxiliar collections
        //    var closed = new List<LBSTile>();
        //    var reCalc = new List<LBSTile>();

        //    //Init
        //    var currentCalcs = new Dictionary<LBSTile, List<Candidate>>();
        //    foreach (var tile in toCalc)
        //    {
        //        Debug.Log("tile:" + tile.Position);
        //        // Get candidates related to current tile
        //        var candidates = CalcCandidates(tile, cGroup);
        //        currentCalcs.Add(tile, candidates);
        //    }

        //    // Run as long as you have tiles 
        //    while (toCalc.Count > 0)
        //    {
        //        var _closed = new List<LBSTile>(closed);

        //        // end condition
        //        var xx = currentCalcs.Where(e => e.Value.Count > 1).ToList();
        //        if (xx.Count <= 0)
        //            break;

        //        // Get tile with lees possibilities
        //        var current = xx.OrderBy(e => e.Value.Count).First();

        //        // cheack if curren tile have tile possibilities
        //        if (current.Value.Count <= 0)
        //        {
        //            // Remove from the list of tiles to calculate 
        //            Debug.Log(current.Key.Position + " no tiene posibles tile.");
        //            toCalc.Remove(current.Key);
        //            continue;
        //        }

        //        // Collapse possibilities
        //        var selected = current.Value.RandomRullete(c => c.weigth);
        //        var connections = selected.bundle.GetConnection(selected.rotation);
        //        connected.SetConnections(current.Key, connections.ToList(), new List<bool>() { false, false, false, false });
        //        currentCalcs[current.Key] = new List<Candidate>() { selected };

        //        // Ignore This tiles
        //        closed.Add(current.Key);

        //        // Collapse neighbours connection 
        //        var neigth = map.GetTileNeighbors(current.Key, Dirs);
        //        OLDSetConnectionNei(current.Key, neigth.ToArray(), closed);

        //        // Add to reCalc list
        //        var neigthCalcs = neigth.RemoveEmpties().Where(n => currentCalcs.Any(c => c.Key == n)).ToList();
        //        reCalc.AddRange(neigthCalcs);

        //        while (reCalc.Count > 0)
        //        {
        //            var tile = reCalc.First();

        //            // Get candidates related to current tile
        //            List<Candidate> lastCandidates;
        //            currentCalcs.TryGetValue(tile, out lastCandidates);
        //            var newCandidates = CalcCandidates(tile, cGroup);

        //            if (lastCandidates == null || newCandidates.Count < lastCandidates.Count)
        //            {
        //                currentCalcs[tile] = newCandidates;

        //                // Get neighbours
        //                var neigs = map.GetTileNeighbors(tile, Dirs).RemoveEmpties();

        //                // Add to reCalc list
        //                foreach (var nei in neigs)
        //                {
        //                    // Check if tile is closed
        //                    if (_closed.Contains(nei))
        //                        continue;

        //                    if (reCalc.Contains(nei))
        //                        continue;

        //                    reCalc.Add(nei);
        //                }
        //            }
        //            reCalc.Remove(tile);
        //            _closed.Add(tile);
        //        }

        //        // Remove from the list of tiles to calculate 
        //        toCalc.Remove(current.Key);
        //    }
        //}

        //public void OLDSetConnectionNei(LBSTile origin, LBSTile[] neis, List<LBSTile> closed)
        //{
        //    var connected = OwnerLayer.GetModule<ConnectedTileMapModule>();

        //    var dirs = Directions.Bidimencional.Edges;

        //    var oring = connected.GetConnections(origin);

        //    for (int i = 0; i < neis.Length; i++)
        //    {
        //        if (neis[i] == null)
        //            continue;

        //        if (closed.Contains(neis[i]))
        //            continue;

        //        var idir = dirs.FindIndex(d => d.Equals(-dirs[i]));

        //        connected.SetConnection(neis[i], idir, oring[i], false);
        //    }
        //}

        #endregion

        public void OnTaskCancelled()
        {
            Restore();
        }
    }

    /// <summary>
    /// A TileConnectionPair which holds how many times it appears adjacent to another tile, and in which direction.
    /// </summary>
    public class TileChance
    {
        public TileConnectionsPair tile;
        public int count;

        public int direction;
        //0 => "Right",
        //1 => "Up",
        //2 => "Left",
        //3 => "Down",

        public TileChance(TileConnectionsPair tile, int count, int direction)
        {
            this.tile = tile;
            this.count = count;
            this.direction = direction;
        }

        public TileChance(TileConnectionsPair tile, int direction)
        {
            this.tile = tile;
            this.direction = direction;
            count = 1;
        }

        //Formerly used for defining empty tiles. It shouldn't be used now.
        public TileChance(int direction)
        {
            List<string> emptyList = new(new string[] { "", "", "", "" });
            tile = new TileConnectionsPair(new LBSTile(Vector2.zero), emptyList, new List<bool>() { false, false, false, false, });
            count = -1;
            this.direction = direction;
        }

        public override bool Equals(object obj)
        {
            var other = obj as TileChance;

            if (other == null) return false;

            if (!other.tile.Connections.SequenceEqual(tile.Connections)) return false;
            if (other.direction != direction) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class TileConnections
    {
        public List<string> connections;

        public TileConnections(List<string> strings)
        {
            connections = strings;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            if (obj is not TileConnections other)
            {
                return false;
            }

            if (connections.SequenceEqual(other.connections))
            {
                return true;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return connections.GetEnumerator();
        }
    }

    public class Candidate : ICloneable
    {
        public float weigth;
        public LBSDirection bundle;
        public int rotation;

        public Candidate() { }

        public Candidate(float weigth, LBSDirection bundle, int rotation)
        {
            this.weigth = weigth;
            this.bundle = bundle;
            this.rotation = rotation;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Candidate;

            if (other == null) return false;

            return
                weigth == other.weigth &&
                bundle.Equals(other.bundle) &&
                rotation == other.rotation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(weigth, bundle.GetHashCode(), rotation);
        }

        public override string ToString()
        {
            return bundle.Owner.Name;
        }

        public object Clone()
        {
            return new Candidate(weigth, bundle, rotation);
        }
    }

    //A former version of Candidate, kept for possible future use. Though it's a little unnecessary since
    //Candidate works fine for the new Chance system.
    public class ChanceCandidate : ICloneable
    {
        public float weigth;
        public LBSDirection bundle;
        public int rotation;

        public ChanceCandidate() { }

        public ChanceCandidate(float weigth, LBSDirection bundle, int rotation)
        {
            this.weigth = weigth;
            this.bundle = bundle;
            this.rotation = rotation;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ChanceCandidate;

            if (other == null) return false;

            return
                weigth == other.weigth &&
                bundle.Equals(other.bundle) &&
                rotation == other.rotation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(weigth, bundle.GetHashCode(), rotation);
        }

        public override string ToString()
        {
            return bundle.Owner.Name;
        }

        public object Clone()
        {
            return new ChanceCandidate(weigth, bundle, rotation);
        }
    }

    class WFCState
    {
        public int step;
        public ConnectedTileMapModule tileMap;
        public List<LBSTile> toCalc;
        public List<LBSTile> closed = new List<LBSTile>();
        public Dictionary<LBSTile, List<Candidate>> currentCalcs = new Dictionary<LBSTile, List<Candidate>>();

        public WFCState(int step, ConnectedTileMapModule tileMap, List<LBSTile> toCalc, List<LBSTile> closed, Dictionary<LBSTile, List<Candidate>> currentCalcs)
        {
            this.step = step;
            var tm = new List<LBSModule>() { tileMap };
            this.tileMap = tm.Clone()[0] as ConnectedTileMapModule;
            this.toCalc = toCalc.Clone();
            this.closed = closed.Clone();
            //this.currentCalcs = currentCalcs.Clone();
            this.currentCalcs = new Dictionary<LBSTile, List<Candidate>>(currentCalcs);
        }
    }
}