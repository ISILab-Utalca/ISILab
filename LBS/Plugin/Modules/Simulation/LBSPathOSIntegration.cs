using UnityEngine;
using PathOS;

public class LBSPathOSIntegration : EntityObstaclePair
{
    
        // GABO TODO: Make it usable with MULTIPLE agents! (currently it can't due to navmesh changes affecting all agents)
        /// <summary>
        /// Toggle dynamic obstacle connections of this entity (if any) for the given agent.
        /// Includes both marked up entities and Testing Layer walls.
        /// If Testing Layer walls were added or removed, re-bakes NavMesh.
        /// </summary>
        /// <param name="agent"> Agent which is being affected by the toggling. </param>
        /// <param name="entity"> Entity which dynamic obstacles we're toggling.</param>
        public void ToggleDynamicObstacles(PathOSAgent agent, LevelEntity entity)
        {
            // Abort if no dynamic obstacles connected
            if (entity.dynamicObstacles.Count == 0) { return; }

            bool IsWallsAddedOrRemoved = false; // Modify NavMesh only if walls were added or removed
            foreach (EntityObstaclePair connectedObject in entity.dynamicObstacles)
            {
                // CLOSE: Make object visible
                if (connectedObject.connectionType == PathOSObstacleConnections.Category.CLOSE)
                {
                    // Walls
                    if (connectedObject.entityObjectRef.name == "WallPrefab")
                    {
                        agent.eyes.RemoveInvisibleWall(connectedObject.entityObjectRef);
                        IsWallsAddedOrRemoved = true;
                    }
                    // Entities
                    else
                    {
                        agent.eyes.RemoveInvisibleEntity(GetEntity(connectedObject.entityObjectRef));
                    }
                }
                // OPEN: Make object invisible
                else if (connectedObject.connectionType == PathOSObstacleConnections.Category.OPEN)
                {
                    // Walls
                    if (connectedObject.entityObjectRef.name == "WallPrefab")
                    {
                        agent.eyes.AddInvisibleWall(connectedObject.entityObjectRef);
                        IsWallsAddedOrRemoved = true;
                    }
                    // Entities
                    else
                    {
                        agent.eyes.AddInvisibleEntity(GetEntity(connectedObject.entityObjectRef));
                    }
                }
                else
                {
                    Debug.LogError("EntityObstaclePair object has no connectionType set!");
                }
            }

            // Re-Bake NavMesh (if walls were added or removed)
            if (IsWallsAddedOrRemoved)
            {
                GenerateNavMeshFromLBSModules(agent);
            }
        }
}
