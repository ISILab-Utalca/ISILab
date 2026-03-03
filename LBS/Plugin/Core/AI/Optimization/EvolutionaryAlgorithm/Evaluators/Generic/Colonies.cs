using ISILab.AI.Optimization;
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
    public class Colonies : IContextualEvaluator, IConfigurableEvaluator, IDistanceEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!

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

        public Dictionary<(int, int), int> DistancePool { get; set; } = new();

        public List<int> permaIndices = null;

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

        #endregion



        #region EVALUATION

        public float Evaluate(IOptimizable evaluable)
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
                if (groups.Contains(group)) continue;
                if (group is not null && group.BundleData.HasTag(itemCharacteristic.FirstTag()))
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

            int[,] distances = new int[size, size];

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
                        switch (searchType)
                        {
                            case PathfindingAlgorithm.Flood_Fill:
                                for (int i = 0; i < size; i++)
                                {
                                    List<int> knownDist = new List<int>();
                                    List<int> others = new List<int>();
                                    for(int j = i + 1; j < size; j++)
                                    {
                                        if( DistancePool.TryGetValue((itemIndices[i], itemIndices[j]), out distances[i, j]))
                                        {
                                            distances[j, i] = distances[i, j];
                                            knownDist.Add(j);
                                        } 
                                        else if(DistancePool.TryGetValue((itemIndices[j], itemIndices[i]), out distances[j, i]))
                                        {
                                            distances[i, j] = distances[j, i];
                                            knownDist.Add(j);
                                        }
                                    }
                                    others = itemIndices.Except(knownDist).ToList();
                                    EvaluatorHelper.FloodFill(itemIndices[i], others, i, ref distances, tilePos, chrom, sectorMod, connectedMod);
                                }
                                break;
                            case PathfindingAlgorithm.JPS_Plus:
                                for (int i = 0; i < size; i++)
                                {
                                    for (int j = i; j < size; j++)
                                    {
                                        if (i == j)
                                            distances[i, i] = 0;
                                        else if (DistancePool.TryGetValue((itemIndices[i], itemIndices[j]), out distances[i, j]))
                                            distances[j, i] = distances[i, j];
                                        else if (DistancePool.TryGetValue((itemIndices[j], itemIndices[i]), out distances[j, i]))
                                            distances[i, j] = distances[j, i];
                                        else
                                            distances[i, j] = distances[j, i] = EvaluatorHelper.JPSPlus.JPSRun(itemIndices[i], itemIndices[j], chrom.Rect, connectedMod);
                                    }
                                }
                                break;
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

            int news = 0;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    if(DistancePool.TryAdd((itemIndices[i], itemIndices[j]), distances[i, j]))
                        news++;
            //Debug.Log($"Added {news} new distances for a total of {DistancePool.Count}");
            //Debug.Log("Pool Size: " + DistancePool.Count);

            string l = "";
            for(int i = 0; i < size; i++)
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

            while(members.Count < size)
            {
                List<int> distSum = new();
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
                        int d = distances[i, j];
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

                int bestDist = distSum[suitables[0]];
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
            CombinedLayer.GetModule<ConnectedTileMapModule>(moduleID)?.InitializePathfinding(selection);
        }

        public void InitializeDefault()
        {
            itemCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Enemies"));

            maxDist = 6;
            minColonySize = 2;

            searchType = PathfindingAlgorithm.JPS_Plus;
        
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
                new MainTagField("Item", itemCharacteristic.FirstTag().Label, itemCharacteristic, "Item to group."),
                new IntegerConfigurationField("Max Distance", maxDist, 2, 20, "Maximum distance desired between items of the same colony."),
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

            clone.DistancePool = DistancePool;

            clone.permaIndices = permaIndices;

            return clone;
        }
    }
}
