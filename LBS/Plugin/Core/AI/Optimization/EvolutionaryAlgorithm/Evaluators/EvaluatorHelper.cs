

using ISILab.AI.Categorization;
using ISILab.Commons;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public static class EvaluatorHelper
    {
        public static void FloodFill(int startPos, List<int> others, int from, ref int[,] distances, Dictionary<Vector2Int, LBSTile> tilePos, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM)
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

        public static void Manhattan(int startPos, List<int> others, int from, ref int[,] distances, BundleTilemapChromosome chrom)
        {
            for (int i = from; i < others.Count; i++)
            {
                Vector2Int v1 = chrom.ToMatrixPosition(startPos);
                Vector2Int v2 = chrom.ToMatrixPosition(others[i]);

                distances[i, from] = distances[from, i] = Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y);
            }
        }

        public static class JPSPlus
        {
            public static void JPSPreprocess(BundleTilemapChromosome chrom, ConnectedTileMapModule connectedTM)
            {
                List<Vector2Int> edges = Directions.Bidimencional.Edges;
                List<Vector2Int> dirs = Directions.Bidimencional.All;

                List<Tuple<Vector2Int, List<Vector2Int>>> primaryJumpPoints = new();

                IEnumerable<Tuple<TileConnectionsPair, int>> impassables = connectedTM.GetAllPairsWithConnections(SchemaBehaviour.Wall, SchemaBehaviour.Window).Where(t => chrom.ToIndex(t.Item1.Tile.Position) != -1);

                foreach(Tuple<TileConnectionsPair, int> impassable in impassables)
                {
                    int dir = impassable.Item2;
                    int dir1 = (dir + 1) % 4,
                        dir2 = (dir + 3) % 4;
                    Vector2Int possibleJP1 = impassable.Item1.Tile.Position + edges[dir1],
                                possibleJP2 = impassable.Item1.Tile.Position + edges[dir2];
                    if (chrom.ToIndex(possibleJP1) != 1 && connectedTM.GetPair(possibleJP1) is not null)
                    {
                        Vector2Int forcedNeigh = possibleJP1 + edges[dir];
                        if(chrom.ToIndex(forcedNeigh) != 1 && connectedTM.GetPair(forcedNeigh) is not null)
                            primaryJumpPoints.Add(new(possibleJP1, new() { edges[dir1] }));
                    }
                    if (chrom.ToIndex(possibleJP2) != 1 && connectedTM.GetPair(possibleJP2) is not null)
                    {
                        Vector2Int forcedNeigh = possibleJP2 + edges[dir];
                        if (chrom.ToIndex(forcedNeigh) != 1 && connectedTM.GetPair(forcedNeigh) is not null)
                            primaryJumpPoints.Add(new(possibleJP2, new() { edges[dir2] }));
                    }
                }

                for(int i = 0; i < chrom.GetGenes().Length; i++)
                {
                    TileConnectionsPair pair = connectedTM.GetPair(chrom.ToMatrixPosition(i));
                    if(pair is null) continue;
                    List<string> conns = pair.Connections;

                }
            }
        }
    }
}
