using ISILab.AI.Optimization;
using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    [System.Serializable]
    public class DCExploration : IContextualEvaluator, IConfigurableEvaluator, IDistanceEvaluator, ITestingEvaluator, IRangedEvaluator
    {
        public float MaxValue => 1;

        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();

        public LBSLayer CombinedLayer { get; set; } = null;

        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;
        public LBSLayer CombinedPopulationLayer { get; set; } = null;

        public string Tooltip => "DC Exploration Evaluator\n\n" +
            "This evaluator aims to balance the distances between every player and every \"point of interest\" such as chests, weapons and other resources, in order to maximize the explorable space.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.\n" +
            "- Any type of Population Layer.";

        public Dictionary<(int, int), float> DistancePool { get; set; } = new();
        public EvaluationInfo EvaluationInfo { get; set; } = new(1);

        private List<int> permaIndices = null; // Needed for using extra population layers as context

        public static EvaluatorConfiguration config;

        [SerializeField, SerializeReference]
        public LBSCharacteristic playerCharacteristic;

        [SerializeField, SerializeReference]
        public LBSCharacteristic colliderCharacteristic;

        [SerializeField, SerializeReference]
        public List<LBSCharacteristic> pointsOfInterest = new List<LBSCharacteristic>();

        [SerializeField]
        public PathfindingAlgorithm searchType;

        public float EvaluateWithInfo(IOptimizable evaluable, out EvaluationInfo evalInfo)
        {
            EvaluationInfo = new(1);
            float result = Evaluate(evaluable);
            evalInfo = EvaluationInfo;
            return result;
        }

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

            var genes = chrom.GetGenes().Cast<BundleData>().ToList();

            BundleTileMap bundleTM = CombinedPopulationLayer.GetModule<BundleTileMap>();
            List<TileBundleGroup> groups = new();

            bool checkPermaIndices = permaIndices is null && bundleTM is not null;
            permaIndices ??= new List<int>();

            List<int> POIs = new List<int>();

            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                bool found = false;
                if (genes[i] is not null)
                {
                    LBSTag tag = playerCharacteristic?.FirstTag();
                    if (tag != null && genes[i].HasTag(tag))
                    {
                        POIs.Add(i);
                        found = true;
                        continue;
                    }
                    foreach(var LBSChar in pointsOfInterest)
                    {
                        LBSTag tagPOI = LBSChar?.FirstTag();
                        if (tagPOI != null && genes[i].HasTag(tagPOI))
                        {
                            POIs.Add(i);
                            found = true;
                            break;
                        }
                    }
                }

                if (found || !checkPermaIndices) continue;

                TileBundleGroup group = bundleTM.GetGroup(chrom.ToGlobalPosition(i));
                if (group is null || groups.Contains(group)) continue;
                if (group.BundleData.HasTag(playerCharacteristic.FirstTag()) ||
                    pointsOfInterest.Any(poiChar => poiChar is not null && group.BundleData.HasTag(poiChar.FirstTag())))
                {
                    permaIndices.Add(i);
                    groups.Add(group);
                }
            }

            POIs.AddRange(permaIndices);

            int size = POIs.Count;

            if (size <= 1)
            {
                Debug.LogWarning("Not enough Points of Interest were found. Try adding a player and some more resource elements. Check the DC Exploration evaluator description for more info.");
                return 0.0f;
            }

            float[,] distances = new float[size, size];

            SectorizedTileMapModule sectorMod = null;
            Dictionary<Vector2Int, LBSTile> tilePos = null;
            string moduleID = null;
            ConnectedTileMapModule connectedMod = null;

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
                                    List<int> knownDist = new List<int>();
                                    for (int j = i + 1; j < size; j++)
                                    {
                                        if (DistancePool.TryGetValue((POIs[i], POIs[j]), out distances[i, j]))
                                        {
                                            distances[j, i] = distances[i, j];
                                            knownDist.Add(j);
                                        }
                                        else if (DistancePool.TryGetValue((POIs[j], POIs[i]), out distances[j, i]))
                                        {
                                            distances[i, j] = distances[j, i];
                                            knownDist.Add(j);
                                        }
                                    }
                                    List<int> others = POIs.Except(knownDist).ToList();
                                    EvaluatorHelper.FloodFill(POIs[i], others, i, ref distances, tilePos, chrom, sectorMod, connectedMod, PathfindingHeuristic.Manhattan, ref info);
                                    EvaluationInfo = info;
                                }
                                break;
                            case PathfindingAlgorithm.JPS_Plus:
                            case PathfindingAlgorithm.A_Star:
                                for (int i = 0; i < size; i++)
                                {
                                    distances[i, i] = 0;
                                    for(int j = i + 1; j < size; j++)
                                    {
                                        if (DistancePool.TryGetValue((POIs[i], POIs[j]), out distances[i, j]))
                                            distances[j, i] = distances[i, j];
                                        else if (DistancePool.TryGetValue((POIs[j], POIs[i]), out distances[j, i]))
                                            distances[i, j] = distances[j, i];
                                        else
                                        {
                                            distances[i, j] = distances[j, i] = searchType == PathfindingAlgorithm.A_Star ?
                                                EvaluatorHelper.AStarRun(POIs[i], POIs[j], chrom.Rect, connectedMod, PathfindingHeuristic.Octile, ref info) :
                                                EvaluatorHelper.JPSPlus.JPSRun(POIs[i], POIs[j], chrom.Rect, connectedMod, PathfindingHeuristic.Octile, ref info);
                                            EvaluationInfo = info;
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
                            EvaluatorHelper.Manhattan(POIs[i], POIs, i, ref distances, chrom);
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    EvaluatorHelper.Manhattan(POIs[i], POIs, i, ref distances, chrom);
                }
            }

            int news = 0;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    if (DistancePool.TryAdd((POIs[i], POIs[j]), distances[i, j]))
                        news++;
            //Debug.Log($"Added {news} new distances for a total of {DistancePool.Count}");
            //Debug.Log("Pool Size: " + DistancePool.Count);

            List<float> neighborDistances = new List<float>();
            float distSum = 0;

            for (int i = 0; i < size; i++)
            {
                float closestDist = float.MaxValue;
                bool found = false;

                for (int j = 0; j < size; j++)
                {
                    if (i == j) continue;
                    float dist = distances[i, j];

                    if (dist > 0 && dist < closestDist)
                    {
                        closestDist = dist;
                        found = true;
                    }
                }

                if (found)
                {
                    neighborDistances.Add(closestDist);
                    distSum += closestDist;
                }
            }

            if (neighborDistances.Count < 2) return 0.0f;

            float averageDist = distSum / neighborDistances.Count;

            float totalError = 0;
            foreach (float d in neighborDistances)
            {
                totalError += Mathf.Abs(d - averageDist);
            }

            if (distSum == 0) return 0f;


            fitness = 1.0f - (totalError / distSum);

            fitness = Mathf.Clamp01(fitness);

            return fitness;
        }

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
            playerCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Player"));
            colliderCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Collider"));

            pointsOfInterest.Clear();
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Chest")));
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Axe")));
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Hammer")));
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Sword")));
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Food")));
            pointsOfInterest.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Tree")));

            searchType = PathfindingAlgorithm.JPS_Plus;

            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            playerCharacteristic = config.GetValue<LBSCharacteristic>("Player");
            colliderCharacteristic = config.GetValue<LBSCharacteristic>("Obstacle");
            pointsOfInterest.Clear();
            pointsOfInterest.AddRange(config.GetValues<LBSCharacteristic>("PointsOfInterest"));

            searchType = config.GetValue<PathfindingAlgorithm>("Pathfinding Algorithm");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var POIs = new List<Tuple<string, LBSCharacteristic>>();
            for(int i = 0; i < pointsOfInterest.Count; i++)
                POIs.Add(new(pointsOfInterest[i].FirstTag().Label, pointsOfInterest[i]));
            var list = new List<EvaluatorConfigurationField>
            {
                new EnumConfigurationField("Pathfinding Algorithm", searchType,
                "Method to use for calculating distances between items.\n\n" +
                "<b>> Flood Fill:</b> Preferable for laberynthin levels.\n" +
                "<b>> Jump Point Search Plus (JPS+):</b> Preferable for open areas with few obstacles.\n" +
                "\n<i>(You should not be particularly concerned about this parameter if your level is small-sized or has few items.)</i>"),
                new MainTagField(playerCharacteristic.FirstTag().Label, playerCharacteristic, "Main item to be compared with every POI."),
                new MainTagField("Obstacle", colliderCharacteristic.FirstTag().Label, colliderCharacteristic),
                new GroupedTagsField("PointsOfInterest", POIs, "Items to distribute throughout the level.")
            };

            return list;
        }

        public object Clone()
        {
            var clone = new DCExploration();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;
            clone.CombinedPopulationLayer = CombinedPopulationLayer;

            clone.playerCharacteristic = playerCharacteristic;
            clone.colliderCharacteristic = colliderCharacteristic;
            clone.pointsOfInterest = new List<LBSCharacteristic>(pointsOfInterest);

            clone.searchType = searchType;

            clone.DistancePool = DistancePool;

            clone.permaIndices = permaIndices;

            return clone;
        }


        /*public float Evaluate(IOptimizable evaluable)
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

            var genes = chrom.GetGenes().Cast<BundleData>().ToList();

            List<int> POIs = new List<int>();

            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(playerCharacteristic.FirstTag()))
                    {
                        POIs.Add(i);
                        continue;
                    }
                    foreach (var LBSChar in pointsOfInterest)
                    {
                        if (genes[i].HasTag(LBSChar.FirstTag()))
                        {
                            POIs.Add(i);
                            break;
                        }
                    }
                }
            }

            int size = POIs.Count;

            if (size <= 1)
            {
                Debug.LogWarning("Not enough Points of Interest were found. Try adding a player and some more resource elements. Check the DC Exploration evaluator description for more info.");
                return 0.0f;
            }

            int[,] distances = new int[size, size];
            bool[,] toIgnore = new bool[size, size];

            if (layer is not null)
            {
                var sectorMod = layer.GetModule<SectorizedTileMapModule>();
                foreach (var pair in sectorMod.PairTiles)
                {
                    if (!tilePos.ContainsKey(pair.Tile.Position))
                        tilePos.Add(pair.Tile.Position, pair.Tile);
                }
                switch (layer.ID)
                {
                    case "Interior":
                    case "Exterior":
                        string moduleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
                        for (int i = 0; i < size; i++)
                        {
                            FloodFill(POIs[i], POIs, i, ref distances, chrom, layer.GetModule<SectorizedTileMapModule>(), layer.GetModule<ConnectedTileMapModule>(moduleID));
                        }
                        break;
                    default:
                        for (int i = 0; i < size; i++)
                        {
                            Manhattan(POIs[i], POIs, i, ref distances, chrom);
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    Manhattan(POIs[i], POIs, i, ref distances, chrom);
                }
            }

            //for(int i = 0; i < size; i++)
            //{
            //    for(int j = 0; j < size; j++)
            //    {
            //        // Llenar toIgnore
            //    }
            //}

            float max = 0;
            foreach(float i in distances)
                if(i > max)
                    max = i;

            List<float> score = new List<float>();
            float sum = 0;
            
            for(int i = 0; i < distances.GetLength(0); i++)
            {
                for(int j = i+1; j < distances.GetLength(1); j++)
                {
                    float newScore = (float)distances[i, j] / max;
                    sum += newScore;
                    score.Add(newScore);
                }
            }

            fitness = sum / (float)score.Count;
            //UnityEngine.Assertions.Assert.IsTrue(float.IsNormal(fitness));
            if (!float.IsNormal(fitness))
                Debug.LogError("Fitness was NaN: " + fitness);
            return fitness;
        }*/


        /*public void FloodFill(int startPos, List<int> others, int from, ref int[,] distances, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM)
        {
            //maxDistance = 0;
            if (from >= others.Count)
                return;

            List<int> remainingOthers = new List<int>(others);
            remainingOthers.RemoveRange(0, from);
            remainingOthers.Remove(startPos);

            //var distFromStart = new Dictionary<int, int>();
            var remaining = new List<int>();
            var closed = new List<int>();
            foreach (var tile in sectorizedTM.PairTiles.Select(tzp => tzp.Tile))
            {
                int index = chrom.ToIndex(tile.Position - chrom.Rect.position);
                //distFromStart.Add(index, int.MaxValue);
                if (index < 0) continue;
                remaining.Add(index);
            }

            var remainingStep = new Queue<int>();
            remainingStep.Enqueue(startPos);

            int i;
            for (i = 0; remaining.Count > 0; i++)
            {
                if (remainingStep.Count == 0)
                    break;
                List<int> nextStep = new List<int>();
                while (remainingStep.Count > 0)
                {
                    int current = remainingStep.Dequeue();
                    Vector2Int currentPos = chrom.ToMatrixPosition(current) + Vector2Int.RoundToInt(chrom.Rect.position);
                    Zone currentZone = sectorizedTM.GetZone(currentPos);
                    //distFromStart[current] = i;
                    remaining.Remove(current);
                    closed.Add(current);

                    List<Vector2Int> dirs = Directions.Bidimencional.Edges;
                    foreach (Vector2Int dir in dirs)
                    {
                        //LBSTile currentTile = sectorizedTM.PairTiles.First(tzp => tzp.Tile.Position == currentPos).Tile;
                        if (!tilePos.TryGetValue(currentPos, out LBSTile currentTile))
                        {
                            continue;
                        }
                        string currentConnection = connectedTM.GetConnections(currentTile)[dirs.FindIndex(d => d.Equals(dir))];
                        if (!(currentConnection.Equals("Door") || currentConnection.Equals("Empty")))
                            continue;

                        Vector2Int newPos = currentPos + dir;

                        int index = chrom.ToIndex(newPos - chrom.Rect.position);

                        //if (tileMap.Contains(pos)) // Esto esta mal. Esto es todo el mapa, no solo la seccion seleccionada
                        if (index < 0 || nextStep.Contains(index) || closed.Contains(index))// || chrom.IsInvalid(index))
                            continue;

                        Zone otherZone = sectorizedTM.GetZone(newPos);
                        if (otherZone is null) continue;

                        //LBSTile newTile = sectorizedTM.PairTiles.First(tzp => tzp.Tile.Position == newPos).Tile;
                        if (!tilePos.TryGetValue(newPos, out LBSTile newTile))
                        {
                            continue;
                        }
                        string connection = connectedTM.GetConnections(newTile)[dirs.FindIndex(d => d.Equals(-dir))];
                        if (!(connection.Equals("Door") || connection.Equals("Empty")))
                            continue;

                        for (int j = from; j < others.Count; j++)
                        {
                            if (index == others[j])
                            {
                                distances[from, j] = distances[j, from] = i + 1;
                                remainingOthers.Remove(index);
                                if (remainingOthers.Count == 0)
                                {
                                    //Debug.Log("i = " + i);
                                    return;
                                }
                                break;
                            }
                        }

                        nextStep.Add(index);
                    }
                }

                nextStep.ForEach(i => remainingStep.Enqueue(i));
            }
            //Debug.Log("i = " + i);
            //maxDistance = i;
        }*/
    }
}
