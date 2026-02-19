

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
                int index = chrom.GlobalToIndex(tile.Position);
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

                    Vector2Int currentPos = chrom.ToGlobalPosition(current);

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
                        int index = chrom.GlobalToIndex(newPos);

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
                List<Vector2Int> allDirs = Directions.Bidimencional.All;

                Dictionary<Vector2Int, List<Vector2Int>> primaryJumpPoints = new();
                Dictionary<Vector2Int, int[]> JPDistances = connectedTM.Pairs.Where(tcp => chrom.Rect.Contains(tcp.Tile.Position)).ToDictionary(tcp => tcp.Tile.Position, tcp => new int[8]);

                List<string> impassableConnections = new() { SchemaBehaviour.Wall, SchemaBehaviour.Window };

                /// PRIMARY JUMP POINTS

                IEnumerable<Tuple<TileConnectionsPair, int>> impassables = connectedTM.GetAllPairsWithConnections(impassableConnections.ToArray()).Where(t => chrom.ToIndex(t.Item1.Tile.Position) != -1);

                foreach(Tuple<TileConnectionsPair, int> impassable in impassables)
                {
                    int dir = impassable.Item2;

                    List<string> parentConns = impassable.Item1.Connections;

                    FindPrimaryJumpPointAtDirection((dir + 1) % 4);
                    FindPrimaryJumpPointAtDirection((dir + 3) % 4);

                    void FindPrimaryJumpPointAtDirection(int JPDir)
                    {
                        if (!impassableConnections.Contains(parentConns[JPDir]))
                        {
                            Vector2Int possibleJP = impassable.Item1.Tile.Position + edges[JPDir];
                            TileConnectionsPair JPNode = connectedTM.GetPair(possibleJP);
                            if (chrom.GlobalToIndex(possibleJP) != 1 && JPNode is not null && !impassableConnections.Contains(JPNode.Connections[dir]))
                            {
                                Vector2Int forcedNeigh = possibleJP + edges[dir];
                                if (chrom.GlobalToIndex(forcedNeigh) != 1 && connectedTM.GetPair(forcedNeigh) is not null)
                                    if (primaryJumpPoints.TryGetValue(possibleJP, out List<Vector2Int> JPDirs))
                                        JPDirs.Add(edges[JPDir]);
                                    else primaryJumpPoints.Add(possibleJP, new() { edges[JPDir] });
                            }
                        }
                    }
                }

                /// STRAIGHT JUMP POINTS

                for(int y = (int)chrom.Rect.yMin; y < (int)chrom.Rect.yMax; y++)
                {
                    int jumpDistance = -1;
                    bool jumpPointSeen = false;

                    for(int x = (int)chrom.Rect.xMin; x < (int)chrom.Rect.xMax; x++)
                    {
                        Vector2Int nodePos = new Vector2Int(x, y);
                        TileConnectionsPair node = connectedTM.GetPair(nodePos);
                        const int dir4 = 2, dir8 = dir4 * 2;

                        if(node is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(node.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[nodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if(primaryJumpPoints.TryGetValue(nodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }

                    jumpDistance = -1;
                    jumpPointSeen = false;

                    for (int x = (int)chrom.Rect.xMax - 1; x >= (int)chrom.Rect.xMin; x--)
                    {
                        Vector2Int nodePos = new Vector2Int(x, y);
                        TileConnectionsPair node = connectedTM.GetPair(nodePos);
                        const int dir4 = 0, dir8 = dir4 * 2;

                        if (node is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(node.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[nodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(nodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }
                }

                for (int x = (int)chrom.Rect.xMin; x < (int)chrom.Rect.xMax; x++)
                {
                    int jumpDistance = -1;
                    bool jumpPointSeen = false;

                    for (int y = (int)chrom.Rect.yMin; y < (int)chrom.Rect.yMax; y++)
                    {
                        Vector2Int nodePos = new Vector2Int(x, y);
                        TileConnectionsPair node = connectedTM.GetPair(nodePos);
                        const int dir4 = 1, dir8 = dir4 * 2;

                        if (node is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(node.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[nodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(nodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }

                    jumpDistance = -1;
                    jumpPointSeen = false;

                    for (int y = (int)chrom.Rect.yMax - 1; y >= (int)chrom.Rect.yMin; y--)
                    {
                        Vector2Int nodePos = new Vector2Int(x, y);
                        TileConnectionsPair node = connectedTM.GetPair(nodePos);
                        const int dir4 = 3, dir8 = dir4 * 2;

                        if (node is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(node.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[nodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(nodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }
                }

                /// DIAGONAL JUMP POINTS (WIP)

                for (int y = (int)chrom.Rect.yMin; y < (int)chrom.Rect.yMax; y++)
                {
                    for (int x = (int)chrom.Rect.xMin; x < (int)chrom.Rect.xMax; x++)
                    {
                        Vector2Int nodePos = new Vector2Int(x, y);
                        TileConnectionsPair node = connectedTM.GetPair(nodePos);
                        if (node is null) continue;

                        const int dir8 = 3,
                            dir8low = dir8 - 1, dir8high = (dir8 + 1) % 8, 
                            dir4low = dir8 / 2, dir4high = (dir4low + 1) % 4;


                        Vector2Int nextPos = nodePos + allDirs[dir8];
                        TileConnectionsPair
                            next = connectedTM.GetPair(nextPos)
                            //, nextLow = connectedTM.GetPair(nodePos + allDirs[dir8 - 1])
                            //, nextHigh = connectedTM.GetPair(nodePos + allDirs[(dir8 + 1) % 8])
                            ;

                        if (x == 0 || y == 0 || 
                            !(JPDistances.ContainsKey(nodePos + allDirs[dir8]) && JPDistances.ContainsKey(nodePos + allDirs[dir8low]) && JPDistances.ContainsKey(nodePos + allDirs[dir8high])) ||
                            impassableConnections.Contains(node.Connections[dir4low]) || impassableConnections.Contains(node.Connections[dir4high]) ||
                            impassableConnections.Contains(next.Connections[(dir4low + 2) % 4]) || impassableConnections.Contains(next.Connections[(dir4high + 2) % 4]))
                        {
                            JPDistances[nodePos][dir8] = 0;
                        }
                        else if (JPDistances[nextPos][dir8low] > 0 || JPDistances[nextPos][dir8high] > 0)
                        {

                        }
                        else
                        {

                        }
                    }
                }

                for (int y = (int)chrom.Rect.yMax - 1; y >= (int)chrom.Rect.yMin; y--)
                {
                    for (int x = (int)chrom.Rect.xMin; x < (int)chrom.Rect.xMax; x++)
                    {

                    }
                }
            }
        }
    }
}
