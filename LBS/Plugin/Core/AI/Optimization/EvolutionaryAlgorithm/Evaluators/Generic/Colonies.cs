//#define EVAL_TEST

using ISILab.AI.Optimization;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    public class Colonies : IContextualEvaluator, IConfigurableEvaluator, IDistanceEvaluator, ITestingEvaluator, IRangedEvaluator
    {
        public float MaxValue => 1;
        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();
        public LBSLayer CombinedLayer { get; set; } = null;
        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;
        public LBSLayer CombinedPopulationLayer { get; set; } = null;

        public string Tooltip => "Colonies Evaluator\n\n" +
            "This evaluator aims to group items of a certain type into colonies, keeping the members within a maximum distance.\n\n" +
            "By default this evaluator groups Enemies-tagged items at most 6 spaces apart.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.\n" +
            "- Any type of Population Layer.";

        public Dictionary<(int, int), float> DistancePool { get; set; } = new();

        public EvaluationInfo EvaluationInfo { get; set; } = new EvaluationInfo(1);
        private bool useEvaluationInfo = false;

        private List<int> permaIndices = null; // Needed for using extra population layers as context

        public static EvaluatorConfiguration config;

        #region CHARACTERISTIC FIELDS

        [SerializeField, SerializeReference]
        public LBSCharacteristic itemCharacteristic;

        [SerializeField]
        private int maxDist;

        [SerializeField]
        private int minColonySize;

        [SerializeField]
        public PathfindingAlgorithm searchType;

        public PathfindingHeuristic searchHeuristic;

        #endregion

        public struct Colony
        {
            public List<int> members;
            public int center;

            public int Size => members.Count;

            public Colony(params int[] members)
            {
                this.members = members.ToList();
                center = -1;
            }

            public Colony(List<int> members)
            {
                this.members = members;
                center = -1;
            }
            
            public int GetCenter(ChromosomeBase2D chromosome)
            {
                if (members.Count == 0) return -1;
                if (members.Count == 1) return members[0];

                List<Vector2> positions = members.Select(m => chromosome.ToMatrixPosition(m).ToFloat()).ToList();
                Vector2 avg = positions.Average();
                Vector2 trueCenter = avg;
                Vector2Int centerPosInt = trueCenter.ToInt(true);
                if (InvalidPos(centerPosInt))
                {
                    Queue<Vector2Int> dirsPriority = new();
                    Vector2 diff = trueCenter - centerPosInt;
                    Vector2Int xOffset = Vector2Int.right.Multiply(Mathf.Sign(diff.x), true);
                    Vector2Int yOffset = Vector2Int.up.Multiply(Mathf.Sign(diff.y), true);
                    Vector2Int xNeigh = centerPosInt + xOffset;
                    Vector2Int yNeigh = centerPosInt + yOffset;

                    if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                    {
                        if(positions.Contains(xNeigh))
                            return chromosome.ToIndex(xNeigh);
                        dirsPriority.Enqueue(xNeigh);
                        dirsPriority.Enqueue(yNeigh);
                    }
                    else
                    {
                        if (positions.Contains(yNeigh))
                            return chromosome.ToIndex(yNeigh);
                        dirsPriority.Enqueue(yNeigh);
                        dirsPriority.Enqueue(xNeigh);
                    }
                    dirsPriority.Enqueue(centerPosInt + xOffset + yOffset);

                    bool invalid = true;
                    while(invalid && dirsPriority.Count > 0)
                    {
                        centerPosInt = dirsPriority.Dequeue();
                        invalid = InvalidPos(centerPosInt);
                    }

                    if (invalid)
                    {
                        throw new System.NotImplementedException();
                        //PriorityQueue<Vector2, float> memsPriority = new();
                        //foreach (Vector2 memPos in positions)
                        //{
                        //    memsPriority.Push(memPos, Vector2.SqrMagnitude(memPos - trueCenter));
                        //}
                        //centerPosInt = memsPriority.Pop().ToInt();
                    }
                }

                return chromosome.ToIndex(centerPosInt);

                bool InvalidPos(Vector2Int pos) => chromosome.IsInvalid(chromosome.ToIndex(pos));
            }

            public Colony SetCenter(ChromosomeBase2D chromosome)
            {
                center = GetCenter(chromosome);
                return this;
            }

            public void AddCenter(int center)
            {
                members.Add(center);
                this.center = center;
            }

            public bool Contains(int member) => members.Contains(member);

            public int this[int i] => members[i];
        }

        #region EVALUATION
        public float Evaluate(IOptimizable evaluable) => NEWEvaluate(evaluable);

        public float EvaluateWithInfo(IOptimizable evaluable, out EvaluationInfo evalInfo)
        {
            useEvaluationInfo = true;
            EvaluationInfo = new EvaluationInfo(1);
            float result = Evaluate(evaluable);
            evalInfo = EvaluationInfo;
            return result;
        }

        public float NEWEvaluate(IOptimizable evaluable)
        {
            var chrom = evaluable as BundleTilemapChromosome;

            if (chrom is null)
            {
                throw new System.Exception("Wrong Chromosome Type");
            }
            if (chrom.IsEmpty())
            {
                return 0.0f;
            }

            LBSLayer layer = CombinedLayer;

            float fitness = 0;

            List<BundleData> genes = chrom.GetGenes().Cast<BundleData>().ToList();

            BundleTileMap bundleTM = CombinedPopulationLayer.GetModule<BundleTileMap>();
            List<TileBundleGroup> groups = new();

            bool checkPermaIndices = permaIndices is null && bundleTM is not null;
            permaIndices ??= new List<int>();

            List<int> itemIndices = new();
            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(itemCharacteristic.FirstTag()))
                    {
                        itemIndices.Add(i);
                        continue;
                    }
                }

                if (!checkPermaIndices) continue;

                TileBundleGroup group = bundleTM.GetGroup(chrom.ToGlobalPosition(i));
                if (group is null || groups.Contains(group)) continue;
                if (group.BundleData.HasTag(itemCharacteristic.FirstTag()))
                {
                    permaIndices.Add(i);
                    groups.Add(group);
                }
            }

            itemIndices.AddRange(permaIndices);

            int size = itemIndices.Count;

            if (size <= 1)
            {
                Debug.LogWarning("No enough items found to make colonies.");
                return 0.0f;
            }

            List<Colony> colonies = new();
            float[,] distances = new float[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    distances[i, j] = -1;

            SectorizedTileMapModule sectorMod = null;
            Dictionary<Vector2Int, LBSTile> tilePos = null;
            string moduleID = null;
            ConnectedTileMapModule connectedMod = null;

            HashSet<int> skip = new();

            if (layer is not null)
            {
                switch (layer.ID)
                {
                    case "Interior":
                    case "Exterior":
                        sectorMod = layer.GetModule<SectorizedTileMapModule>();
                        tilePos = new Dictionary<Vector2Int, LBSTile>();
                        foreach (TileZonePair pair in sectorMod.PairTiles)
                        {
                            if (!tilePos.ContainsKey(pair.Tile.Position))
                                tilePos.Add(pair.Tile.Position, pair.Tile);
                        }
                        moduleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
                        connectedMod = layer.GetModule<ConnectedTileMapModule>(moduleID);
                        EvaluationInfo info = EvaluationInfo;
                        switch (searchType)
                        {
                            case PathfindingAlgorithm.Flood_Fill:
                                for (int i = 0; i < size; i++)
                                {
                                    Colony colony;
                                    int exists = colonies.FindIndex(c => c.Contains(itemIndices[i]));
                                    List<int> filter = new();
                                    if (exists == -1)
                                    {
                                        colony = new(new List<int>());
                                        colony.AddCenter(itemIndices[i]);
                                        colonies.Add(colony);
                                        filter = skip.ToList();
                                    }
                                    else
                                    {
                                        colony = colonies[exists];
                                        filter.Add(colony.center);
                                    }
                                    distances[i, i] = 0;
                                    EvaluatorHelper.PartialFloodFill(maxDist, itemIndices[i], itemIndices, filter, i, out List<int> found, ref distances, tilePos, chrom, sectorMod, connectedMod, searchHeuristic, ref info);
                                    if(useEvaluationInfo) EvaluationInfo = info;
                                    IEnumerable<int> members = found.Except(skip);
                                    colony.members.AddRange(members);
                                    //Debug.Log($"{i} => {members.Select(m => itemIndices.IndexOf(m)).ToList().ElementsToString()}");
                                    skip.UnionWith(found);
                                }
                                break;
                            case PathfindingAlgorithm.JPS_Plus:
                            case PathfindingAlgorithm.A_Star:
                                for (int i = 0; i < size; i++)
                                {
                                    Colony colony;
                                    int exists = colonies.FindIndex(c => c.Contains(itemIndices[i]));
                                    List<int> filter = new();
                                    if (exists == -1)
                                    {
                                        colony = new(new List<int>());
                                        colony.AddCenter(itemIndices[i]);
                                        colonies.Add(colony);
                                        filter = skip.ToList();
                                    }
                                    else
                                    {
                                        colony = colonies[exists];
                                        filter.Add(colony.center);
                                    }
                                    distances[i, i] = 0;
                                    //if (skip.Contains(i)) continue;
                                    List<int> members = new();// { itemIndices[i] };
                                    for (int j = i + 1; j < size; j++)
                                    {
                                        if (filter.Contains(itemIndices[j])) continue;
                                        distances[i, j] = distances[j, i] = searchType == PathfindingAlgorithm.A_Star ?
                                            EvaluatorHelper.PartialAStarRun(maxDist, itemIndices[i], itemIndices[j], chrom.Rect, connectedMod, searchHeuristic, ref info) :
                                            EvaluatorHelper.JPSPlus.PartialJPSRun(maxDist, itemIndices[i], itemIndices[j], chrom.Rect, connectedMod, searchHeuristic, ref info);
                                        if (useEvaluationInfo) EvaluationInfo = info;
                                        if (distances[i, j] < 0) continue; // Not found at specified distance
                                        if(!skip.Contains(itemIndices[j])) members.Add(itemIndices[j]); // Add to incoming colony
                                        skip.Add(itemIndices[j]);
                                    }
                                    colony.members.AddRange(members);
                                    //Debug.Log($"{i} => {members.Select(m => itemIndices.IndexOf(m)).ToList().ElementsToString()}");
                                    //colonies.Add(new(members));
                                    //colonies[^1] = colonies[^1].SetCenter(chrom);
                                }
                                break;
                            default:
                                Debug.LogWarning("Algorithm not implemented. Executing JPS+ instead...");
                                goto case PathfindingAlgorithm.JPS_Plus;
                        }
                        break;
                    default:
                        NoContextSearch();
                        break;
                }
            }
            else
            {
                NoContextSearch();
            }

            void NoContextSearch()
            {
                for (int i = 0; i < size; i++)
                {
                    distances[i, i] = 0;
                    if (skip.Contains(i)) continue;
                    EvaluatorHelper.PartialManhattan(maxDist, itemIndices[i], itemIndices.Except(skip).ToList(), i, out List<int> found, ref distances, chrom);
                    colonies.Add(new(found));
                    //colonies[^1] = colonies[^1].SetCenter(chrom);
                    skip.UnionWith(found.Select(f => itemIndices.IndexOf(f)));
                }
            }

            string l = "";
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    l += $"[{distances[i, j]}] ";
                }
                l += "\n";
            }
            //Debug.Log(l);

            foreach(Colony colony in colonies)
            {
                if(colony.Size < minColonySize) continue;

                int cSize = colony.Size;
                int maxScore = (cSize * cSize - cSize) / 2;
                int score = maxScore;
                for (int i = 0; i < cSize - 1; i++)
                    for (int j = i + 1; j < cSize; j++)
                    {
                        float dist = distances[itemIndices.IndexOf(colony[i]), itemIndices.IndexOf(colony[j])];
                        if (dist > maxDist || dist < 0)
                            score--;
                    }

                float colonyFitness = (float)score / (float)maxScore;
                fitness += colonyFitness;
            }

            fitness /= colonies.Count;

            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }

        #endregion

        #region INITIALIZATION

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            IContextualEvaluator ctx = this;
            CombinedInteriorLayer = ctx.InteriorLayers(selection);
            CombinedExteriorLayer = ctx.ExteriorLayers(selection);
            CombinedPopulationLayer = ctx.PopulationLayers();
            permaIndices = null;
            CombinedLayer = ctx.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);

            if (CombinedLayer is null) return;
            string moduleID = CombinedLayer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
            CombinedLayer.GetModule<ConnectedTileMapModule>(moduleID)?.InitializePathfinding(selection, searchType);
        }

        public void InitializeDefault()
        {
            itemCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Enemies"));

            maxDist = 6;
            minColonySize = 2;

            searchType = PathfindingAlgorithm.JPS_Plus;
            searchHeuristic = PathfindingHeuristic.Chebyshev;
        
            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        #endregion

        #region CONFIGURATION

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            itemCharacteristic = config.GetValue<LBSCharacteristic>("Item");
            maxDist = config.GetValue<int>("Max Distance");
            minColonySize = config.GetValue<int>("Min Colony Size");

            searchType = config.GetValue<PathfindingAlgorithm>("Pathfinding Algorithm");
            searchHeuristic = config.GetValue<PathfindingHeuristic>("Heuristic");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new EnumConfigurationField("Pathfinding Algorithm", searchType,
                "Method to use for calculating distances between items.\n\n" +
                "<b>> Flood Fill:</b> Preferable for laberynthin levels.\n" +
                "<b>> Jump Point Search Plus (JPS+):</b> Preferable for open areas with few obstacles.\n" +
                "\n<i>(You should not be particularly concerned about this parameter if your level is small-sized or has few items.)</i>"),
                new EnumConfigurationField("Heuristic", searchHeuristic),
                new MainTagField("Item", itemCharacteristic.FirstTag().Label, itemCharacteristic, "Item to group."),
                new IntegerConfigurationField("Max Distance", maxDist, 2, 15, "Maximum distance desired between items of the same colony."),
                new IntegerConfigurationField("Min Colony Size", minColonySize, 2, 10, "Minimum number of members a colony should have to be considered as such.")
            };

            return list;
        }

        #endregion

        public object Clone()
        {
            var clone = new Colonies();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;
            clone.CombinedPopulationLayer = CombinedPopulationLayer;

            clone.itemCharacteristic = itemCharacteristic;

            clone.maxDist = maxDist;
            clone.minColonySize = minColonySize;

            clone.searchType = searchType;
            clone.searchHeuristic = searchHeuristic;

            clone.DistancePool = DistancePool;

            clone.permaIndices = permaIndices;

            return clone;
        }

        [System.Obsolete]
        public float OLDEvaluate(IOptimizable evaluable)
        {
            var chrom = evaluable as BundleTilemapChromosome;

            if (chrom is null)
            {
                throw new System.Exception("Wrong Chromosome Type");
            }
            if (chrom.IsEmpty())
            {
                return 0.0f;
            }

            LBSLayer layer = CombinedLayer;

            float fitness = 0;

            List<BundleData> genes = chrom.GetGenes().Cast<BundleData>().ToList();

#if !EVAL_TEST
            BundleTileMap bundleTM = CombinedPopulationLayer.GetModule<BundleTileMap>();
            List<TileBundleGroup> groups = new();

            bool checkPermaIndices = permaIndices is null && bundleTM is not null;
            permaIndices ??= new List<int>();
#endif

            List<int> itemIndices = new();
            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(itemCharacteristic.FirstTag()))
                    {
                        itemIndices.Add(i);
                        continue;
                    }
                }
#if !EVAL_TEST
                if (!checkPermaIndices) continue;

                TileBundleGroup group = bundleTM.GetGroup(chrom.ToGlobalPosition(i));
                if (groups.Contains(group)) continue;
                if (group is not null && group.BundleData.HasTag(itemCharacteristic.FirstTag()))
                {
                    permaIndices.Add(i);
                    groups.Add(group);
                }
#endif
            }
#if !EVAL_TEST
            itemIndices.AddRange(permaIndices);
#endif
            int size = itemIndices.Count;

            if (size <= 1)
            {
                Debug.LogWarning("No enough items found to make colonies.");
                return 0.0f;
            }

            float[,] distances = new float[size, size];

            if (layer is not null)
            {
                switch (layer.ID)
                {
                    case "Interior":
                    case "Exterior":
                        var sectorMod = layer.GetModule<SectorizedTileMapModule>();
                        Dictionary<Vector2Int, LBSTile> tilePos = new();
                        foreach (TileZonePair pair in sectorMod.PairTiles)
                        {
                            if (!tilePos.ContainsKey(pair.Tile.Position))
                                tilePos.Add(pair.Tile.Position, pair.Tile);
                        }
                        string moduleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
                        var connectedMod = layer.GetModule<ConnectedTileMapModule>(moduleID);
                        EvaluationInfo info = EvaluationInfo;
                        switch (searchType)
                        {
                            case PathfindingAlgorithm.Flood_Fill:
                                for (int i = 0; i < size; i++)
                                {
#if !EVAL_TEST
                                    List<int> knownDist = new List<int>();
                                    List<int> others = new List<int>();
                                    for (int j = i + 1; j < size; j++)
                                    {
                                        if (DistancePool.TryGetValue((itemIndices[i], itemIndices[j]), out distances[i, j]))
                                        {
                                            distances[j, i] = distances[i, j];
                                            knownDist.Add(j);
                                        }
                                        else if (DistancePool.TryGetValue((itemIndices[j], itemIndices[i]), out distances[j, i]))
                                        {
                                            distances[i, j] = distances[j, i];
                                            knownDist.Add(j);
                                        }
                                    }
                                    others = itemIndices.Except(knownDist).ToList();
                                    EvaluatorHelper.FloodFill(itemIndices[i], others, i, ref distances, tilePos, chrom, sectorMod, connectedMod, PathfindingHeuristic.Chebyshev, ref info);
                                    if (useEvaluationInfo) EvaluationInfo = info;
#else
                                    EvaluatorHelper.FloodFill(itemIndices[i], itemIndices, i, ref distances, tilePos, chrom, sectorMod, connectedMod);
#endif
                                }
                                break;
                            case PathfindingAlgorithm.JPS_Plus:
                            case PathfindingAlgorithm.A_Star:
                                for (int i = 0; i < size; i++)
                                {
                                    for (int j = i; j < size; j++)
                                    {
#if !EVAL_TEST
                                        if (i == j)
                                            distances[i, i] = 0;
                                        else if (DistancePool.TryGetValue((itemIndices[i], itemIndices[j]), out distances[i, j]))
                                            distances[j, i] = distances[i, j];
                                        else if (DistancePool.TryGetValue((itemIndices[j], itemIndices[i]), out distances[j, i]))
                                            distances[i, j] = distances[j, i];
                                        else
#endif
                                        {
                                            distances[i, j] = distances[j, i] = searchType == PathfindingAlgorithm.A_Star ?
                                                EvaluatorHelper.AStarRun(itemIndices[i], itemIndices[j], chrom.Rect, connectedMod, ref info) :
                                                EvaluatorHelper.JPSPlus.JPSRun(itemIndices[i], itemIndices[j], chrom.Rect, connectedMod, ref info);
                                            if (useEvaluationInfo) EvaluationInfo = info;
                                        }
                                    }
                                }
                                break;
                            default:
                                Debug.LogWarning("Algorithm not implemented. Executing JPS+ instead...");
                                goto case PathfindingAlgorithm.JPS_Plus;
                        }
                        break;
                    default:
                        for (int i = 0; i < size; i++)
                        {
                            EvaluatorHelper.Manhattan(itemIndices[i], itemIndices, i, ref distances, chrom);
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    EvaluatorHelper.Manhattan(itemIndices[i], itemIndices, i, ref distances, chrom);
                }
            }
#if EVAL_TEST
            return 0.0f;
#endif

            int news = 0;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    if (DistancePool.TryAdd((itemIndices[i], itemIndices[j]), distances[i, j]))
                        news++;
            //Debug.Log($"Added {news} new distances for a total of {DistancePool.Count}");
            //Debug.Log("Pool Size: " + DistancePool.Count);

            string l = "";
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    l += $"[{distances[i, j]}] ";
                }
                l += "\n";
            }
            //Debug.Log(l);

            // COLONY CONSTRUCTION

            List<List<int>> colonies = new();
            List<int> members = new();

            while (members.Count < size)
            {
                List<float> distSum = new();
                List<List<int>> nearest = new();
                List<int> coreSuitability = new();
                List<int> suitables = new();
                int maxSuitableScore = 0;
                for (int i = 0; i < size; i++)
                {
                    distSum.Add(0);
                    nearest.Add(new());
                    coreSuitability.Add(0);
                    if (members.Contains(i)) continue;
                    for (int j = 0; j < size; j++)
                    {
                        if (i == j || members.Contains(j)) continue;
                        float d = distances[i, j];
                        distSum[i] += d;
                        if (d <= maxDist)
                        {
                            nearest[i].Add(j);
                            coreSuitability[i]++;
                        }
                    }

                    if (coreSuitability[i] > maxSuitableScore)
                    {
                        suitables.Clear();
                        suitables.Add(i);
                        maxSuitableScore = coreSuitability[i];
                    }
                    else if (coreSuitability[i] == maxSuitableScore)
                        suitables.Add(i);
                }

                float bestDist = distSum[suitables[0]];
                int core = suitables[0];
                for (int i = 1; i < suitables.Count; i++)
                {
                    if (distSum[suitables[i]] < bestDist)
                    {
                        bestDist = distSum[suitables[i]];
                        core = suitables[i];
                    }
                }

                colonies.Add(new() { core });
                colonies[^1].AddRange(nearest[core]);
                members.AddRange(colonies[^1]);

                // Colony scoring

                if (colonies[^1].Count < minColonySize) continue;

                int cSize = colonies[^1].Count;
                int maxScore = (cSize * cSize - cSize) / 2;
                int score = maxScore;
                for (int i = 0; i < cSize - 1; i++)
                    for (int j = i + 1; j < cSize; j++)
                        if (distances[colonies[^1][i], colonies[^1][j]] > maxDist)
                            score--;

                float colonyFitness = (float)score / (float)maxScore;
                fitness += colonyFitness;
            }

            fitness /= colonies.Count;

            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }
    }
}
