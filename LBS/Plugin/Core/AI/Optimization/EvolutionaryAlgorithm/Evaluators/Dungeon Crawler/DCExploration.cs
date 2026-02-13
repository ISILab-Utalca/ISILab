using ISILab.AI.Optimization;
using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    [System.Serializable]
    public class DCExploration : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!

        public float MaxValue => 1;

        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();

        public LBSLayer CombinedLayer { get; set; } = null;

        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;

        Dictionary<Vector2Int, LBSTile> tilePos = new Dictionary<Vector2Int, LBSTile>();

        public string Tooltip => "DC Exploration Evaluator\n\n" +
            "This evaluator aims to balance the distances between every player and every \"point of interest\" such as chests, weapons and other resources, in order to maximize the explorable space.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.";

        public static EvaluatorConfiguration config;

        [SerializeField, SerializeReference]
        public LBSCharacteristic playerCharacteristic;

        [SerializeField, SerializeReference]
        public LBSCharacteristic colliderCharacteristic;

        [SerializeField, SerializeReference]
        public List<LBSCharacteristic> pointsOfInterest = new List<LBSCharacteristic>();

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

            List<int> POIs = new List<int>();

            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    LBSTag tag = playerCharacteristic?.FirstTag();
                    if (tag != null && genes[i].HasTag(tag))
                    {
                        POIs.Add(i);
                        continue;
                    }
                    foreach(var LBSChar in pointsOfInterest)
                    {
                        LBSTag tagPOI = LBSChar?.FirstTag();
                        if (tagPOI != null && genes[i].HasTag(tagPOI))
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
            //bool[,] toIgnore = new bool[size, size];

            if (layer is not null)
            {
                switch (layer.ID)
                {
                    case "Interior":
                    case "Exterior":
                        var sectorMod = layer.GetModule<SectorizedTileMapModule>();
                        foreach (TileZonePair pair in sectorMod.PairTiles)
                        {
                            if (!tilePos.ContainsKey(pair.Tile.Position))
                                tilePos.Add(pair.Tile.Position, pair.Tile);
                        }
                        string moduleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
                        var connectedMod = layer.GetModule<ConnectedTileMapModule>(moduleID);
                        for (int i = 0; i < size; i++)
                        {
                            FloodFill(POIs[i], POIs, i, ref distances, chrom, sectorMod, connectedMod);
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

         
            List<float> neighborDistances = new List<float>();

            for (int i = 0; i < size; i++)
            {
                float closestDist = float.MaxValue;
                bool found = false;

                for (int j = 0; j < size; j++)
                {
                    if (i == j) continue;
                    int dist = distances[i, j];

                    if (dist > 0 && dist < closestDist)
                    {
                        closestDist = dist;
                        found = true;
                    }
                }

                if (found)
                {
                    neighborDistances.Add(closestDist);
                }
            }

            if (neighborDistances.Count < 2) return 0.0f;

            float averageDist = 0;
            foreach (float d in neighborDistances) averageDist += d;
            averageDist /= neighborDistances.Count;

            float totalError = 0;
            foreach (float d in neighborDistances)
            {
                totalError += Mathf.Abs(d - averageDist);
            }

            float totalSum = averageDist * neighborDistances.Count;

            if (totalSum == 0) return 0f;


            fitness = 1.0f - (totalError / totalSum);

            fitness = Mathf.Clamp01(fitness);

            return fitness;
        }

        public void FloodFill(int startPos, List<int> others, int from, ref int[,] distances, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM)
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

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            CombinedInteriorLayer = (this as IContextualEvaluator).InteriorLayers(selection);
            CombinedExteriorLayer = (this as IContextualEvaluator).ExteriorLayers(selection);
            CombinedLayer = (this as IContextualEvaluator).MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
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

            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            playerCharacteristic = config.GetValue<LBSCharacteristic>("Player");
            colliderCharacteristic = config.GetValue<LBSCharacteristic>("Obstacle");
            pointsOfInterest.Clear();
            pointsOfInterest.AddRange(config.GetValues<LBSCharacteristic>("PointsOfInterest"));
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var thisTarget = config.target as DCExploration; // (!) Las chars de thisTarget son null
            var POIs = new List<Tuple<string, LBSCharacteristic>>();
            for(int i = 0; i < pointsOfInterest.Count; i++)
                POIs.Add(new(pointsOfInterest[i].FirstTag().Label, pointsOfInterest[i]));
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField(playerCharacteristic.FirstTag().Label, playerCharacteristic),
                new MainTagField("Obstacle", colliderCharacteristic.FirstTag().Label, colliderCharacteristic),
                new GroupedTagsField("PointsOfInterest", POIs)
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

            clone.playerCharacteristic = playerCharacteristic;
            clone.colliderCharacteristic = colliderCharacteristic;
            clone.pointsOfInterest = new List<LBSCharacteristic>(pointsOfInterest);
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
