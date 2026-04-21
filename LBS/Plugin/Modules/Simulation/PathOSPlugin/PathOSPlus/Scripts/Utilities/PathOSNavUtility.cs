using NinePenguins;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

/*
PathOSNavUtility.cs 
PathOSNavUtility (c) Nine Penguins (Samantha Stahlke) 2018
*/

namespace PathOS
{
    public class PathOSNavUtility
    {
        //Simple class for defining the boundaries of a NavMesh in the XZ plane.
        [System.Serializable]
        public class NavmeshBoundsXZ
        {
            public float altitudeSampleHeight { get; set; }
            public Vector3 centre { get; set; }
            public Vector3 min;
            public Vector3 max;
            public Vector3 size { get; set; }

            public NavmeshBoundsXZ()
            {
                altitudeSampleHeight = 0.0f;
                min = new Vector3(float.MaxValue, 0.0f, float.MaxValue);
                max = new Vector3(float.MinValue, 0.0f, float.MinValue);
                size = Vector3.zero;
                centre = Vector3.zero;
            }

            public void RecomputeCentreAndSize()
            {
                size = new Vector3(max.x - min.x, 0.0f, max.z - min.z);
                centre = 0.5f * (max + min);
            }
        }

        //boundaries of a NavMesh in the XYZ plane.
        [System.Serializable]
        public class NavmeshBoundsXYZ
        {
            public float altitudeSampleHeight { get; set; }
            public Vector3 centre { get; set; }
            public Vector3 min;
            public Vector3 max;
            public Vector3 size { get; set; }

            public NavmeshBoundsXYZ()
            {
                altitudeSampleHeight = 0.0f;
                min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                size = Vector3.zero;
                centre = Vector3.zero;
            }

            public void RecomputeCentreAndSize()
            {
                size = new Vector3(max.x - min.x, max.y - min.y, max.z - min.z);
                centre = 0.5f * (max + min);
            }
        }

        //Maintains a (for now) yes-no "visited map" of the environment.
        public class NavmeshMemoryMapper
        {
            public struct NavmeshMemoryMapperCastHit
            {
                public int numUnexplored;
                public float portionUnexplored;
                public float distance;
            }

            public class AStarTile
            {
                public int xCoord = 0;
                public int yCoord = 0;
                public int zCoord = 0;
                public Vector3 point = Vector3.zero;
                public float gScore = 1000.0f;
                public float hScore = 1000.0f;
                public float fScore = 1000.0f;
                public float penalty = 0.0f;

                public AStarTile parent = null;

                public AStarTile() { }

                public AStarTile(AStarTile parent)
                {
                    this.xCoord = parent.xCoord;
                    this.yCoord = parent.yCoord;
                    this.zCoord = parent.zCoord;
                    this.gScore = parent.gScore + 1;
                    this.parent = parent;
                }

                public void AddPenalty(float penalty)
                {
                    this.penalty = penalty;

                    RecomputeF();
                }

                public void UpdateScores(AStarTile dest)
                {
                    this.hScore = Mathf.Abs(dest.xCoord - this.xCoord)
                        + Mathf.Abs(dest.zCoord - this.zCoord);

                    RecomputeF();
                }

                private void RecomputeF()
                {
                    fScore = (hScore == 0) ? -PathOS.Constants.Behaviour.SCORE_MAX
                        : gScore + hScore + penalty;
                }

                public void ChangeParentOptimal(AStarTile parent)
                {
                    if (parent.fScore + 1 < this.fScore)
                    {
                        this.parent = parent;
                        this.gScore = parent.gScore + 1;
                        RecomputeF();
                    }
                }

                public void InsertByScore(ref List<AStarTile> list)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i].fScore >= this.fScore)
                        {
                            list.Insert(i, this);
                            return;
                        }
                    }

                    list.Add(this);
                }

                //Equality is determined by location.
                public static bool operator ==(AStarTile lhs, AStarTile rhs)
                {
                    if (object.ReferenceEquals(lhs, null))
                        return object.ReferenceEquals(rhs, null);

                    if (object.ReferenceEquals(rhs, null))
                        return object.ReferenceEquals(lhs, null);

                    return lhs.xCoord == rhs.xCoord && lhs.zCoord == rhs.zCoord;
                }

                public static bool operator !=(AStarTile lhs, AStarTile rhs)
                {
                    if (object.ReferenceEquals(lhs, null))
                        return !object.ReferenceEquals(rhs, null);

                    if (object.ReferenceEquals(rhs, null))
                        return !object.ReferenceEquals(lhs, null);

                    return lhs.xCoord != rhs.xCoord || lhs.zCoord != rhs.zCoord;
                }

                public override bool Equals(object obj)
                {
                    if (null == obj)
                        return false;

                    AStarTile objAsTile = obj as AStarTile;

                    if (objAsTile == default(AStarTile))
                        return false;

                    return this == objAsTile;
                }

                public override int GetHashCode()
                {
                    return xCoord * zCoord;
                }
            }

            public enum NavmeshMapCode
            {
                NM_DNE = -1,
                NM_UNKNOWN = 0,
                NM_SEEN = 10,
                NM_OBSTACLE = 50,
                NM_VISITED = 100
            };

            public PathOSAgentMemory memory;

            NavmeshBoundsXYZ bounds;
            Vector3 sampleGridSize;
            public const int maxCastSamples = 128;

            Vector3 gridOrigin;
            NavmeshMapCode[,,] visitedGrid;

            private int activeFloor = 0;
            public int ActiveFloor
            {
                get => activeFloor;
                set => activeFloor = value;
            }

            private Texture2D[] visualGrid;
            private bool[] visualGridDirty;

            public NavmeshMemoryMapper(Vector3 gridScale, int floorCount)
            {
                this.sampleGridSize = gridScale;

                //Test autodetection of NavMesh bounds.
                NavMeshTriangulation navDetails = NavMesh.CalculateTriangulation();
                NavmeshBoundsXYZ autoBounds = new NavmeshBoundsXYZ();

                Vector3 v = Vector3.zero;

                for (int i = 0; i < navDetails.vertices.Length; ++i)
                {
                    v = navDetails.vertices[i];

                    if (v.x < autoBounds.min.x)
                        autoBounds.min.x = v.x;
                    if (v.x > autoBounds.max.x)
                        autoBounds.max.x = v.x;
                    if (v.y < autoBounds.min.y)
                        autoBounds.min.y = v.y;
                    if (v.y > autoBounds.max.y)
                        autoBounds.max.y = v.y;
                    if (v.z < autoBounds.min.z)
                        autoBounds.min.z = v.z;
                    if (v.z > autoBounds.max.z)
                        autoBounds.max.z = v.z;
                }
                /*autoBounds.min.y = 0;
                autoBounds.max.y = floorCount * gridScale.y;//*/

                //Round bounds areas up to the nearest grid tile and add 1 tilesize
                //at each end.
                autoBounds.min = RoundExtrema(autoBounds.min, true);
                autoBounds.max = RoundExtrema(autoBounds.max, false);

                autoBounds.RecomputeCentreAndSize();
                SetBounds(autoBounds, floorCount);
            }

            private Vector3 RoundExtrema(Vector3 extrema, bool minimum)
            {
                // X
                float signx = (extrema.x < 0) ? -1.0f : 1.0f;
                float resultx = (Mathf.Floor(Mathf.Abs(extrema.x / sampleGridSize.x))) * sampleGridSize.x;
                resultx *= signx;

                //For a minimum, we always want to "step down" to add a margin to the map.
                //For a maximum, we do the opposite.
                resultx += (minimum) ? -2.0f * sampleGridSize.x : 2.0f * sampleGridSize.x;
                
                // Y
                float signy = (extrema.y < 0) ? -1.0f : 1.0f;
                float resulty = (Mathf.Floor(Mathf.Abs(extrema.y / sampleGridSize.y))) * sampleGridSize.y;
                resulty *= signy;
                //resulty += (minimum) ? -2.0f * sampleGridSize.y : 2.0f * sampleGridSize.y;

                // Z
                float signz = (extrema.z < 0) ? -1.0f : 1.0f;
                float resultz = (Mathf.Floor(Mathf.Abs(extrema.z / sampleGridSize.z))) * sampleGridSize.z;
                resultz *= signz;
                resultz += (minimum) ? -2.0f * sampleGridSize.z : 2.0f * sampleGridSize.z;

                return new (resultx, resulty, resultz);
            }

            private void SetBounds(NavmeshBoundsXYZ bounds, int floorCount)
            {
                this.bounds = bounds;
                gridOrigin = bounds.min;
                Debug.Log("Grid origin: " + gridOrigin);

                //Calculate the grid size based on NavMesh extents and grid sampling edge.
                int sizeX = (int)(bounds.size.x / sampleGridSize.x) + 1;
                int sizeZ = (int)(bounds.size.z / sampleGridSize.z) + 1;

                visitedGrid = new NavmeshMapCode[sizeX, floorCount, sizeZ];

                //Create a texture to represent the grid for on-screen display.
                visualGrid = new Texture2D[floorCount];
                visualGridDirty = new bool[floorCount];
                for (int i = 0; i < visualGrid.Length; i++)
                {
                    visualGrid[i] = new Texture2D(sizeX, sizeZ, TextureFormat.ARGB32, false, true);
                    visualGrid[i].filterMode = FilterMode.Point;
                    visualGridDirty[i] = false;
                }

                for (int j = 0; j < floorCount; j++)
                {
                    for (int i = 0; i < sizeX; i++)
                    {
                        for(int k = 0; k < sizeZ; k++)
                        {
                            visitedGrid[i, j, k] = NavmeshMapCode.NM_UNKNOWN;
                            visualGrid[j].SetPixel(i, k, PathOS.UI.mapUnknown);
                        }
                    }
                    visualGrid[j].Apply();
                }

            }

            public Vector3 GetBoundsSize()
            {
                return bounds.size;
            }

            public float GetAspect()
            {
                return (float)visualGrid[activeFloor].width / (float)visualGrid[activeFloor].height;
            }

            public Texture2D GetVisualGrid(int floor)
            {
                if (floor < 0 || floor >= visualGrid.Length)
                {
                    Debug.LogError("floor index out of bounds");
                    return null;
                }
                return visualGrid[floor];
            }

            private void GetGridCoords(Vector3 point, ref int gridX, ref int gridY, ref int gridZ)
            {
                //Calculate a vector from the grid's origin to the sample point.
                Vector3 diff = point - gridOrigin;

                //Calculate grid indices based on sampling size.
                gridX = (int)(diff.x / sampleGridSize.x);
                gridY = (int)(diff.y / sampleGridSize.y);
                gridZ = (int)(diff.z / sampleGridSize.z);
                Debug.Log($"({point.y}) - ({diff.y}) - ({gridY})");
                //Debug.Log($"({sampleGridSize.x} {sampleGridSize.y} {sampleGridSize.z})");
                //Debug.Log($"({gridX} {gridY} {gridZ})");
                //Debug.Log($"({point.x} {point.y} {point.z})");
                return;
            }

            private Vector3 GetPoint(int gridX, int gridZ)
            {
                Vector3 diff = Vector3.zero;

                //Calculate difference between grid origin and sample tile in world units.
                diff.x = gridX * sampleGridSize.x;
                diff.z = gridZ * sampleGridSize.z;

                //Compute point based on grid origin.
                Vector3 point = gridOrigin + diff;
                point.y = bounds.altitudeSampleHeight;

                return point;
            }

            private NavmeshMapCode SampleMap(Vector3 point)
            {
                //Calculate grid indices.
                int gridX = 0, gridY = 0, gridZ = 0;
                GetGridCoords(point, ref gridX, ref gridY, ref gridZ);

                if (gridX >= 0 && gridY >= 0 && gridZ >= 0
                    && gridX < visitedGrid.GetLength(0)
                    && gridY < visitedGrid.GetLength(1)
                    && gridZ < visitedGrid.GetLength(2))
                    return visitedGrid[gridX, gridY, gridZ];
                else
                    return NavmeshMapCode.NM_DNE;
            }

            public void RayMemoryMap(Ray ray, float maxDistance, out NavmeshMemoryMapperCastHit hit, bool fillSeen = false, bool raycast = true)
            {
                if (raycast)
                {
                    RaycastMemoryMap(ray.origin, ray.direction, maxDistance, out hit, fillSeen);
                }
                else
                {
                    PointMemoryMap(ray, maxDistance, out hit, fillSeen);
                }
            }

            public void PointMemoryMap(Ray ray, float maxDistance, out NavmeshMemoryMapperCastHit hit, bool fillSeen = false)
            {
                Vector3 point = ray.origin;

                Vector3 d = ray.direction;
                d.Normalize();

                //What is our sampling distance?
                //Depending on the angle between the direction and the grid lines,
                //this will fluctuate - we effectively want to sample so 
                //we'll hit in one-tile increments.
                //This could be improved later to be less approximate and hit
                //every tile the ray would cross.
                float theta = Vector3.Angle(Vector3.forward, d);
                theta = Mathf.Abs(theta);

                //Debug.Log(string.Format("Theta: {0:0.000}", theta));

                theta -= (int)(theta / 90.0f) * 90.0f;

                if (theta > 45.0f)
                    theta = 90.0f - theta;

                //Debug.Log(string.Format("Clamped Theta: {0:0.000}", theta));

                float sampleDistance = sampleGridSize.y / Mathf.Cos(Mathf.Deg2Rad * theta);
                //Debug.Log(string.Format("Grid Size: {0:0.000}, Sampling distance: {1:0.000}", sampleGridSize, sampleDistance));

                d = sampleDistance * d;

                int numUnexplored = 0, totalSampled = 0;
                float totalDistance = 0.0f;
                int obstacleCount = 0;
                NavmeshMapCode sample = NavmeshMapCode.NM_DNE;

                for (int i = 1; (i * sampleDistance) < maxDistance && i < maxCastSamples; ++i)
                {
                    if((i+1) * sampleDistance > maxDistance)
                    {
                        sample = SampleMap(point);

                        if (sample == NavmeshMapCode.NM_UNKNOWN)
                            ++numUnexplored;
                        else if (sample == NavmeshMapCode.NM_OBSTACLE)
                            ++obstacleCount;
                        //Stop if we reach the edge of the grid or we've crossed more than 
                        //one obstacle tile (avoid mistaking corners for walls).
                        else if (sample == NavmeshMapCode.NM_DNE || obstacleCount > 1)
                            break;

                        //Fill in sight information, if applicable.
                        if (fillSeen)
                            Fill(point, NavmeshMapCode.NM_SEEN);

                        ++totalSampled;
                    }
                    
                    point += d;

                    totalDistance += sampleDistance;
                }

                hit.numUnexplored = numUnexplored;
                hit.distance = totalDistance;
                hit.portionUnexplored = (totalSampled > 0) ? (float)numUnexplored / (float)totalSampled : 0.0f;
            }

            //In-progress memory raycast.
            //Right now the distance will be an estimation of the straight-line distance 
            //traversable in that direction, and unexplored tiles will stop being counted
            //if the ray samples from an obstacle tile.
            public void RaycastMemoryMap(Vector3 origin, Vector3 dir, float maxDistance, out NavmeshMemoryMapperCastHit hit,
                bool fillSeen = false)
            {
                Vector3 point = origin;

                Vector3 d = new Vector3(dir.x, dir.y/*0.0f*/, dir.z);
                d.Normalize();

                //What is our sampling distance?
                //Depending on the angle between the direction and the grid lines,
                //this will fluctuate - we effectively want to sample so 
                //we'll hit in one-tile increments.
                //This could be improved later to be less approximate and hit
                //every tile the ray would cross.
                float theta = Vector3.Angle(Vector3.forward, d);
                theta = Mathf.Abs(theta);

                //Debug.Log(string.Format("Theta: {0:0.000}", theta));

                theta -= (int)(theta / 90.0f) * 90.0f;

                if (theta > 45.0f)
                    theta = 90.0f - theta;

                //Debug.Log(string.Format("Clamped Theta: {0:0.000}", theta));

                float sampleDistance = sampleGridSize.y / Mathf.Cos(Mathf.Deg2Rad * theta);
                //Debug.Log(string.Format("Grid Size: {0:0.000}, Sampling distance: {1:0.000}", sampleGridSize, sampleDistance));

                d = sampleDistance * d;

                int numUnexplored = 0, totalSampled = 0;
                float totalDistance = 0.0f;
                int obstacleCount = 0;
                NavmeshMapCode sample = NavmeshMapCode.NM_DNE;

                for (int i = 1; (i * sampleDistance) < maxDistance && i < maxCastSamples; ++i)
                {
                    sample = SampleMap(point);

                    if (sample == NavmeshMapCode.NM_UNKNOWN)
                        ++numUnexplored;
                    else if (sample == NavmeshMapCode.NM_OBSTACLE)
                        ++obstacleCount;
                    //Stop if we reach the edge of the grid or we've crossed more than 
                    //one obstacle tile (avoid mistaking corners for walls).
                    else if (sample == NavmeshMapCode.NM_DNE || obstacleCount > 1)
                        break;

                    //Fill in sight information, if applicable.
                    if (fillSeen)
                        Fill(point, NavmeshMapCode.NM_SEEN);

                    ++totalSampled;
                    point += d;

                    totalDistance += sampleDistance;
                }

                hit.numUnexplored = numUnexplored;
                hit.distance = totalDistance;
                hit.portionUnexplored = (totalSampled > 0) ? (float)numUnexplored / (float)totalSampled : 0.0f;
            }

            public void Fill(Vector3 point, NavmeshMapCode code = NavmeshMapCode.NM_VISITED)
            {
                //Calculate grid indices.
                int gridX = 0, gridY = 0, gridZ = 0;
                GetGridCoords(point, ref gridX, ref gridY, ref gridZ);

                //int adjustedY = (int) (gridY / sampleGridSize.y);// - 1;
                //if(gridY < 0) adjustedY = 0;
                //Debug.Log($"({point.y}) - ({gridY})");// - ({adjustedY})");
                if (gridX < 0 || gridY < 0 || gridZ < 0
                    || gridX >= visitedGrid.GetLength(0)
                    || gridY >= visitedGrid.GetLength(1)
                    || gridZ >= visitedGrid.GetLength(2))
                {
                    //NPDebug.LogError("Navmesh sample location outside of grid bounds!\n" +
                    //    "Check that navmesh is baked properly. Otherwise there is an " +
                    //    "issue with PathOS' Navmesh border detection!",
                    //    typeof(NavmeshMemoryMapper));

                    return;
                }

                NavmeshMapCode oldCode = default;
                oldCode = visitedGrid[gridX, gridY, gridZ];

                //Override based on priority of codes.
                if (oldCode >= code)
                    return;

                visitedGrid[gridX, gridY, gridZ] = code;

                Color fillColor = PathOS.UI.mapUnknown;

                switch (code)
                {
                    case NavmeshMapCode.NM_VISITED:
                        fillColor = PathOS.UI.mapVisited;
                        break;

                    case NavmeshMapCode.NM_SEEN:
                        fillColor = PathOS.UI.mapSeen;
                        break;

                    case NavmeshMapCode.NM_OBSTACLE:
                        fillColor = PathOS.UI.mapObstacle;
                        break;
                }

                visualGrid[gridY].SetPixel(gridX, gridZ, fillColor);
                visualGridDirty[gridY] = true;
            }

            public void BakeVisualGrid()
            {
                for(int i = 0; i < visualGrid.Length; i++)
                {
                    if (visualGridDirty[i])
                    {
                        visualGrid[i].Apply();
                        visualGridDirty[i] = false;
                    }
                }
            }

            private void GetAdjacentWalkable(ref List<AStarTile> adjacent,
                ref AStarTile parent, ref AStarTile dest)
            {
                adjacent.Clear();

                AStarTile left = new AStarTile(parent);
                --left.xCoord;

                AStarTile right = new AStarTile(parent);
                ++right.xCoord;

                AStarTile up = new AStarTile(parent);
                ++up.yCoord;

                AStarTile down = new AStarTile(parent);
                --down.yCoord;

                AStarTile front = new AStarTile(parent);
                --front.zCoord;

                AStarTile back = new AStarTile(parent);
                ++back.zCoord;

                if (Walkable(left.xCoord, left.yCoord, left.zCoord))
                {
                    left.point = GetPoint(left.xCoord, left.zCoord);
                    left.UpdateScores(dest);
                    left.AddPenalty(memory.MovementHazardPenalty(left.point));
                    adjacent.Add(left);
                }

                if (Walkable(right.xCoord, right.yCoord, right.zCoord))
                {
                    right.point = GetPoint(right.xCoord, right.zCoord);
                    right.UpdateScores(dest);
                    right.AddPenalty(memory.MovementHazardPenalty(right.point));
                    adjacent.Add(right);
                }

                if (Walkable(up.xCoord, up.yCoord, up.zCoord))
                {
                    up.point = GetPoint(up.xCoord, up.zCoord);
                    up.UpdateScores(dest);
                    up.AddPenalty(memory.MovementHazardPenalty(up.point));
                    adjacent.Add(up);
                }

                if (Walkable(down.xCoord, down.yCoord, down.zCoord))
                {
                    down.point = GetPoint(down.xCoord, down.zCoord);
                    down.UpdateScores(dest);
                    down.AddPenalty(memory.MovementHazardPenalty(down.point));
                    adjacent.Add(down);
                }

                if (Walkable(front.xCoord, front.yCoord, front.zCoord))
                {
                    front.point = GetPoint(front.xCoord, front.zCoord);
                    front.UpdateScores(dest);
                    front.AddPenalty(memory.MovementHazardPenalty(front.point));
                    adjacent.Add(front);
                }

                if (Walkable(back.xCoord, back.yCoord, back.zCoord))
                {
                    back.point = GetPoint(back.xCoord, back.zCoord);
                    back.UpdateScores(dest);
                    back.AddPenalty(memory.MovementHazardPenalty(back.point));
                    adjacent.Add(back);
                }
            }

            public bool NavigateAStar(Vector3 start, Vector3 dest, ref List<Vector3> waypoints)
            {
                int gridX = 0, gridY = 0, gridZ = 0;
                waypoints.Clear();

                //Define tiles for the start and destination.
                GetGridCoords(start, ref gridX, ref gridY, ref gridZ);
                AStarTile startTile = new AStarTile();
                startTile.xCoord = gridX;
                startTile.zCoord = gridZ;
                startTile.point = GetPoint(startTile.xCoord, startTile.zCoord);

                GetGridCoords(dest, ref gridX, ref gridY, ref gridZ);
                AStarTile destTile = new AStarTile();
                destTile.xCoord = gridX;
                destTile.zCoord = gridZ;
                destTile.point = GetPoint(destTile.xCoord, destTile.zCoord);

                startTile.gScore = Mathf.Abs(startTile.xCoord - destTile.xCoord)
                    + Mathf.Abs(startTile.zCoord - destTile.zCoord);

                List<AStarTile> open = new List<AStarTile>();
                List<AStarTile> adjacent = new List<AStarTile>();
                List<AStarTile> closed = new List<AStarTile>();

                bool complete = false;
                bool destinationReached = false;

                AStarTile curTile = startTile;

                //NPDebug.LogMessage("Initialized A-Star.");

                while (!complete)
                {
                    closed.Add(curTile);

                    if (curTile == destTile)
                    {
                        //NPDebug.LogMessage("Reached destination.");
                        complete = true;
                        destinationReached = true;
                        break;
                    }

                    GetAdjacentWalkable(ref adjacent, ref curTile, ref destTile);

                    for (int i = 0; i < adjacent.Count; ++i)
                    {
                        if (closed.Contains(adjacent[i]))
                            continue;

                        if (open.Contains(adjacent[i]))
                        {
                            AStarTile existingTile = open.Find(tile => tile == adjacent[i]);
                            existingTile.ChangeParentOptimal(curTile);
                            continue;
                        }

                        adjacent[i].InsertByScore(ref open);
                    }

                    if (open.Count == 0)
                    {
                        complete = true;
                        break;
                    }

                    int maxIndex = 0;

                    for (int i = 1; i < open.Count; ++i)
                    {
                        if (open[i].fScore <= open[0].fScore)
                            maxIndex = i;
                        else
                            break;
                    }

                    //Stochasticity introduced for promoting less
                    //"robotic" behaviour.
                    int selectedIndex = UnityEngine.Random.Range(0, maxIndex + 1);
                    curTile = open[selectedIndex];
                    open.RemoveAt(selectedIndex);
                }

                List<AStarTile> path = new List<AStarTile>();

                //Construct final path.
                if (!destinationReached)
                {
                    //"Try" to navigate back - take the tile that got closest and build path.
                    AStarTile best = closed[0];

                    for (int i = 1; i < closed.Count; ++i)
                    {
                        if (closed[i].hScore < best.hScore)
                            best = closed[i];
                    }

                    curTile = best;
                }

                //curTile is either our destination or the best tile found.
                AStarTile lastInsert;

                while (curTile != startTile)
                {
                    path.Insert(0, curTile);
                    lastInsert = curTile;

                    do
                    {
                        curTile = curTile.parent;

                    } while (curTile != startTile
                    && Vector3.SqrMagnitude(curTile.point - lastInsert.point)
                    < PathOS.Constants.Navigation.WAYPOINT_DIST_MIN_SQR);
                }

                //Skip targeting of the destination tile.
                //(This will happen automatically when the agent reaches
                //the last waypoint before the target.)
                for (int i = 0; i < path.Count - 1; ++i)
                {
                    waypoints.Add(path[i].point);
                }

                return waypoints.Count > 0;
            }

            private bool Walkable(int x, int y, int z)
            {
                if (x < 0 || y < 0 || z < 0
                    || x >= visitedGrid.GetLength(0)
                    || y >= visitedGrid.GetLength(1)
                    || z >= visitedGrid.GetLength(2))
                    return false;

                return visitedGrid[x, y, z] != NavmeshMapCode.NM_UNKNOWN
                    && visitedGrid[x, y, z] != NavmeshMapCode.NM_OBSTACLE;
            }

            // GABO: Reset Obstacles
            // ***Temporary solution to use when rebaking map, in order to prevent the agent from getting stuck due to
            // believing it's surrounded by obstacles that may now be unblocked.
            public void ResetObstacles()
            {
                for (int j = 0; j < visitedGrid.GetLength(1); j++)
                {
                    for (int i = 0; i < visitedGrid.GetLength(0); i++)
                    {
                        for(int k = 0; k < visitedGrid.GetLength(2); k++)
                        {
                            if (visitedGrid[i, j, k] == NavmeshMapCode.NM_OBSTACLE)
                            {
                                visitedGrid[i, j, k] = NavmeshMapCode.NM_UNKNOWN;
                                visualGrid[j].SetPixel(i, k, PathOS.UI.mapUnknown);
                            }
                        }
                    }
                    visualGridDirty[j] = true;
                }
            }
        }

        public static bool GetClosestPointWalkable(Vector3 p, float margin, ref Vector3 result)
        {
            NavMeshHit hitResult = new NavMeshHit();

            bool found = NavMesh.SamplePosition(p, out hitResult, margin, NavMesh.AllAreas);

            if (found)
                result = hitResult.position;

            return found;
        }

        public static Vector3 XZPos(Vector3 p)
        {
            p.y = 0.0f;
            return p;
        }

        // GABO: Determines if a given NavMesh agent can reach the specified target position.
        // Due to "GetClosestPointWalkable()" not calculating if a NavMesh agent is ACTUALLY able
        // to get to "p" beyond a limited "margin" (understandable since the original code always
        // expected a completely connected NavMesh) we create the needed auxiliary method.
        public static bool CanAgentReachTarget(NavMeshAgent agent, Vector3 target, float margin, ref Vector3 result)
        {
            // Get sampled position from NavMesh
            NavMeshHit hitResult = new NavMeshHit();
            bool found = NavMesh.SamplePosition(target, out hitResult, margin, NavMesh.AllAreas);
            if (found)
            {
                result = hitResult.position;
            }
            else
            {
                Debug.LogWarning("CanAgentReachTarget(): Target failed position sampling! Is target on the NavMesh?");
                return false;
            }

            // Calculate if path between agent and sampled position exists
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(result, path))
            {
                // Check if the path is complete
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // GABO TEMP FIX: For some reason, other agents baking their own meshes invalidates path calculation,
                // so the warning is limited to single agent testing. It is not consistent with every test map either,
                // like "PlusSign5Room", or not always at least.
                if (Resources.FindObjectsOfTypeAll<PathOSAgent>().Length == 1)
                {
                    Debug.LogWarning("CanAgentReachTarget(): Invalid pathfinding! Is agent (and/or target) on the NavMesh?");
                }
                return false;
            }
        }

        // GABO: Gets agent type ID from name.
        // ***Helpful method, just in case.
        // (https://web.archive.org/web/20210919121830/https://answers.unity.com/questions/1650130/change-agenttype-at-runtime.html)
        public static int GetAgentTypeIdByName(string agentTypeName)
        {
            int count = NavMesh.GetSettingsCount();
            for (var i = 0; i < count; i++)
            {
                int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                string name = NavMesh.GetSettingsNameFromID(id);
                if (name == agentTypeName)
                {
                    return id;
                }
            }
            return -1;
        }
    }
}

