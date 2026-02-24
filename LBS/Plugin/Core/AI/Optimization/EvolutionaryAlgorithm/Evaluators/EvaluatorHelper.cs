

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
            public static Dictionary<Vector2Int, int[]> JPSPreprocessDistances(BundleTilemapChromosome chrom, ConnectedTileMapModule connectedTM)
            {
                List<Vector2Int> edges = Directions.Bidimencional.Edges;
                List<Vector2Int> allDirs = Directions.Bidimencional.All;

                Dictionary<Vector2Int, List<Vector2Int>> primaryJumpPoints = new();
                Dictionary<Vector2Int, int[]> JPDistances = connectedTM.Pairs.Where(tcp => chrom.Rect.Contains(tcp.Tile.Position)).ToDictionary(tcp => tcp.Tile.Position, tcp => new int[8]);

                List<string> impassableConnections = new() { SchemaBehaviour.Wall, SchemaBehaviour.Window };

                int xMin = (int)chrom.Rect.xMin,
                    xMax = (int)chrom.Rect.xMax,
                    yMin = (int)chrom.Rect.yMin,
                    yMax = (int)chrom.Rect.yMax;

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

                for(int y = yMin; y < yMax; y++)
                {
                    int jumpDistance = -1;
                    bool jumpPointSeen = false;

                    for(int x = xMin; x < xMax; x++)
                    {
                        Vector2Int currentNodePos = new Vector2Int(x, y);
                        TileConnectionsPair currentNode = connectedTM.GetPair(currentNodePos);
                        const int dir4 = 2, dir8 = dir4 * 2;

                        if(currentNode is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(currentNode.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[currentNodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if(primaryJumpPoints.TryGetValue(currentNodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }

                    jumpDistance = -1;
                    jumpPointSeen = false;

                    for (int x = xMax - 1; x >= xMin; x--)
                    {
                        Vector2Int currentNodePos = new Vector2Int(x, y);
                        TileConnectionsPair currentNode = connectedTM.GetPair(currentNodePos);
                        const int dir4 = 0, dir8 = dir4 * 2;

                        if (currentNode is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(currentNode.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[currentNodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(currentNodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }
                }

                for (int x = xMin; x < xMax; x++)
                {
                    int jumpDistance = -1;
                    bool jumpPointSeen = false;

                    for (int y = yMin; y < yMax; y++)
                    {
                        Vector2Int currentNodePos = new Vector2Int(x, y);
                        TileConnectionsPair currentNode = connectedTM.GetPair(currentNodePos);
                        const int dir4 = 1, dir8 = dir4 * 2;

                        if (currentNode is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(currentNode.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[currentNodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(currentNodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }

                    jumpDistance = -1;
                    jumpPointSeen = false;

                    for (int y = yMax - 1; y >= yMin; y--)
                    {
                        Vector2Int currentNodePos = new Vector2Int(x, y);
                        TileConnectionsPair currentNode = connectedTM.GetPair(currentNodePos);
                        const int dir4 = 3, dir8 = dir4 * 2;

                        if (currentNode is null)
                        {
                            jumpDistance = -1;
                            jumpPointSeen = false;
                            continue;
                        }

                        jumpDistance++;

                        if (impassableConnections.Contains(currentNode.Connections[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = false;
                        }

                        JPDistances[currentNodePos][dir8] = jumpPointSeen ? jumpDistance : -jumpDistance;

                        if (primaryJumpPoints.TryGetValue(currentNodePos, out List<Vector2Int> JPDirs) && JPDirs.Contains(edges[dir4]))
                        {
                            jumpDistance = 0;
                            jumpPointSeen = true;
                        }
                    }
                }

                /// DIAGONAL JUMP POINTS (WIP)

                int d8, d8low, d8high, d4low, d4high;
                Vector2Int nodePos;
                TileConnectionsPair node;

                for (int y = yMin; y < yMax; y++)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        nodePos = new Vector2Int(x, y);
                        node = connectedTM.GetPair(nodePos);
                        if (node is null) continue;

                        CalcDiagDist(3, x == xMin,      y == xMin, nodePos, node);
                        CalcDiagDist(1, x == xMax - 1,  y == yMin, nodePos, node);
                    }
                }

                for (int y = yMax - 1; y >= yMin; y--)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        nodePos = new Vector2Int(x, y);
                        node = connectedTM.GetPair(nodePos);
                        if (node is null) continue;

                        CalcDiagDist(5, x == xMin,      y == yMax - 1, nodePos, node);
                        CalcDiagDist(7, x == xMax - 1,  y == yMax - 1, nodePos, node);
                    }
                }

                void SetDirection(uint dir)
                {
                    d8 = (int)dir % 8;
                    d8low = (d8 + 7) % 8;
                    d8high = (d8 + 1) % 8;
                    d4low = d8 / 2;
                    d4high = (d4low + 1) % 4;
                }

                void CalcDiagDist(uint dir, bool xBorder, bool yBorder, Vector2Int nodePos, TileConnectionsPair node)
                {
                    SetDirection(dir);

                    Vector2Int nextPos = nodePos + allDirs[d8];
                    TileConnectionsPair next = connectedTM.GetPair(nextPos);

                    if (xBorder || yBorder ||
                        !(JPDistances.ContainsKey(nodePos + allDirs[d8]) && JPDistances.ContainsKey(nodePos + allDirs[d8low]) && JPDistances.ContainsKey(nodePos + allDirs[d8high])) ||
                        impassableConnections.Contains(node.Connections[d4low]) || impassableConnections.Contains(node.Connections[d4high]) ||
                        impassableConnections.Contains(next.Connections[(d4low + 2) % 4]) || impassableConnections.Contains(next.Connections[(d4high + 2) % 4]))
                    {
                        JPDistances[nodePos][d8] = 0;
                    }
                    else if (JPDistances[nextPos][d8low] > 0 || JPDistances[nextPos][d8high] > 0)
                    {
                        JPDistances[nodePos][d8] = 1;
                    }
                    else
                    {
                        int nextDist = JPDistances[nextPos][d8];
                        if (nextDist > 0)
                            JPDistances[nodePos][d8] = nextDist + 1;
                        else JPDistances[nodePos][d8] = nextDist - 1;
                    }
                }

                return JPDistances;
            }
        
            public static void JPSRun(Dictionary<Vector2Int, int[]> JPDistances)
            {
                // TODO
            }
        }
    }
}
