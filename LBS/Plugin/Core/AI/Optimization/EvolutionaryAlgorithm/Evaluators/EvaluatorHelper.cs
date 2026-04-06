

using ISILab.AI.Categorization;
using ISILab.Commons;
using ISILab.Commons.Extensions;
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
    public class PriorityQueue<KeyType, PriorityType> where PriorityType : IComparable
    {
        struct Element<SubClassKeyType, SubClassPriorityType> where SubClassPriorityType : IComparable
        {
            public SubClassKeyType key;
            public SubClassPriorityType priority;

            public Element(SubClassKeyType key, SubClassPriorityType priority)
            {
                this.key = key;
                this.priority = priority;
            }
        }

        List<Element<KeyType, PriorityType>> queue = new List<Element<KeyType, PriorityType>>();

        public void Push(KeyType arg_key, PriorityType arg_priority)
        {
            Element<KeyType, PriorityType> new_elem = new Element<KeyType, PriorityType>(arg_key, arg_priority);

            int index = 0;
            foreach (var element in queue)
            {
                // if my new element's priority is less than than the element in this location
                if (new_elem.priority.CompareTo(element.priority) < 0)
                {
                    break;
                }

                ++index;
            }

            // Insert at the found index
            queue.Insert(index, new_elem);
        }

        public KeyType Pop()
        {
            if (IsEmpty())
            {
                throw new UnityException("Attempted to pop off an empty queue");
            }

            Element<KeyType, PriorityType> top = queue[0];

            queue.RemoveAt(0);

            return top.key;
        }

        public bool IsEmpty()
        {
            return queue.Count == 0;
        }

        public bool Contains(KeyType key)
        {
            return queue.Any(e => e.key.Equals(key));
        }
    }

    public static class EvaluatorHelper
    {
        public static void FloodFill(int startPos, List<int> others, int from, ref int[,] distances, Dictionary<Vector2Int, LBSTile> tilePos, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
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

            //List<TileZonePair> pairTiles = sectorizedTM.PairTiles;
            //for (int i = 0; i < chrom.Rect.height; i++)
            //{
            //    for (int j = 0; j < chrom.Rect.width; j++)
            //    {
            //        int index = (int)chrom.Rect.height * i + j;
            //        if (pairTiles.Any(tzp => tzp.Tile.Position.Equals(chrom.ToGlobalPosition(index))))
            //            remaining.Add(index);
            //    }
            //}

            //Debug.Log(remaining.Count);

            var remainingStep = new Queue<int>();
            remainingStep.Enqueue(startPos);

            List<Vector2Int> dirs = Directions.Bidimencional.Edges;
            int dirCount = dirs.Count;
            int[] inverseIndices = new int[dirCount];
            for (int k = 0; k < dirCount; k++)
            {
                inverseIndices[k] = dirs.FindIndex(d => d == -dirs[k]);
            }

            //int i;
            for (int i = 0; remaining.Count > 0; i++)
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

                        if (index < 0 || nextStepCheck.Contains(index) || closed.Contains(index) || remainingStep.Contains(index))
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

                        evalInfo.visitedNodes++;

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

        public static void PartialFloodFill(int limit, int startPos, List<int> others, List<int> filtered, int from, out List<int> found, ref int[,] distances, Dictionary<Vector2Int, LBSTile> tilePos, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
        {
            found = new List<int>() { };

            //if (from >= others.Count)
            //    return;

            List<int> remainingOthers = new List<int>(others.Except(filtered));
            //remainingOthers.RemoveRange(0, from);
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

            Dictionary<int, int> allowedSteps = new() { [startPos] = limit };

            List<Vector2Int> dirs = Directions.Bidimencional.Edges;
            int dirCount = dirs.Count;
            int[] inverseIndices = new int[dirCount];
            for (int k = 0; k < dirCount; k++)
            {
                inverseIndices[k] = dirs.FindIndex(d => d == -dirs[k]);
            }

            int i; // Distancia recorrida
            for (i = 0; remaining.Count > 0 && remainingStep.Count > 0; i++) // Casillas que falta por revisar
            {
                if (remainingStep.Count == 0)
                    break;

                HashSet<int> nextStepCheck = new HashSet<int>();
                List<int> nextStep = new List<int>();

                while (remainingStep.Count > 0) // Casillas que deben revisar sus vecinos en esta iteracion. Todas tienen distancia == i
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

                        //if (index < 0 || nextStepCheck.Contains(index) || closed.Contains(index)) // Quiza en vez de nextStepCheck simplemente se pueda revisar nextStep
                        //    continue;

                        if (index < 0 || remainingStep.Contains(index)) continue;
                        if (nextStepCheck.Contains(index) || closed.Contains(index))
                        {
                            if (allowedSteps[index] < allowedSteps[current] - 1)
                            {
                                allowedSteps.Remove(index);
                            }
                            else continue;
                        }

                        Zone otherZone = sectorizedTM.GetZone(newPos);
                        if (otherZone is null) continue;

                        if (!tilePos.TryGetValue(newPos, out LBSTile newTile)) continue;

                        int invIndex = inverseIndices[k];
                        if (invIndex == -1) continue;

                        string connection = connectedTM.GetConnections(newTile)[invIndex];

                        if (!((connection.Length == 4 && connection == "Door") ||
                              (connection.Length == 5 && connection == "Empty")))
                            continue;

                        allowedSteps.Add(index, allowedSteps[current] - 1);

                        evalInfo.visitedNodes++;

                        for (int j = from; j < others.Count; j++)
                        {
                            if (filtered.Contains(others[j])) continue;
                            if (index == others[j])
                            {
                                distances[from, j] = distances[j, from] = i + 1;
                                remainingOthers.Remove(index);
                                //if (!filtered.Contains(index))
                                found.Add(index);
                                if (remainingOthers.Count == 0) return;
                                allowedSteps[index] = limit;
                                break;
                            }
                        }

                        nextStep.Add(index);
                        nextStepCheck.Add(index);
                    }
                }

                foreach (int step in nextStep)
                    if (allowedSteps[step] > 0)
                        remainingStep.Enqueue(step);
                    else
                        allowedSteps.Remove(step);
            }
        }

        public static void PartialFloodFillChebyshev(int limit, int startPos, List<int> others, List<int> filtered, int from, out List<int> found, ref int[,] distances, Dictionary<Vector2Int, LBSTile> tilePos, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorizedTM, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
        {
            found = new List<int>() { };

            //if (from >= others.Count)
            //    return;

            List<int> remainingOthers = new List<int>(others.Except(filtered));
            //remainingOthers.RemoveRange(0, from);
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

            Dictionary<int, int> allowedSteps = new() { [startPos] = limit };

            List<Vector2Int> dirs = Directions.Bidimencional.All;
            int dirCount = dirs.Count;
            int[] inverseIndices = new int[dirCount];
            for (int k = 0; k < dirCount; k++)
            {
                inverseIndices[k] = dirs.FindIndex(d => d == -dirs[k]);
            }

            int i; // Distancia recorrida
            for (i = 0; remaining.Count > 0 && remainingStep.Count > 0; i++) // Casillas que falta por revisar
            {
                if (remainingStep.Count == 0)
                    break;

                HashSet<int> nextStepCheck = new HashSet<int>();
                List<int> nextStep = new List<int>();

                while (remainingStep.Count > 0) // Casillas que deben revisar sus vecinos en esta iteracion. Todas tienen distancia == i
                {
                    int current = remainingStep.Dequeue();

                    Vector2Int currentPos = chrom.ToGlobalPosition(current);

                    remaining.Remove(current);
                    closed.Add(current);

                    if (!tilePos.TryGetValue(currentPos, out LBSTile currentTile)) continue;
                    List<string> currentConnections = connectedTM.GetConnections(currentTile);

                    for (int k = 0; k < dirCount; k++)
                    {
                        int dir4low = k / 2;
                        int dir4high = (dir4low + 1) % 4;
                        List<string> currentDirConnections = new() { currentConnections[dir4low] };
                        if (k % 2 != 0)
                        {
                            if (!(tilePos.ContainsKey(currentPos + dirs[dir4low * 2]) && 
                                tilePos.ContainsKey(currentPos + dirs[dir4high * 2])))
                                continue;
                            currentDirConnections.Add(currentConnections[dir4high]);
                        }

                        bool impassable = false;
                        foreach (string conn in currentDirConnections)
                        {
                            if (!((conn.Length == 4 && conn == "Door") ||
                                (conn.Length == 5 && conn == "Empty")))
                            {
                                impassable = true;
                                break;
                            }
                        }
                        if (impassable) continue;

                        Vector2Int dir = dirs[k];
                        Vector2Int newPos = currentPos + dir;
                        int index = chrom.GlobalToIndex(newPos);

                        //if (index < 0 || nextStepCheck.Contains(index) || closed.Contains(index)) // Quiza en vez de nextStepCheck simplemente se pueda revisar nextStep
                        //    continue;

                        if (index < 0 || remainingStep.Contains(index)) continue;
                        bool updateSteps = false;
                        if (nextStepCheck.Contains(index) || closed.Contains(index))
                        {
                            if (allowedSteps[index] < allowedSteps[current] - 1)
                            {
                                updateSteps = true;
                            }
                            else continue;
                        }

                        Zone otherZone = sectorizedTM.GetZone(newPos);
                        if (otherZone is null) continue;

                        if (!tilePos.TryGetValue(newPos, out LBSTile newTile)) continue;

                        int invIndex = inverseIndices[k];
                        if (invIndex == -1) continue;

                        List<string> newConnections = connectedTM.GetConnections(newTile);
                        List<string> newDirConnections = new() { newConnections[invIndex / 2] };
                        if (k % 2 != 0) newDirConnections.Add(newConnections[(invIndex / 2 + 1) % 4]);
                        //string connection = connectedTM.GetConnections(newTile)[invIndex];

                        foreach (string conn in newDirConnections)
                        {
                            if (!((conn.Length == 4 && conn == "Door") ||
                              (conn.Length == 5 && conn == "Empty")))
                            {
                                impassable = true;
                                break;
                            }
                        }
                        if (impassable) continue;

                        if (updateSteps) allowedSteps.Remove(index);
                        allowedSteps.Add(index, allowedSteps[current] - 1);

                        evalInfo.visitedNodes++;

                        for (int j = from; j < others.Count; j++)
                        {
                            if (filtered.Contains(others[j])) continue;
                            if (index == others[j])
                            {
                                distances[from, j] = distances[j, from] = i + 1;
                                remainingOthers.Remove(index);
                                //if (!filtered.Contains(index))
                                found.Add(index);
                                if (remainingOthers.Count == 0) return;
                                allowedSteps[index] = limit;
                                break;
                            }
                        }

                        nextStep.Add(index);
                        nextStepCheck.Add(index);
                    }
                }

                foreach (int step in nextStep)
                    if (allowedSteps[step] > 0)
                        remainingStep.Enqueue(step);
                    else
                        allowedSteps.Remove(step);
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

        public static void PartialManhattan(int limit, int startPos, List<int> others, int from, out List<int> found, ref int[,] distances, BundleTilemapChromosome chrom)
        {
            found = new List<int>();
            for (int i = from; i < others.Count; i++)
            {
                Vector2Int v1 = chrom.ToMatrixPosition(startPos);
                Vector2Int v2 = chrom.ToMatrixPosition(others[i]);

                int dist = Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y);
                if (dist > limit)
                {
                    distances[i, from] = distances[from, i] = -1;
                }
                else
                {
                    distances[i, from] = distances[from, i] = dist;
                    found.Add(others[i]);
                }
            }
        }

        public class AStarNode
        {
            public AStarNode parent;

            public Vector2Int pos;

            public int givenCost;

            public int finalCost;

            public AStarNode(Vector2Int pos) { this.pos = pos; }

            public override bool Equals(object obj)
            {
                if (obj is not AStarNode other) return false;
                return pos.Equals(other.pos);
            }

            public override int GetHashCode()
            {
                return pos.GetHashCode();
            }

            public override string ToString()
            {
                return $"{pos} | g = {givenCost} , h = {finalCost - givenCost} , f = {finalCost}";
            }
        }

        public class JPSNode : AStarNode
        {
            public int fromDir;

            public JPSNode(Vector2Int pos) : base(pos) { }

            public override bool Equals(object obj) => base.Equals(obj);
            public override int GetHashCode() => base.GetHashCode();
            public override string ToString() => base.ToString();
        }

        public static int AStarRun(int startInd, int goalInd, Rect area, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
        {
            int cost = -1;
            if (connectedTM is null) return cost;

            PriorityQueue<AStarNode, int> open = new();
            HashSet<AStarNode> closed = new();

            AStarNode startNode = connectedTM.PathfindNodes[startInd];
            startNode.parent = null;
            startNode.givenCost = 0;
            startNode.finalCost = 0;

            open.Push(startNode, 0);

            Vector2Int goalPos = area.ToGlobalPosition(goalInd);

            while (!open.IsEmpty())
            {
                AStarNode current = open.Pop();

                if(current.pos == goalPos)
                {
                    cost = current.givenCost;
                    break;
                }

                //AStarNode parent = current.parent;

                List<string> currentConnections = connectedTM.GetConnections(current.pos);

                List<Vector2Int> dirs = Directions.Bidimencional.All;
                for(int i = 0; i < dirs.Count; i++)
                {
                    // Revisar muros
                    int dir4low = i / 2;
                    int dir4high = (dir4low + 1) % 4;
                    List<string> currentDirConnections = new() { currentConnections[dir4low] };
                    if (i % 2 != 0)
                    {
                        if(!(Array.Find(connectedTM.PathfindNodes, n => n is not null && n.pos.Equals(current.pos + dirs[dir4low * 2])) is not null &&
                            Array.Find(connectedTM.PathfindNodes, n => n is not null && n.pos.Equals(current.pos + dirs[dir4high * 2])) is not null))
                            continue;
                        currentDirConnections.Add(currentConnections[dir4high]);
                    }

                    bool impassable = false;
                    foreach(string conn in currentDirConnections)
                    {
                        if(!((conn.Length == 4 && conn == "Door") ||
                            (conn.Length == 5 && conn == "Empty")))
                        {
                            impassable = true;
                            break;
                        }
                    }
                    if (impassable) continue;

                    AStarNode newSuccesor = null;

                    Vector2Int newPos = current.pos + dirs[i];
                    if (connectedTM.GetPair(newPos) is null) continue;

                    List<string> newConnections = connectedTM.GetConnections(newPos);
                    List<string> newDirConnections = new() { newConnections[(dir4low + 2) % 4] };
                    if (i % 2 != 0) newDirConnections.Add(newConnections[(dir4high + 2) % 4]);

                    foreach (string conn in newDirConnections)
                    {
                        if (!((conn.Length == 4 && conn == "Door") ||
                          (conn.Length == 5 && conn == "Empty")))
                        {
                            impassable = true;
                            break;
                        }
                    }
                    if (impassable) continue;

                    if (connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] is null)
                    {
                        //newSuccesor = new AStarNode(newPos);
                        //connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] = newSuccesor;
                    }
                    else
                    {
                        newSuccesor = connectedTM.PathfindNodes[area.GlobalToIndex(newPos)];
                    }
                    int givenCost = current.givenCost + 1;

                    bool notNull = newSuccesor is not null;
                    bool contains = closed.Contains(newSuccesor);
                    bool costsLess = givenCost < newSuccesor.givenCost;
                    //if (newSuccesor.pos == new Vector2Int(4, 14))
                    //    ;
                    //if (contains && costsLess && newSuccesor.pos.x > 0 && newSuccesor.pos.x < 6 && newSuccesor.pos.y > 12 && newSuccesor.pos.y < 16)
                    //    ;

                    if(notNull && (!contains || costsLess))
                    {
                        newSuccesor.parent = current;
                        newSuccesor.givenCost = givenCost;

                        int dx = goalPos.x - newSuccesor.pos.x,
                            dy = goalPos.y - newSuccesor.pos.y;
                        newSuccesor.finalCost = givenCost + Mathf.Max(dx, dy); // Chebyshev

                        closed.Add(newSuccesor);

                        open.Push(newSuccesor, newSuccesor.finalCost);
                        evalInfo.visitedNodes++;
                    }
                }
            }

            return cost;
        }

        public static int PartialAStarRun(int limit, int startInd, int goalInd, Rect area, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
        {
            int cost = -1;
            if (connectedTM is null) return cost;

            PriorityQueue<AStarNode, int> open = new();
            HashSet<AStarNode> closed = new();

            AStarNode startNode = connectedTM.PathfindNodes[startInd];
            startNode.parent = null;
            startNode.givenCost = 0;
            startNode.finalCost = 0;

            open.Push(startNode, 0);

            Vector2Int goalPos = area.ToGlobalPosition(goalInd);

            while (!open.IsEmpty())
            {
                AStarNode current = open.Pop();

                if (current.pos == goalPos)
                {
                    cost = current.givenCost;
                    break;
                }

                //AStarNode parent = current.parent;

                List<string> currentConnections = connectedTM.GetConnections(current.pos);

                List<Vector2Int> dirs = Directions.Bidimencional.All;

                for (int i = 0; i < dirs.Count; i++)
                {
                    // Revisar muros
                    int dir4low = i / 2;
                    int dir4high = (dir4low + 1) % 4;
                    List<string> currentDirConnections = new() { currentConnections[dir4low] };
                    if (i % 2 != 0)
                    {
                        if (!(Array.Find(connectedTM.PathfindNodes, n => n is not null && n.pos.Equals(current.pos + dirs[dir4low * 2])) is not null &&
                            Array.Find(connectedTM.PathfindNodes, n => n is not null && n.pos.Equals(current.pos + dirs[dir4high * 2])) is not null))
                            continue;
                        currentDirConnections.Add(currentConnections[dir4high]);
                    }

                    bool impassable = false;
                    foreach (string conn in currentDirConnections)
                    {
                        if (!((conn.Length == 4 && conn == "Door") ||
                            (conn.Length == 5 && conn == "Empty")))
                        {
                            impassable = true;
                            break;
                        }
                    }
                    if (impassable) continue;

                    AStarNode newSuccesor = null;

                    Vector2Int newPos = current.pos + dirs[i];
                    if (connectedTM.GetPair(newPos) is null) continue;

                    List<string> newConnections = connectedTM.GetConnections(newPos);
                    List<string> newDirConnections = new() { newConnections[(dir4low + 2) % 4] };
                    if(i % 2 != 0) newDirConnections.Add(newConnections[(dir4high + 2) % 4]);

                    foreach (string conn in newDirConnections)
                    {
                        if (!((conn.Length == 4 && conn == "Door") ||
                          (conn.Length == 5 && conn == "Empty")))
                        {
                            impassable = true;
                            break;
                        }
                    }
                    if (impassable) continue;

                    if (connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] is null)
                    {
                        //newSuccesor = new AStarNode(newPos);
                        //connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] = newSuccesor;
                    }
                    else
                    {
                        newSuccesor = connectedTM.PathfindNodes[area.GlobalToIndex(newPos)];
                    }
                    int givenCost = current.givenCost + 1;

                    if (givenCost <= limit && newSuccesor is not null && (!closed.Contains(newSuccesor) || givenCost < newSuccesor.givenCost))
                    {
                        newSuccesor.parent = current;
                        newSuccesor.givenCost = givenCost;

                        int dx = goalPos.x - newSuccesor.pos.x,
                            dy = goalPos.y - newSuccesor.pos.y;
                        newSuccesor.finalCost = givenCost + Mathf.Max(dx, dy); // Chebyshev

                        closed.Add(newSuccesor);

                        open.Push(newSuccesor, newSuccesor.finalCost);
                        evalInfo.visitedNodes++;
                    }
                }
            }

            return cost;
        }

        public static class JPSPlus
        {
            public static Dictionary<Vector2Int, int[]> JPSPreprocessDistances(Rect area, ConnectedTileMapModule connectedTM)
            {
                List<Vector2Int> edges = Directions.Bidimencional.Edges;
                List<Vector2Int> allDirs = Directions.Bidimencional.All;

                Dictionary<Vector2Int, List<Vector2Int>> primaryJumpPoints = new();
                Dictionary<Vector2Int, int[]> JPDistances = connectedTM.Pairs.Where(tcp => area.Contains(tcp.Tile.Position)).ToDictionary(tcp => tcp.Tile.Position, tcp => new int[8]);

                List<string> impassableConnections = new() { SchemaBehaviour.Wall, SchemaBehaviour.Window };

                int xMin = (int)area.xMin,
                    xMax = (int)area.xMax,
                    yMin = (int)area.yMin,
                    yMax = (int)area.yMax;

                /// PRIMARY JUMP POINTS

                List<Tuple<TileConnectionsPair, int>> impassables = connectedTM.GetAllPairsWithConnections(impassableConnections.ToArray()).Where(t => area.Contains(t.Item1.Tile.Position)).ToList();

                foreach (Tuple<TileConnectionsPair, int> impassable in impassables)
                {
                    int dir = impassable.Item2;

                    List<string> parentConns = impassable.Item1.Connections;

                    FindPrimaryJumpPointAtDirection(impassable.Item1.Tile.Position, parentConns, dir, (dir + 1) % 4);
                    FindPrimaryJumpPointAtDirection(impassable.Item1.Tile.Position, parentConns, dir, (dir + 3) % 4);

                    void FindPrimaryJumpPointAtDirection(Vector2Int parentPos, List<string> parentConns, int sideDir, int JPDir, int c = 0)
                    {
                        if (c > 2)
                        {
                            //Debug.LogWarning("Recursion limit reached. Aborting.");
                            return;
                        }

                        if (impassableConnections.Contains(parentConns[JPDir])) return;

                        Vector2Int possibleJP = parentPos + edges[JPDir];
                        TileConnectionsPair JPNode = connectedTM.GetPair(possibleJP);

                        if (area.GlobalToIndex(possibleJP) == -1 || JPNode is null || impassableConnections.Contains(JPNode.Connections[sideDir])) return;

                        Vector2Int forcedNeigh = possibleJP + edges[sideDir];

                        if (area.GlobalToIndex(forcedNeigh) == -1 || connectedTM.GetPair(forcedNeigh) is null) return;

                        if (primaryJumpPoints.TryGetValue(possibleJP, out List<Vector2Int> JPDirs))
                        {
                            if (JPDirs.Contains(edges[JPDir])) return;
                            JPDirs.Add(edges[JPDir]);
                        }
                        else primaryJumpPoints.Add(possibleJP, new() { edges[JPDir] });

                        FindPrimaryJumpPointAtDirection(possibleJP, JPNode.Connections, (JPDir + 2) % 4, sideDir, c + 1);
                    }
                }

                /// STRAIGHT JUMP POINTS

                for (int y = yMin; y < yMax; y++)
                {
                    int jumpDistance = -1;
                    bool jumpPointSeen = false;

                    for (int x = xMin; x < xMax; x++)
                    {
                        Vector2Int currentNodePos = new Vector2Int(x, y);
                        TileConnectionsPair currentNode = connectedTM.GetPair(currentNodePos);
                        const int dir4 = 2, dir8 = dir4 * 2;

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

                    jumpDistance = -1;
                    jumpPointSeen = false;

                    for (int y = yMax - 1; y >= yMin; y--)
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
                }

                /// DIAGONAL JUMP POINTS

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

                        CalcDiagDist(5, x == xMin, y == xMin, nodePos, node);
                        CalcDiagDist(7, x == xMax - 1, y == yMin, nodePos, node);
                    }
                }

                for (int y = yMax - 1; y >= yMin; y--)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        nodePos = new Vector2Int(x, y);
                        node = connectedTM.GetPair(nodePos);
                        if (node is null) continue;

                        CalcDiagDist(3, x == xMin, y == yMax - 1, nodePos, node);
                        CalcDiagDist(1, x == xMax - 1, y == yMax - 1, nodePos, node);
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

            private static readonly List<int[]> validDirLookUpTable = new()
            {
                new[] {6, 7, 0, 1, 2},
                new[] {0, 1, 2},
                new[] {0, 1, 2, 3, 4},
                new[] {2, 3, 4},
                new[] {2, 3, 4, 5, 6},
                new[] {4, 5, 6},
                new[] {4, 5, 6, 7, 0},
                new[] {6, 7, 0}
            };

            public static int JPSRun(int startInd, int goalInd, Rect area, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
            {
                int cost = -1;
                if (connectedTM is null) return cost;

                Dictionary<Vector2Int, int[]> JPDistances = connectedTM.JPSDistances;

                PriorityQueue<JPSNode, int> open = new();
                HashSet<JPSNode> closed = new();

                JPSNode startNode = connectedTM.PathfindNodes[startInd] as JPSNode;
                startNode.parent = null;
                startNode.givenCost = 0;
                startNode.finalCost = 0;

                open.Push(startNode, 0);

                Vector2Int goalPos = area.ToGlobalPosition(goalInd);

                List<int> targetJumpPoints = new List<int>();

                while (!open.IsEmpty())
                {
                    JPSNode current = open.Pop();

                    if (current.pos == goalPos)
                    {
                        cost = current.givenCost;
                        break;
                    }

                    //JPSNode parent = current.parent as JPSNode;
                    int[] nodeDistances = JPDistances[current.pos];

                    int[] dirs = current.parent is null ? new[] { 0, 1, 2, 3, 4, 5, 6, 7 } : validDirLookUpTable[current.fromDir];
                    foreach (int dir in dirs)
                    {
                        JPSNode newSuccesor = null;
                        int givenCost = 0;

                        int maxGoalDiff = Mathf.Max(Mathf.Abs(goalPos.x - current.pos.x), Mathf.Abs(goalPos.y - current.pos.y)),
                            minGoalDiff = Mathf.Min(Mathf.Abs(goalPos.x - current.pos.x), Mathf.Abs(goalPos.y - current.pos.y));
                        if (dir % 2 == 0 &&
                            GoalAtDirection(current.pos, dir, goalPos, true) &&
                            maxGoalDiff <= Mathf.Abs(nodeDistances[dir]))
                        {
                            newSuccesor = connectedTM.PathfindNodes[goalInd] as JPSNode;
                            givenCost = current.givenCost + maxGoalDiff;
                        }
                        else if (dir % 2 == 1 &&
                            GoalAtDirection(current.pos, dir, goalPos) &&
                            minGoalDiff <= Mathf.Abs(nodeDistances[dir]))
                        {
                            // TARGET JUMP POINT
                            newSuccesor = GetNodeAtDist(current.pos, dir, minGoalDiff);
                            givenCost = current.givenCost + Mathf.Max(Mathf.Abs(newSuccesor.pos.x - current.pos.x), Mathf.Abs(newSuccesor.pos.y - current.pos.y));
                        }
                        else if (nodeDistances[dir] > 0)
                        {
                            newSuccesor = GetNodeAtDist(current.pos, dir, nodeDistances[dir]);
                            givenCost = Mathf.Max(Mathf.Abs(newSuccesor.pos.x - current.pos.x), Mathf.Abs(newSuccesor.pos.y - current.pos.y)) + current.givenCost;
                        }

                        if (newSuccesor is not null && (!closed.Contains(newSuccesor) || givenCost < newSuccesor.givenCost))
                        {
                            newSuccesor.parent = current;
                            newSuccesor.givenCost = givenCost;
                            newSuccesor.fromDir = dir;

                            int dx = goalPos.x - newSuccesor.pos.x,
                                dy = goalPos.y - newSuccesor.pos.y;
                            newSuccesor.finalCost = givenCost + Mathf.Max(dx, dy); // Chebyshev

                            closed.Add(newSuccesor);

                            open.Push(newSuccesor, newSuccesor.finalCost);
                            evalInfo.visitedNodes++;
                        }
                    }
                }

                while (targetJumpPoints.Count > 0)
                {
                    connectedTM.PathfindNodes[targetJumpPoints[0]] = null;
                    targetJumpPoints.RemoveAt(0);
                }

                return cost;

                /// LOCAL FUNCTIONS

                bool GoalAtDirection(Vector2Int pos, int dir, Vector2Int goalPos, bool exact = false)
                {
                    if (dir < 0 && dir >= 8) return false;

                    int dx = goalPos.x - pos.x,
                        dy = goalPos.y - pos.y;
                    bool equalMagnitude = Mathf.Abs(dy) == Mathf.Abs(dx);

                    bool[] returns = new bool[]
                    {
                        dy == 0 && dx > 0,
                        dy > 0  && dx > 0   && (equalMagnitude || !exact),
                        dy > 0  && dx == 0,
                        dy > 0  && dx < 0   && (equalMagnitude || !exact),
                        dy == 0 && dx < 0,
                        dy < 0  && dx < 0   && (equalMagnitude || !exact),
                        dy < 0  && dx == 0,
                        dy < 0  && dx > 0   && (equalMagnitude || !exact)
                    };

                    return returns[dir];
                }

                JPSNode GetNodeAtDist(Vector2Int pos, int dir, int dist)
                {
                    JPSNode newNode = null;
                    List<Vector2Int> dirs = Directions.Bidimencional.All;
                    Vector2Int move = dirs[dir] * dist;
                    Vector2Int newPos = pos + move;

                    if (connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] is null)
                    {
                        newNode = new JPSNode(newPos);
                        connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] = newNode;
                        targetJumpPoints.Add(area.GlobalToIndex(newPos));
                    }
                    else
                    {
                        newNode = connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] as JPSNode;
                    }

                    return newNode;
                }
            }

            public static int PartialJPSRun(int limit, int startInd, int goalInd, Rect area, ConnectedTileMapModule connectedTM, ref EvaluationInfo evalInfo)
            {
                int cost = -1;
                if (connectedTM is null) return cost;

                Dictionary<Vector2Int, int[]> JPDistances = connectedTM.JPSDistances;

                PriorityQueue<JPSNode, int> open = new();
                HashSet<JPSNode> closed = new();

                JPSNode startNode = connectedTM.PathfindNodes[startInd] as JPSNode;
                startNode.parent = null;
                startNode.givenCost = 0;
                startNode.finalCost = 0;

                open.Push(startNode, 0);

                Vector2Int goalPos = area.ToGlobalPosition(goalInd);

                List<int> targetJumpPoints = new List<int>();

                while (!open.IsEmpty())
                {
                    JPSNode current = open.Pop();

                    if (current.pos == goalPos)
                    {
                        cost = current.givenCost;
                        break;
                    }

                    JPSNode parent = current.parent as JPSNode;
                    int[] nodeDistances = JPDistances[current.pos];

                    int[] dirs = current.parent is null ? new[] { 0, 1, 2, 3, 4, 5, 6, 7 } : validDirLookUpTable[current.fromDir];
                    foreach (int dir in dirs)
                    {
                        JPSNode newSuccesor = null;
                        int givenCost = 0;

                        int maxGoalDiff = Mathf.Max(Mathf.Abs(goalPos.x - current.pos.x), Mathf.Abs(goalPos.y - current.pos.y)),
                            minGoalDiff = Mathf.Min(Mathf.Abs(goalPos.x - current.pos.x), Mathf.Abs(goalPos.y - current.pos.y));
                        if (dir % 2 == 0 &&
                            GoalAtDirection(current.pos, dir, goalPos, true) &&
                            maxGoalDiff <= Mathf.Abs(nodeDistances[dir]))
                        {
                            newSuccesor = connectedTM.PathfindNodes[goalInd] as JPSNode;
                            givenCost = current.givenCost + maxGoalDiff;
                        }
                        else if (dir % 2 == 1 &&
                            GoalAtDirection(current.pos, dir, goalPos) &&
                            minGoalDiff <= Mathf.Abs(nodeDistances[dir]))
                        {
                            // TARGET JUMP POINT
                            newSuccesor = GetNodeAtDist(current.pos, dir, minGoalDiff);
                            givenCost = current.givenCost + Mathf.Max(Mathf.Abs(newSuccesor.pos.x - current.pos.x), Mathf.Abs(newSuccesor.pos.y - current.pos.y));
                        }
                        else if (nodeDistances[dir] > 0)
                        {
                            newSuccesor = GetNodeAtDist(current.pos, dir, nodeDistances[dir]);
                            givenCost = Mathf.Max(Mathf.Abs(newSuccesor.pos.x - current.pos.x), Mathf.Abs(newSuccesor.pos.y - current.pos.y)) + current.givenCost;
                        }

                        if (givenCost <= limit && newSuccesor is not null && (!closed.Contains(newSuccesor) || givenCost < newSuccesor.givenCost))
                        {
                            newSuccesor.parent = current;
                            newSuccesor.givenCost = givenCost;
                            newSuccesor.fromDir = dir;

                            int dx = goalPos.x - newSuccesor.pos.x,
                                dy = goalPos.y - newSuccesor.pos.y;
                            newSuccesor.finalCost = givenCost + Mathf.Max(dx, dy); // Chebyshev

                            closed.Add(newSuccesor);

                            open.Push(newSuccesor, newSuccesor.finalCost);
                            evalInfo.visitedNodes++;
                        }
                    }
                }

                while (targetJumpPoints.Count > 0)
                {
                    connectedTM.PathfindNodes[targetJumpPoints[0]] = null;
                    targetJumpPoints.RemoveAt(0);
                }

                return cost;

                /// LOCAL FUNCTIONS

                bool GoalAtDirection(Vector2Int pos, int dir, Vector2Int goalPos, bool exact = false)
                {
                    if (dir < 0 && dir >= 8) return false;

                    int dx = goalPos.x - pos.x,
                        dy = goalPos.y - pos.y;
                    bool equalMagnitude = Mathf.Abs(dy) == Mathf.Abs(dx);

                    bool[] returns = new bool[]
                    {
                        dy == 0 && dx > 0,
                        dy > 0  && dx > 0   && (equalMagnitude || !exact),
                        dy > 0  && dx == 0,
                        dy > 0  && dx < 0   && (equalMagnitude || !exact),
                        dy == 0 && dx < 0,
                        dy < 0  && dx < 0   && (equalMagnitude || !exact),
                        dy < 0  && dx == 0,
                        dy < 0  && dx > 0   && (equalMagnitude || !exact)
                    };

                    return returns[dir];
                }

                JPSNode GetNodeAtDist(Vector2Int pos, int dir, int dist)
                {
                    JPSNode newNode = null;
                    List<Vector2Int> dirs = Directions.Bidimencional.All;
                    Vector2Int move = dirs[dir] * dist;
                    Vector2Int newPos = pos + move;

                    if (connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] is null)
                    {
                        newNode = new JPSNode(newPos);
                        connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] = newNode;
                        targetJumpPoints.Add(area.GlobalToIndex(newPos));
                    }
                    else
                    {
                        newNode = connectedTM.PathfindNodes[area.GlobalToIndex(newPos)] as JPSNode;
                    }

                    return newNode;
                }
            }
        }
    }
}
