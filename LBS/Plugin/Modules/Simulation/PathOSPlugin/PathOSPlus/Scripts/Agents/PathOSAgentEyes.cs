using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/*
PathOSAgentEyes.cs 
PathOSAgentEyes (c) Nine Penguins (Samantha Stahlke) 2018-19
*/

namespace PathOS
{
    [RequireComponent(typeof(PathOSAgent))]
    public class PathOSAgentEyes : MonoBehaviour
    {
        private PathOSAgent agent;
        private static PathOSManager manager;

        //The agent's "eyes" - i.e., the camera the player would use.
        [PathOSDisplayName("Player Camera")]
        [Tooltip("The camera representing the player's view (agent's \"eyes\")")]
        public Camera cam;

        [Header("Navmesh \"Sight\"")]

        [PathOSDisplayName("Visibility Size Threshold")]
        [Tooltip("How large must an object appear on screen (expressed" +
            "as a percentage of viewport width) before it can be considered" +
            "visible?")]
        public float visSizeThreshold = 0.05f;

        [PathOSDisplayName("Raycast Distance")]
        [Tooltip("How far the agent \"looks\" over the navmesh " +
            "when scanning for obstacles/exploration targets. " +
            "(This value should be positive.)")]

        public float navmeshCastDistance = 50.0f;

        [PathOSDisplayName("Raycast Height")]
        [Tooltip("The y-value at which the agent \"looks\" for " +
            "obstacles/navigation targets.")]

        public float navmeshCastHeight = 5.0f;

        //What can the agent "see" currently?
        public List<PerceivedEntity> visible { get; set; }
        public List<PerceivedEntity> perceptionInfo { get; set; }

        //Timer to handle visual processing checks. Roll for perception.
        private float perceptionTimer = 0.0f;

        //Vertex set for visibility checks on object bounds.
        private Vector3[] boundsCheck;

        //Field of view for immediate-range "explorability" checks.
        private float xFOV;

        // GABO: List of invisible marked up entities (to this agent)
        public List<LevelEntity> invisibleEntities { get; private set; }
        // GABO: List of invisible walls (to this agent)
        public List<GameObject> invisibleWalls { get; private set; }

        Quaternion fixedRotation;

        void Awake()
        {
            agent = GetComponent<PathOSAgent>();

            visible = new List<PerceivedEntity>();
            perceptionInfo = new List<PerceivedEntity>();

            if (null == manager)
                manager = PathOSManager.instance;

            Vector3 target = cam.transform.position;
            cam.transform.Translate(Vector3.up * 10);
            cam.transform.LookAt(target);
            fixedRotation = cam.transform.rotation;

            for (int i = 0; i < manager.levelEntities.Count; ++i)
            {
                LevelEntity entity = manager.levelEntities[i];
                Vector3 entityPos = entity.objectRef.transform.position;

                Vector3 entityVecXZ = entityPos - cam.transform.position;
                entityVecXZ.y = 0.0f;

                perceptionInfo.Add(new PerceivedEntity(entity));
            }

            boundsCheck = new Vector3[8];

            xFOV = Mathf.Rad2Deg * 2.0f * Mathf.Atan(
                cam.aspect * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f));

            // GABO: Setup empty list of invisible entities and walls
            invisibleEntities = new();
            invisibleWalls = new();
        }

        void Update()
        {
            //cam.transform.rotation = fixedRotation;

            perceptionTimer += Time.deltaTime;

            //Visual processing update.
            if (perceptionTimer >= PathOS.Constants.Perception.PERCEPTION_COMPUTE_TIME)
            {
                ProcessPerception();
            }
        }

        public float XFOV()
        {
            return xFOV;
        }

        public void ProcessPerception()
        {
            Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(cam);

            visible.Clear();

            Vector3 camForwardXZ = cam.transform.forward;
            //camForwardXZ.y = 0.0f;

            for (int i = 0; i < perceptionInfo.Count; ++i)
            {
                PerceivedEntity entity = perceptionInfo[i];

                Vector3 entityPos = entity.entityRef.objectRef.transform.position;

                Vector3 entityVecXZ = entityPos - cam.transform.position;
                //entityVecXZ.y = 0.0f;

                bool wasVisible = entity.visible;

                //Visibility check - this can change between checks as the agent
                //moves around.
                entity.entityRef.UpdateBounds();

                entity.visible =
                    !invisibleEntities.Contains(entity.entityRef) // GABO: Ignore invisible entities (to be used for OPEN entities)
                    && Vector3.Dot(camForwardXZ, entityVecXZ) > 0
                    && GeometryUtility.TestPlanesAABB(frustum, entity.entityRef.bounds)
                    && entity.entityRef.SizeVisibilityCheck(cam, visSizeThreshold)
                    && RaycastVisibilityCheck(entity.entityRef.bounds, entityPos); // GABO: Modified to ignore invisible walls.

                /// SEBA NOTES
                /// 1. is not invisible
                /// 2. is in front
                /// 3. is in frustum
                /// 4. is sized enough
                /// 5. no obstacles in view

                if (wasVisible != entity.visible)
                {
                    entity.visibilityTimer = 0.0f;
                    entity.impressionMade = false;
                }

                //Keep track of how long the object has been in the current visibility state.
                entity.visibilityTimer = entity.visibilityTimer + perceptionTimer;

                if (entity.visible)
                {
                    visible.Add(entity);

                    if (entity.visibilityTimer >= PathOS.Constants.Memory.IMPRESSION_TIME_MIN)
                    {
                        if (!entity.impressionMade)
                        {
                            entity.impressionMade = true;

                            if (entity.impressionCount < PathOS.Constants.Memory.IMPRESSIONS_MAX)
                                ++entity.impressionCount;
                        }

                        entity.perceivedPos = entityPos;
                        agent.memory.Memorize(entity);

                        //Mandatory/completion/boss goals are committed to LTM automatically.
                        if (entity.entityType == EntityType.ET_GOAL_MANDATORY
                            || entity.entityType == EntityType.ET_GOAL_COMPLETION
                             || entity.entityType == EntityType.ET_HAZARD_ENEMY_BOSS)
                            agent.memory.CommitUnforgettable(entity);

                        agent.memory.TryCommitLTM(entity);
                    }
                }
            }

            perceptionTimer = 0.0f;

            Debug.LogWarning(visible.Count);
        }

        //Uses an AABB and given position as nine points for checking
        //visibility via raycast.
        bool RaycastVisibilityCheck(Bounds bounds, Vector3 pos)
        {
            Vector3 ray = cam.transform.position - pos;

            // GABO: We add the invisible walls (and children) to the "Ignore Raycast" layer
            // temporarily and mask the raycasts with it so they're ignored.
            Dictionary<GameObject, int> oldLayers = new();
            foreach (GameObject wall in invisibleWalls)
            {
                // Parent
                oldLayers[wall] = wall.layer;
                wall.layer = LayerMask.NameToLayer("Ignore Raycast");
                // Children
                Transform[] children = wall.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    // Ignore self to avoid bugs
                    if (child.transform == wall.transform) { continue; }

                    oldLayers[child.gameObject] = child.gameObject.layer;
                    child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
            }

            if (!Physics.Raycast(pos, ray.normalized, out RaycastHit hit, ray.magnitude, ~LayerMask.GetMask("Ignore Raycast")))
            {
                // GABO: We return invisible walls (and children) to their original layer
                foreach (GameObject wall in invisibleWalls)
                {
                    wall.layer = oldLayers[wall];
                    Transform[] children = wall.GetComponentsInChildren<Transform>();
                    foreach (Transform child in children)
                    {
                        // Ignore self to avoid bugs
                        if (child.transform == wall.transform) { continue; }

                        child.gameObject.layer = oldLayers[child.gameObject];
                    }
                }
                return true;
            }

            boundsCheck[0].Set(bounds.min.x, bounds.min.y, bounds.min.z);
            boundsCheck[1].Set(bounds.min.x, bounds.min.y, bounds.max.z);
            boundsCheck[2].Set(bounds.min.x, bounds.max.y, bounds.min.z);
            boundsCheck[3].Set(bounds.min.x, bounds.max.y, bounds.max.z);
            boundsCheck[4].Set(bounds.max.x, bounds.min.y, bounds.min.z);
            boundsCheck[5].Set(bounds.max.x, bounds.min.y, bounds.max.z);
            boundsCheck[6].Set(bounds.max.x, bounds.max.y, bounds.min.z);
            boundsCheck[7].Set(bounds.max.x, bounds.max.y, bounds.max.z);

            for (int i = 0; i < boundsCheck.Length; ++i)
            {
                ray = cam.transform.position - boundsCheck[i];

                if (!Physics.Raycast(boundsCheck[i], ray.normalized, ray.magnitude, ~LayerMask.GetMask("Ignore Raycast")))
                {
                    // GABO: We return invisible walls (and children) to their original layer
                    foreach (GameObject wall in invisibleWalls)
                    {
                        wall.layer = oldLayers[wall];
                        Transform[] children = wall.GetComponentsInChildren<Transform>();
                        foreach (Transform child in children)
                        {
                            // Ignore self to avoid bugs
                            if (child.transform == wall.transform) { continue; }

                            child.gameObject.layer = oldLayers[child.gameObject];
                        }
                    }
                    return true;
                }
            }

            // GABO: We return invisible walls (and children) to their original layer
            foreach (GameObject wall in invisibleWalls)
            {
                wall.layer = oldLayers[wall];
                Transform[] children = wall.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    // Ignore self to avoid bugs
                    if (child.transform == wall.transform) { continue; }

                    child.gameObject.layer = oldLayers[child.gameObject];
                }
            }

            return false;
        }

        //This should be updated eventually to do a more sophisticated check accounting
        //for *apparent* distance - i.e., by adding a couple of physics raycasts from the 
        //camera.
        public NavMeshHit ExploreVisibilityCheck(Vector3 origin, Vector3 dir, out bool result)
        {
            Vector3 position = origin;
            float distance = 0.0f;
            //bool result;

            NavMeshHit hit = new NavMeshHit();

            //result = NavMesh.Raycast(origin,
            //    origin + dir.normalized * navmeshCastDistance/* + Vector3.up * navmeshCastHeight*/,
            //    out hit, NavMesh.AllAreas);
            //position = hit.position;
            //distance = hit.distance;


            result = Physics.Raycast(origin, dir, out RaycastHit raycastHit, navmeshCastDistance);
            position = raycastHit.point;
            distance = raycastHit.distance; // No retorna esta distancia, sino la de hit.
            if (result)
            {
                result = NavMesh.SamplePosition(raycastHit.point, out hit, 1, NavMesh.AllAreas);
                if (result)
                {
                    position = hit.position;
                    distance = Vector3.Distance(origin, position);
                }
            }

            if (!result)
            {
                hit.distance = distance;
                hit.position = position;
            }

            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMapCode fillCode = PathOSNavUtility.NavmeshMemoryMapper.NavmeshMapCode.NM_SEEN;
            //(result) ?
            //    PathOSNavUtility.NavmeshMemoryMapper.NavmeshMapCode.NM_OBSTACLE :
            //    PathOSNavUtility.NavmeshMemoryMapper.NavmeshMapCode.NM_SEEN;

            agent.memory.memoryMap.Fill(position, fillCode);

            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit memHit =
                new PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit();

            agent.memory.memoryMap.RayMemoryMap(new Ray(origin, dir), distance, out memHit, true, false); // No mapea 1 a 1, sino a lo largo, en lineas.

            return hit;
        }

        // GABO: Add to list of invisible objects, so this agent can't see it.
        public void AddInvisibleEntity(LevelEntity newInvisibleEntity)
        {
            invisibleEntities.Add(newInvisibleEntity);
        }
        // GABO: Remove from list of invisible objects, making it visible again.
        public void RemoveInvisibleEntity(LevelEntity invisibleEntityToRemove)
        {
            invisibleEntities.Remove(invisibleEntityToRemove);
        }
        // GABO: Add wall to list of invisible walls, so this agent can see through them.
        public void AddInvisibleWall(GameObject wall)
        {
            invisibleWalls.Add(wall);
        }
        // GABO: Remove wall from list of invisible walls, so they block this agent's view.
        public void RemoveInvisibleWall(GameObject wall)
        {
            invisibleWalls.Remove(wall);
        }
    }

}
