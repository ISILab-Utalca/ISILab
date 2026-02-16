using ISILab.AI.Optimization;
using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data;
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
    public class Colonies : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!

        public float MaxValue => 1;
        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();
        public LBSLayer CombinedLayer { get; set; } = null;
        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;

        public string Tooltip => "DC Custom Evaluator\n\n" +
            "Explain the purpose of your Custom Evaluator and how it works.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.";

        public static EvaluatorConfiguration config;

        const int minColonySize = 2;

        #region CHARACTERISTIC FIELDS

        [SerializeField, SerializeReference]
        public LBSCharacteristic itemCharacteristic;

        [SerializeField]
        private int maxDist;

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
                    }
                }
            }

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
                        for (int i = 0; i < size; i++)
                        {
                            FloodFill(itemIndices[i], itemIndices, i, ref distances, tilePos, chrom, sectorMod, connectedMod);
                        }
                        break;
                    default:
                        for (int i = 0; i < size; i++)
                        {
                            Manhattan(itemIndices[i], itemIndices, i, ref distances, chrom);
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    Manhattan(itemIndices[i], itemIndices, i, ref distances, chrom);
                }
            }

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

        public void FloodFill(int startPos, List<int> others, int from, ref int[,] distances, Dictionary<Vector2Int, LBSTile> tilePos, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM)
        {
            if (from >= others.Count)
                return;

            List<int> remainingOthers = new List<int>(others);
            remainingOthers.RemoveRange(0, from);
            remainingOthers.Remove(startPos);

            var remaining = new HashSet<int>();
            var closed = new HashSet<int>();

            foreach (LBSTile tile in sectorizedTM.PairTiles.Select(tzp => tzp.Tile))
            {
                int index = chrom.ToIndex(tile.Position - chrom.Rect.position);
                if (index < 0) continue;
                remaining.Add(index);
            }

            var remainingStep = new Queue<int>();
            remainingStep.Enqueue(startPos);

            List<Vector2Int> dirs = Directions.Bidimencional.Edges;
            int dirCount = dirs.Count;
            int[] inverseIndices = new int[dirCount];
            for (int k = 0; k < dirCount; k++)
            {
                inverseIndices[k] = dirs.FindIndex(d => d == -dirs[k]);
            }

            int i;
            for (i = 0; remaining.Count > 0; i++)
            {
                if (remainingStep.Count == 0)
                    break;

                HashSet<int> nextStepCheck = new HashSet<int>();
                List<int> nextStep = new List<int>();

                while (remainingStep.Count > 0)
                {
                    int current = remainingStep.Dequeue();

                    Vector2Int currentPos = chrom.ToMatrixPosition(current) + Vector2Int.RoundToInt(chrom.Rect.position);

                    remaining.Remove(current);
                    closed.Add(current);

                    if (!tilePos.TryGetValue(currentPos, out LBSTile currentTile)) continue;
                    List<string> currentConnections = connectedTM.GetConnections(currentTile);

                    for (int k = 0; k < dirCount; k++)
                    {
                        string currentConnection = currentConnections[k];

                        if (!((currentConnection.Length == 4 && currentConnection == "Door") ||
                              (currentConnection.Length == 5 && currentConnection == "Empty")))
                            continue;

                        Vector2Int dir = dirs[k];
                        Vector2Int newPos = currentPos + dir;
                        int index = chrom.ToIndex(newPos - chrom.Rect.position);

                        if (index < 0 || nextStepCheck.Contains(index) || closed.Contains(index))
                            continue;

                        Zone otherZone = sectorizedTM.GetZone(newPos);
                        if (otherZone is null) continue;

                        if (!tilePos.TryGetValue(newPos, out LBSTile newTile)) continue;

                        int invIndex = inverseIndices[k];
                        if (invIndex == -1) continue;

                        string connection = connectedTM.GetConnections(newTile)[invIndex];

                        if (!((connection.Length == 4 && connection == "Door") ||
                              (connection.Length == 5 && connection == "Empty")))
                            continue;

                        for (int j = from; j < others.Count; j++)
                        {
                            if (index == others[j])
                            {
                                distances[from, j] = distances[j, from] = i + 1;
                                remainingOthers.Remove(index);
                                if (remainingOthers.Count == 0) return;
                                break;
                            }
                        }

                        nextStep.Add(index);
                        nextStepCheck.Add(index);
                    }
                }

                foreach (int step in nextStep) remainingStep.Enqueue(step);
            }
        }

        public void Manhattan(int startPos, List<int> others, int from, ref int[,] distances, BundleTilemapChromosome chrom)
        {
            for (int i = from; i < others.Count; i++)
            {
                Vector2Int v1 = chrom.ToMatrixPosition(startPos);
                Vector2Int v2 = chrom.ToMatrixPosition(others[i]);

                distances[i, from] = distances[from, i] = Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y);
            }
        }

        #endregion

        #region INITIALIZATION

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            var contextualEvaluator = this as IContextualEvaluator;
            CombinedInteriorLayer = contextualEvaluator.InteriorLayers(selection);
            CombinedExteriorLayer = contextualEvaluator.ExteriorLayers(selection);
            CombinedLayer = contextualEvaluator.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
        }

        public void InitializeDefault()
        {
            itemCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Enemies"));

            maxDist = 6;
        
            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        #endregion

        #region CONFIGURATION

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            itemCharacteristic = config.GetValue<LBSCharacteristic>("Item");
            maxDist = config.GetValue<int>("Max Distance");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField("Item", itemCharacteristic.FirstTag().Label, itemCharacteristic),
                new IntegerConfigurationField("Max Distance", maxDist, 2, 20)
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

            clone.itemCharacteristic = itemCharacteristic;

            clone.maxDist = maxDist;

            return clone;
        }
    }
}
