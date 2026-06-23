using ISILab.Commons.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BSPDungeonGenerator
{
    private int[,] map;
    private int minPartitionSize;
    private int minRoomSize;
    private int roomCounter;

    // Node class representing each partition
    private class Leaf
    {
        public int x, y, width, height;
        public Leaf leftChild, rightChild;
        public RectInt room;
        public int roomId = 0;
        private bool async;

        public Leaf(int x, int y, int width, int height, bool async = false)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.async = async;
        }

        public bool Split(int minSize)
        {
            if (leftChild != null || rightChild != null) return false; // Already split

            // Randomly decide split direction
            bool splitH = SafeRandom.Value(async) > 0.5f;

            // Force split direction if the partition is too wide or too tall
            if (width > height && width / (float)height >= 1.25f) splitH = false; // Cut vertically
            else if (height > width && height / (float)width >= 1.25f) splitH = true; // Cut horizontally

            int max = (splitH ? height : width) - minSize;
            if (max <= minSize) return false; // Too small to split further

            int split = SafeRandom.Range(minSize, max, async);

            if (splitH)
            {
                leftChild = new Leaf(x, y, width, split, async);
                rightChild = new Leaf(x, y + split, width, height - split, async);
            }
            else
            {
                leftChild = new Leaf(x, y, split, height, async);
                rightChild = new Leaf(x + split, y, width - split, height, async);
            }
            return true;
        }
    }

    /// <summary>
    /// Generates a dungeon matrix using BSP.
    /// </summary>
    /// <param name="gridWidth">Total width of the map.</param>
    /// <param name="gridHeight">Total height of the map.</param>
    /// <param name="minPartition">Minimum size a partition can be split into.</param>
    /// <param name="minRoom">Minimum size of the actual room inside a partition.</param>
    /// <returns>A 2D int array: 0=Empty, 1=Corridor, >1=Rooms.</returns>
    public int[,] Generate(int gridWidth, int gridHeight, int minPartition, int minRoom, bool async = false)
    {
        map = new int[gridWidth, gridHeight];
        minPartitionSize = minPartition;
        minRoomSize = minRoom;

        // Start counting rooms at 2 (0 = empty, 1 = corridor)
        roomCounter = 2;

        Leaf root = new Leaf(0, 0, gridWidth, gridHeight, async);
        List<Leaf> leaves = new List<Leaf> { root };

        bool didSplit = true;
        while (didSplit)
        {
            didSplit = false;
            for (int i = 0; i < leaves.Count; i++)
            {
                Leaf l = leaves[i];
                if (l.leftChild == null && l.rightChild == null)
                {
                    // Attempt to split if it's large enough or randomly
                    if (l.width > minPartitionSize * 2 || l.height > minPartitionSize * 2 || SafeRandom.Value(async) > 0.25f)
                    {
                        if (l.Split(minPartitionSize))
                        {
                            leaves.Add(l.leftChild);
                            leaves.Add(l.rightChild);
                            didSplit = true;
                        }
                    }
                }
            }
        }

        // Recursively create rooms and corridors starting from the root
        CreateRoomsAndCorridors(root, async);

        return map;
    }

    private void CreateRoomsAndCorridors(Leaf leaf, bool async)
    {
        if (leaf.leftChild != null || leaf.rightChild != null)
        {
            if (leaf.leftChild != null) CreateRoomsAndCorridors(leaf.leftChild, async);
            if (leaf.rightChild != null) CreateRoomsAndCorridors(leaf.rightChild, async);

            // Connect the two children with a corridor
            if (leaf.leftChild != null && leaf.rightChild != null)
            {
                CreateCorridor(GetRoomFromLeaf(leaf.leftChild, async), GetRoomFromLeaf(leaf.rightChild, async), async);
            }
        }
        else
        {
            // This is a bottom-level leaf. Create a room inside it.
            int maxW = Mathf.Max(minRoomSize, leaf.width - 2);
            int maxH = Mathf.Max(minRoomSize, leaf.height - 2);

            int w = SafeRandom.Range(minRoomSize, maxW, async);
            int h = SafeRandom.Range(minRoomSize, maxH, async);

            // Random position inside the leaf, ensuring at least 1 pixel of padding
            int rx = SafeRandom.Range(leaf.x + 1, leaf.x + leaf.width - w - 1, async);
            int ry = SafeRandom.Range(leaf.y + 1, leaf.y + leaf.height - h - 1, async);

            leaf.room = new RectInt(rx, ry, w, h);
            leaf.roomId = roomCounter++;

            // Draw the room onto the map array
            for (int x = rx; x < rx + w; x++)
            {
                for (int y = ry; y < ry + h; y++)
                {
                    map[x, y] = leaf.roomId;
                }
            }
        }
    }

    private RectInt GetRoomFromLeaf(Leaf leaf, bool async)
    {
        if (leaf != null)
        {
            if (leaf.room.width > 0) return leaf.room;

            RectInt lRoom = GetRoomFromLeaf(leaf.leftChild, async);
            RectInt rRoom = GetRoomFromLeaf(leaf.rightChild, async);

            if (lRoom.width == 0 && rRoom.width == 0) return new RectInt();
            if (lRoom.width > 0 && rRoom.width == 0) return lRoom;
            if (rRoom.width > 0 && lRoom.width == 0) return rRoom;

            // If both children have rooms, pick one randomly to connect from
            return SafeRandom.Value(async) > 0.5f ? lRoom : rRoom;
        }
        return new RectInt();
    }

    private void CreateCorridor(RectInt roomA, RectInt roomB, bool async)
    {
        // Get the center points of both rooms
        Vector2Int pointA = new Vector2Int(roomA.x + roomA.width / 2, roomA.y + roomA.height / 2);
        Vector2Int pointB = new Vector2Int(roomB.x + roomB.width / 2, roomB.y + roomB.height / 2);

        // Randomly choose whether to draw Horizontal-Vertical or Vertical-Horizontal
        if (SafeRandom.Value(async) > 0.5f)
        {
            DrawLine(pointA.x, pointA.y, pointB.x, pointA.y); // Horizontal
            DrawLine(pointB.x, pointA.y, pointB.x, pointB.y); // Vertical
        }
        else
        {
            DrawLine(pointA.x, pointA.y, pointA.x, pointB.y); // Vertical
            DrawLine(pointA.x, pointB.y, pointB.x, pointB.y); // Horizontal
        }
    }

    private void DrawLine(int x1, int y1, int x2, int y2)
    {
        int xMin = Mathf.Min(x1, x2);
        int xMax = Mathf.Max(x1, x2);
        int yMin = Mathf.Min(y1, y2);
        int yMax = Mathf.Max(y1, y2);

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                // Draw corridor (1) ONLY if the space is empty (0)
                // This prevents corridors from overwriting room IDs
                if (map[x, y] == 0)
                {
                    map[x, y] = 1;
                }
            }
        }
    }
}