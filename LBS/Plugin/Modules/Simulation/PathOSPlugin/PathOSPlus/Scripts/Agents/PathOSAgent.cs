
using ISILab.LBS.Plugin.Modules.Simulation.PathOSPlus.OGVis.Scripts;
using NinePenguins;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
PathOSAgent.cs 
PathOSAgent (c) Samantha Stahlke and Atiya Nova 2018
*/

namespace PathOS
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(PathOSAgentMemory))]
    [RequireComponent(typeof(PathOSAgentEyes))]
    [RequireComponent(typeof(HeuristicOS))]
    public class PathOSAgent : MonoBehaviour
    {
        #region FIELDS

        [Header("Agent Tuning")]
        public AgentOS tuning = new();

        [Header("Health Tuning")]
        public HealthOS healthTuning = new();

        [Header("Memory Tuning")]
        public MemoryOS memoryTuning = new();


        [HideInInspector] public NavigationState navigationState = new();
        [HideInInspector] public ExplorationState explorationState = new();
        [HideInInspector] public MemoryState memoryState = new();
        [HideInInspector] public HealthState healthState = new();


        internal NavMeshAgent navAgent;
        public HeuristicOS heuristics = new();

        private GameObject cameraObject;
        private static bool cameraFollow;

        //Used for testing.
        [Range(1.0f, 8.0f)]
        public float timeScale = 1.0f;
        public bool freezeAgent;
        private bool verboseDebugging = false;

        #endregion

        #region PROPERTIES

        internal static PathOSManager manager;
        public static OGLogManager logger { get; set; }

        public PathOSAgentMemory memory { get; set; }
        public PathOSAgentEyes eyes { get; private set; }

        public float visitThresholdSqr { get; set; }

        public bool completed { get; set; }

        public float hazardPenalty { get; set; }

        public float MemPathChance
        {
            get => memoryState.memPathChance;
            set => memoryState.memPathChance = value;
        }

        [SerializeField]
        public float ExperienceScale { get => tuning.experienceScale; internal set => tuning.experienceScale = value; }

        #endregion

        private void Awake()
        {
            healthState.Init();
            Debug.Log(healthState.health);
            eyes = GetComponent<PathOSAgentEyes>();
            memory = GetComponent<PathOSAgentMemory>();
            navAgent = GetComponent<NavMeshAgent>();
            heuristics = gameObject.GetComponent<HeuristicOS>();

     
            completed = false;

            cameraObject = GameObject.FindWithTag("PathOSCamera");

            navigationState.currentDest = new TargetDest();
            navigationState.currentDest.pos = GetPosition();

            memoryState.memPathWaypoints = new List<Vector3>();
            explorationState.unreachableReference = new List<Vector3>();

            manager ??= PathOSManager.instance;
            logger ??= OGLogManager.instance;

            heuristics.Init(this);

        }

        private void Start()
        {
            LogAgentData();
            PerceptionUpdate();

            //Stochastic initialization of look time.
            navigationState.lookTimer = Random.Range(0.0f, navigationState.lookTime);
        }

        private void OnDestroy()
        {
            // GABO: Resets global time scale when destroyed (prevents affecting Time.timeScale beyond agent lifetime
            // when using Agent Batching)
            if (name.Contains("Temporary Batch Agent"))
            {
                Time.timeScale = 1.0f;
            }
        }

        private void LogAgentData()
        {
            if (logger != null)
            {
                string header = "";

                header += "HEURISTICS,";
                header += "EXPERIENCE," + tuning.experienceScale + ",";

                foreach (HeuristicScale scale in heuristics.modifiableHeuristicScales)
                {
                    header += scale.heuristic + "," + scale.scale + ",";
                }

                logger.WriteHeader(this.gameObject, header);
            }
        }

        public Vector3 GetPosition()
        {
            return navAgent.transform.position;
        }

        public Vector3 GetEyesPosition()
        {
            return eyes.cam.transform.position;
        }

        public Vector3 GetOriginPos()
        {
            switch (eyes.camType)
            {
                case PathOSAgentEyes.CamType.FreeMode:      return GetEyesPosition();
                case PathOSAgentEyes.CamType.FirstPerson:   return GetPosition();
            }

            return default;
        }

        public void RecalibratePath()
        {
            navAgent.ResetPath();
            navigationState.ResetDestinationSelf(this);
            //ComputeNewDestination();
        }

        public void ResetCamera()
        {
            if (cameraObject != null) cameraObject.transform.position = new Vector3(transform.position.x, 15.0f, transform.position.z);
        }
        public void ToggleCameraFollow()
        {
            cameraFollow = !cameraFollow;
        }
        private float RouteComputeTimeCalculated()
        {
            return Constants.Navigation.ROUTE_COMPUTE_BASE
                + Constants.Memory.RETRIEVAL_TIME * memory.entities.Count;
        }

        //Used by the Inspector to ensure scale widgets will appear for all defined heuristics.
        //This SHOULD NOT be called by anything else.
        public void RefreshHeuristicList()
        {
            if (!heuristics) return;

            Dictionary<Heuristic, float> weights = new Dictionary<Heuristic, float>();

            for (int i = 0; i < heuristics.modifiableHeuristicScales.Count; ++i)
            {
                Heuristic heuristic = heuristics.modifiableHeuristicScales[i].heuristic;
                float scale = heuristics.modifiableHeuristicScales[i].scale;
                weights.Add(heuristic, scale);
            }

            heuristics.modifiableHeuristicScales.Clear();

            foreach (Heuristic heuristic in System.Enum.GetValues(typeof(Heuristic)))
            {
                float weight = 0.0f;

                if (weights.ContainsKey(heuristic)) weight = weights[heuristic];
                heuristics.modifiableHeuristicScales.Add(new HeuristicScale(heuristic, weight));
            }
        }

        //Update the agent's target position.
        private void ComputeNewDestination()
        {
            //Base target = our existing destination.
            TargetDest dest = new TargetDest(navigationState.currentDest);

            //Clear the list of candidate destinations.
            navigationState.destList.Clear();

            float maxScore = -10000.0f;

            explorationState.pastCumulativeEntityScore = explorationState.cumulativeEntityScore;
            explorationState.cumulativeEntityScore = 0.0f;

            //Potential entity goals.
            EntityMemory currentGoalMemory = null;

            Vector3 eyesForward = default;
            //Used in the calculation of exploration directions.
            switch (eyes.camType)
            {
                case PathOSAgentEyes.CamType.FreeMode:
                    eyesForward = eyes.cam.transform.forward;
                    break;

                case PathOSAgentEyes.CamType.FirstPerson:
                    eyesForward = transform.forward;
                    eyesForward.y = 0.0f;
                    eyesForward.Normalize();
                    break;
            }

            Vector3 yRotationAxis = eyes.cam.transform.up;
            Vector3 xRotationAxis = eyes.cam.transform.right;

            //Optimization: Score current goal first to reduce
            //extra computation, since the current goal receives a score bonus.
            if (navigationState.currentDest.entity != null)
            {
                currentGoalMemory = memory.GetMemory(navigationState.currentDest.entity);

                if (null == currentGoalMemory)
                {
                    NPDebug.LogError("Something went wrong! Targeting " +
                        navigationState.currentDest.entity.entityRef.objectRef.name +
                        " but it could not be found in agent memory!",
                        typeof(PathOSAgent));
                }
                else
                    ScoreEntity(currentGoalMemory, ref maxScore);
            }
            else
            {
                Vector3 goalForward = default;
                switch (eyes.camType)
                {
                    case PathOSAgentEyes.CamType.FreeMode:
                        goalForward = navigationState.currentDest.pos - GetEyesPosition();
                        break;

                    case PathOSAgentEyes.CamType.FirstPerson:
                        goalForward = navigationState.currentDest.pos - GetPosition();
                        goalForward.y = 0.0f;
                        break;
                }

                if (goalForward.sqrMagnitude > 0.1f)
                {
                    goalForward.Normalize();
                    float angleToGoal = Vector3.Angle(eyesForward, goalForward);
                    bool goalVisible = Mathf.Abs(angleToGoal) < (eyes.XFOV() * 0.5f); // FP. Generalizar.
                    
                    ScoreExploreDirection(GetOriginPos(), goalForward, goalVisible, ref maxScore,
                        true, navigationState.currentDest.pos);
                }
            }

            for (int i = 0; i < memory.entities.Count; ++i)
            {
                if (!ReferenceEquals(currentGoalMemory, memory.entities[i]))
                    ScoreEntity(memory.entities[i], ref maxScore);
            }

            //Potential directional goals.

            //Memorized paths.
            //Treated as not visible since they are based on the player's "idea" of the space.
            for (int i = 0; i < memory.paths.Count; ++i)
            {
                ScoreExploreDirection(memory.paths[i].originPoint,
                    memory.paths[i].direction,
                    false, ref maxScore);
            }

            //Only considering the XZ plane.
            float halfX = eyes.XFOV() * 0.5f;
            int steps = (int)(halfX / tuning.exploreDegrees);

            float halfY = eyes.cam.fieldOfView * 0.5f;
            int stepsY = eyes.camType == PathOSAgentEyes.CamType.FreeMode ? (int)(halfY / tuning.exploreDegrees) : 0;

            for(int j = 0; j <= stepsY; ++j)
            {
                Vector3 XRotated = Quaternion.AngleAxis(j * tuning.exploreDegrees, xRotationAxis) * eyesForward;
                Vector3 negXRotated = Quaternion.AngleAxis(j * -tuning.exploreDegrees, xRotationAxis) * eyesForward;

                ScoreExploreDirection(GetOriginPos(), XRotated, true, ref maxScore);

                for (int i = 1; i <= steps; ++i)
                {
                    ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * tuning.exploreDegrees, yRotationAxis) * XRotated,
                        true, ref maxScore);
                    ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * -tuning.exploreDegrees, yRotationAxis) * XRotated,
                        true, ref maxScore);

                    ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * tuning.exploreDegrees, yRotationAxis) * negXRotated,
                        true, ref maxScore);
                    ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * -tuning.exploreDegrees, yRotationAxis) * negXRotated,
                        true, ref maxScore);
                }
            }

            //Behind the agent (from memory).
            Vector3 XZBack = -eyesForward;

            ScoreExploreDirection(GetOriginPos(), XZBack, false, ref maxScore);
            halfX = (360.0f - eyes.XFOV()) * 0.5f;
            steps = (int)(halfX / tuning.invisibleExploreDegrees);

            for (int i = 1; i <= steps; ++i)
            {
                ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * tuning.invisibleExploreDegrees, yRotationAxis) * XZBack,
                    false, ref maxScore);
                ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * -tuning.invisibleExploreDegrees, yRotationAxis) * XZBack,
                    false, ref maxScore);
            }

            //If no destinations are added to the list,
            //the old target will be used.
            if (navigationState.destList.Count != 0)
                dest = ScoringUtility.PickTarget(navigationState.destList, maxScore);

            //Only recompute goal routing if our new goal is different
            //from the previous goal.
            if (navigationState.currentDest.entity != dest.entity ||
                Vector3.SqrMagnitude(navigationState.currentDest.pos - dest.pos)
                > Constants.Navigation.GOAL_EPSILON_SQR)
            {
                ++navigationState.changeTargetCount;

                navigationState.currentDest = dest;

                float memChanceRoll = Random.Range(0.0f, 1.0f);
                memoryState.onMemPath = false;

                if (memChanceRoll <= memoryState.memPathChance)
                    memoryState.onMemPath = memory.memoryMap.NavigateAStar(
                        GetPosition(), navigationState.currentDest.pos, ref memoryState.memPathWaypoints);

                if (memoryState.onMemPath)
                {
                    navAgent.SetDestination(memoryState.memPathWaypoints[0]);
                    navigationState.pathResolved = false;
                    memoryState.memWaypoint.x = memoryState.memPathWaypoints[0].x;
                    memoryState.memWaypoint.z = memoryState.memPathWaypoints[0].z;
                }
                else navigationState.RouteDestination(this);


                //Once something has been selected as a destination,
                //commit it to long-term memory.
                if (null != navigationState.currentDest.entity)
                    memory.CommitLTM(navigationState.currentDest.entity);
            }

            explorationState.assessedGoalsInit = true;

            if (verboseDebugging)
                NPDebug.LogMessage("Position: " + navAgent.transform.position +
                    ", Destination: " + navigationState.currentDest);
        }
        public PerceivedEntity GetDestinationEntity() => navigationState.currentDest.entity;

        //maxScore is updated if the entity achieves a higher score.
        private void ScoreEntity(EntityMemory memory, ref float maxScore)
        {
            //A previously visited entity shouldn't be targeted.
            //Likewise, an entity found to be unreachable shouldn't be targeted.
            if (memory.visited || memory.unreachable)
                return;

            bool isFinalGoal = memory.entity.entityType == EntityType.ET_GOAL_COMPLETION;

            float bias = 0.0f;

            //Special circumstances for the final goal - since it marks the end of play
            //for a player.
            if (isFinalGoal)
            {
                //If mandatory goals remain, the final goal can't be targeted.
                if (this.memory.MandatoryGoalsLeft() || !explorationState.assessedGoalsInit)
                    return;

                bias += Mathf.Lerp(Constants.Behaviour.FINAL_GOAL_BONUS_MIN,
                    Constants.Behaviour.FINAL_GOAL_BONUS_MAX,
                    heuristics.heuristicScaleLookup[Heuristic.EFFICIENCY]);

                //Penalize for the agent's assessment of benefit for all unvisited
                //positive entities.
                bias -= explorationState.pastCumulativeEntityScore;
            }

            Vector3 toEntity = memory.RecallPos() - GetPosition();

            float distFactor = (toEntity.sqrMagnitude < Constants.Behaviour.DIST_SCORE_FACTOR_SQR) ?
                1.0f : Constants.Behaviour.DIST_SCORE_FACTOR_SQR / toEntity.sqrMagnitude;

            //Weighted scoring function.
            //Bias added to account for entity's type.
            float entityBias = 0.0f;

            foreach (HeuristicScale heuristicScale in heuristics.modifiableHeuristicScales)
            {
                (Heuristic, EntityType) key = (heuristicScale.heuristic, memory.entity.entityType);

                if (!heuristics.entityScoringLookup.ContainsKey(key))
                {
                    NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                    continue;
                }

                entityBias += heuristicScale.scale * heuristics.entityScoringLookup[key]
                    * distFactor;
            }

            bias += entityBias;

            float score = ScoreDirection(GetPosition(), toEntity, bias, toEntity.magnitude);

            //Bias for preferring interactive objects (if they are favourable).
            if (entityBias > 0.0f && score > 0.0f)
                score += Constants.Behaviour.INTERACTIVITY_BIAS;

            if (!isFinalGoal && score > 0.0f)
                explorationState.cumulativeEntityScore += score;

            //Bias for preferring the goal we have already set.
            //(If we haven't already reached it).
            if (memory.entity == navigationState.currentDest.entity
                && Vector3.SqrMagnitude(GetPosition() - navigationState.currentDest.pos)
                > Constants.Navigation.GOAL_EPSILON_SQR)
                score += Constants.Behaviour.EXISTING_GOAL_BIAS;

            //Check if the destination should be added to the candidate list.
            if (score > maxScore
                || (maxScore - score)
                < Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD)
            {
                TargetDest newDest = new TargetDest();
                newDest.score = score;

                //We only need to update the destination position
                //if we're targeting an entity other than the current target.
                if (memory.entity == navigationState.currentDest.entity)
                {
                    newDest.pos = navigationState.currentDest.pos;
                    newDest.accurate = navigationState.currentDest.accurate;
                }
                else
                {
                    //Check for reachability.
                    Vector3 realPos = Vector3.zero;

                    // GABO TODO DEBUG: reachability
                    //bool reachable = PathOSNavUtility.GetClosestPointWalkable(memory.entity.ActualPosition(),
                    //    navAgent.height * PathOS.Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                    //    ref realPos);
                    bool reachable = PathOSNavUtility.CanAgentReachTarget(
                        navAgent,
                        memory.entity.ActualPosition(),
                        navAgent.height * Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                        ref realPos);
                    // FIN DEBUG

                    if (reachable)
                        reachable = Vector3.SqrMagnitude(
                            PathOSNavUtility.XZPos(realPos) -
                            PathOSNavUtility.XZPos(memory.entity.ActualPosition()))
                            < visitThresholdSqr;

                    if (!reachable)
                    {
                        memory.MakeUnreachable();
                        return;
                    }

                    //If the entity is visible/always known to the player, ensure 
                    //its position is set to the actual position of the entity.
                    if (memory.entity.visible || memory.entity.entityRef.alwaysKnown)
                    {
                        newDest.pos = realPos;
                        newDest.accurate = true;
                    }
                    //Otherwise, fetch its position from memory.
                    //(Imperfect recall, done when the decision is made).
                    else
                    {
                        Vector3 guessPos = Vector3.zero;

                        // GABO TODO DEBUG
                        //reachable = PathOSNavUtility.GetClosestPointWalkable(
                        //memory.RecallPos(),
                        //navAgent.height * PathOS.Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                        //ref guessPos);
                        reachable = PathOSNavUtility.CanAgentReachTarget(
                            navAgent,
                            memory.RecallPos(),
                            navAgent.height * Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                            ref guessPos);
                        // FIN DEBUG

                        newDest.pos = (reachable) ? guessPos : realPos;
                        newDest.accurate = !reachable;
                    }
                }

                //Only update maxScore if the new score is actually higher.
                //(Prevent over-accumulation of error.)
                //This will only execute if the destination is reachable.
                if (score > maxScore)
                    maxScore = score;

                newDest.entity = memory.entity;
                navigationState.destList.Add(newDest);
            }
        }

        //maxScore is updated if the direction achieves a higher score.
        void ScoreExploreDirection(Vector3 origin, Vector3 dir, bool visible, ref float maxScore,
            bool overridePos = false, Vector3 overrideDest = default)
        {
            float distance = 0.0f;
            Vector3 newTarget = origin;

            // SEBA: Commenting this prevents the agent from getting stuck on an unreachable target. // Or maybe not...
            //if (overridePos && overrideDest != null)
            //{
            //    newTarget = overrideDest;
            //}
            //else
            {
                if (visible)
                {
                    NavMeshHit hit = new NavMeshHit();
                    //Grab the "extent" of the direction on the navmesh from the perceptual system.
                    switch (eyes.camType)
                    {
                        case PathOSAgentEyes.CamType.FreeMode:
                            hit = eyes.ExploreVisibilityCheckFreeMode(GetEyesPosition(), dir, out bool result);
                            distance = result ? hit.distance : 0;
                            newTarget = result ? hit.position : GetPosition();
                            break;
                        case PathOSAgentEyes.CamType.FirstPerson:
                            hit = eyes.ExploreVisibilityCheck(GetPosition(), dir);
                            distance = hit.distance;
                            newTarget = hit.position;
                            break;
                    }
                    
                }
                else
                {
                    //Grab the "extent" of the direction on our memory model of the navmesh.
                    PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
                    memory.memoryMap.RaycastMemoryMap(origin, dir, eyes.navmeshCastDistance, out hit);
                    distance = hit.distance;

                    // GABO TODO DEBUG
                    //bool reachable = PathOSNavUtility.GetClosestPointWalkable(
                    //    origin + distance * dir, exploreTargetMargin, ref newTarget);
                    bool reachable = PathOSNavUtility.CanAgentReachTarget(
                        navAgent,
                        origin + distance * dir,
                        tuning.exploreTargetMargin,
                        ref newTarget);
                    // FIN DEBUG

                    //Disqualify a target if the agent has determined it to be unreachable.
                    if (!reachable || explorationState.IsUnreachable(newTarget))
                        return;
                }
            }

            float bias = 0.0f;

            //Bias for preferring the goal we have already set.
            //(If we haven't reached it already.)
            bool distanceToTarget = Vector3.SqrMagnitude(
                newTarget - navigationState.currentDest.pos) < Constants.Navigation.GOAL_EPSILON_SQR;
            bool distanceToThrehold = (GetPosition() - navigationState.currentDest.pos).magnitude > tuning.exploreThreshold;

            if (distanceToTarget && distanceToThrehold)
            {
                bias += Constants.Behaviour.EXISTING_GOAL_BIAS;
            }

            float score = ScoreDirection(origin, dir, bias, distance);

            //Same inclusion logic as for entity goals.
            if (score > maxScore
                || (maxScore - score)
                < Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD)
            {
                if (score > maxScore)
                    maxScore = score;

                TargetDest newDest = new TargetDest();
                newDest.score = score;

                //If we're originating from where we stand, target the "end" point.
                //Else, target the "start" point, and the agent will re-assess its 
                //options when it gets there.
                if (Vector3.SqrMagnitude(origin - GetOriginPos())
                    < Constants.Navigation.EXPLORE_PATH_POS_THRESHOLD_FAC
                    * tuning.exploreThreshold)
                    newDest.pos = newTarget;
                else
                {
                    switch (eyes.camType)
                    {
                        case PathOSAgentEyes.CamType.FreeMode:
                            newDest.pos = GetPosition();
                            break;

                        case PathOSAgentEyes.CamType.FirstPerson:
                            newDest.pos = origin;
                            break;
                    }
                }

                newDest.accurate = true;
                newDest.entity = null;
                navigationState.destList.Add(newDest);
            }

            memory.AddPath(new ExploreMemory(origin, dir, newTarget, score));
        }

        float ScoreDirection(Vector3 origin, Vector3 dir, float bias, float maxDistance)
        {
            dir.Normalize();

            //Score base = bias.
            float score = bias;

            //Add to the score based on our curiosity and the potential to 
            //"fill in our map" as we move in this direction.
            //This is similar to the scaling created by assessing an exploration direction.
            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
            memory.memoryMap.RaycastMemoryMap(origin, dir, maxDistance, out hit);

            score += (heuristics.heuristicScaleLookup[Heuristic.CURIOSITY])
                * hit.numUnexplored / PathOSNavUtility.NavmeshMemoryMapper.maxCastSamples
                * hit.distance / eyes.navmeshCastDistance;

            //Enumerate over all entities the agent knows about, and use them
            //to affect our assessment of the potential target.
            for (int i = 0; i < memory.entities.Count; ++i)
            {
                if (memory.entities[i].visited || memory.entities[i].unreachable)
                    continue;

                //Vector to the entity.
                Vector3 entityVec = memory.entities[i].RecallPos() - origin;

                //Scale our factor by inverse square of distance.
                float distFactor = (entityVec.sqrMagnitude < Constants.Behaviour.DIST_SCORE_FACTOR_SQR) ?
                1.0f : Constants.Behaviour.DIST_SCORE_FACTOR_SQR / entityVec.sqrMagnitude;

                Vector3 dir2entity = entityVec.normalized;

                float dot = Vector3.Dot(dir, dir2entity);
                dot = Mathf.Clamp(dot, 0.0f, 1.0f);

                //Weighted scoring function.
                foreach (HeuristicScale heuristicScale in heuristics.modifiableHeuristicScales)
                {
                    (Heuristic, EntityType) key = (heuristicScale.heuristic,
                        memory.entities[i].entity.entityType);

                    if (!heuristics.entityScoringLookup.ContainsKey(key))
                    {
                        NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                        continue;
                    }

                    bias += heuristicScale.scale * heuristics.entityScoringLookup[key] * dot * distFactor;
                }
            }

            return score;
        }

        private void Update()
        {
            //Inactive state toggle for debugging purposes (or if the agent is finished).
            if (freezeAgent || completed)
                return;

            if (timeScale <= 0.0f) timeScale = 1.0f;

            // GABO: Ignoring this line for temporary batch agents, since you're not supposed
            // to control their timeScale in the inspector or when batching ends, while also
            // allowing use of PathOSBatchingWindow's time scale slider which doesn't work
            // properly when this line is set since entering Game Mode calls this object
            // default timeScale for some reason.
            if (!name.Contains("Temporary Batch Agent"))
            {
                Time.timeScale = timeScale;
            }

            healthState.UpdateDeadState();

            //If we've reached our destination, reset the number of times
            //we've "changed our mind" without doing anything.
            if (navigationState.changeTargetCount > 0
                && (Vector3.SqrMagnitude(GetPosition() - navigationState.currentDest.pos) < Constants.Navigation.GOAL_EPSILON_SQR
                    || (navigationState.currentDest.entity != null && memory.Visited(navigationState.currentDest.entity))))
            {
                navigationState.changeTargetCount = 0;

                if (navigationState.currentDest.entity != null)
                {
                healthTuning.CalculateHealth(
                    tuning,
                    healthState, 
                    navigationState.currentDest.entity.entityType);

                    //Updates weights based on the player's health
                    heuristics.UpdateWeightsBasedOnHealth(this);
                }
            }

            //Update spatial memory.
            memory.memoryMap.Fill(navAgent.transform.position);

            //Update of periodic actions.
            navigationState.routeTimer += Time.deltaTime;
            navigationState.perceptionTimer += Time.deltaTime;

            if (!navigationState.lookingAround)
                navigationState.lookTimer += Time.deltaTime;

            //Rerouting update.
            if (navigationState.routeTimer >= RouteComputeTimeCalculated())
            {
                navigationState.routeTimer = 0.0f;

                float rerouteChance = navigationState.changeTargetCount
                    * Constants.Behaviour.GOAL_INDECISION_CHANCE;

                float rerouteRoll = Random.Range(0.0f, 1.0f);

                if (rerouteRoll >= rerouteChance)
                {
                    ComputeNewDestination();
                }
            }

            //Memory path update.
            if (memoryState.onMemPath)
            {
                Vector3 curXZ = GetPosition();
                curXZ.y = 0.0f;

                if (Vector3.SqrMagnitude(curXZ - memoryState.memWaypoint)
                    < Constants.Navigation.WAYPOINT_EPSILON_SQR)
                {
                    memoryState.memPathWaypoints.RemoveAt(0);

                    if (memoryState.memPathWaypoints.Count == 0)
                    {
                        memoryState.onMemPath = false;
                        navigationState.RouteDestination(this);
                    }
                    else
                    {
                        navAgent.SetDestination(memoryState.memPathWaypoints[0]);
                        navigationState.pathResolved = false;
                        memoryState.memWaypoint.x = memoryState.memPathWaypoints[0].x;
                        memoryState.memWaypoint.z = memoryState.memPathWaypoints[0].z;
                    }
                }
            }
            else if (navigationState.DestinationIsInaccurate())
                MakeEntityDestinationAccurate();
            //Debug.LogWarning(!pathResolved + " && " + NavmeshPathIncomplete());
            //Targeting update. This prevents the agent from getting stuck.
            if (navigationState.NavmeshPathIncomplete(this))
            {
                //If we're following a memory path,
                //abort and route to the final target on the Navmesh.
                if (memoryState.onMemPath)
                {
                    memoryState.onMemPath = false;
                    navigationState.RouteDestination(this);
                }
                //If we're dealing with an entity...
                else if (navigationState.currentDest.entity != null)
                {
                    PerceivedEntity entity = navigationState.currentDest.entity;

                    if (!navigationState.currentDest.accurate)
                        MakeEntityDestinationAccurate();

                    else
                    {
                        float adjVisitSqr = (entity.entityRef.overrideVisitRadius) ?
                            entity.entityRef.visitRadiusSqr : visitThresholdSqr;

                        //Compress unreachability check to XZ plane.
                        Vector3 agentPos = GetPosition();
                        agentPos.y = 0.0f;

                        Vector3 targetPos = entity.perceivedPos;
                        targetPos.y = 0.0f;

                        if (Vector3.SqrMagnitude(agentPos - targetPos) >= adjVisitSqr)
                            memory.MakeUnreachable(entity);

                        //Reset the number of times we've changed our mind
                        //without doing anything (since we tried to get here).
                        navigationState.changeTargetCount = 0;
                    }
                }
                //If we're dealing with an exploration target...
                else
                {
                    //This will prevent the agent from retargeting the current destination.
                    explorationState.AddUnreachable(navigationState.currentDest.pos);
                    navigationState.changeTargetCount = 0;
                }

                navigationState.pathResolved = true;
            }

            //Perception update.
            //This will allow the agent's eyes to "process" nearby entities
            //and also update the time threshold for looking around based 
            //on nearby hazards.
            if (navigationState.perceptionTimer >= Constants.Perception.PERCEPTION_COMPUTE_TIME)
            {
                navigationState.perceptionTimer = 0.0f;
                PerceptionUpdate();
            }

            //Look-around update.
            if (navigationState.ShouldLookAround())
            {
                navigationState.lookTimer = 0.0f;
                navigationState.lookingAround = true;
                StartCoroutine(navigationState.LookAround(this));
            }

            //Set the agent's completion flag.
            if (manager.endOnCompletionGoal
                && memory.FinalGoalCompleted())
            {
                completed = true;
                gameObject.SetActive(false);
            }

            //Camera follow update
            if (cameraFollow)
            {
                if (cameraObject != null) cameraObject.transform.position = new Vector3(transform.position.x, 15.0f, transform.position.z);
            }
        }


        private void MakeEntityDestinationAccurate()
        {
            // GABO TODO DEBUG: Reachability
            //bool reachable = PathOSNavUtility.GetClosestPointWalkable(
            //            currentDest.entity.ActualPosition(),
            //            navAgent.height * PathOS.Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
            //            ref currentDest.pos);
            bool reachable = PathOSNavUtility.CanAgentReachTarget(
                    navAgent,
                    navigationState.currentDest.entity.ActualPosition(),
                    navAgent.height * Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                    ref navigationState.currentDest.pos);
            // FIN DEBUG

            if (reachable)
                reachable = Vector3.SqrMagnitude(
                    PathOSNavUtility.XZPos(navigationState.currentDest.pos) -
                    PathOSNavUtility.XZPos(navigationState.currentDest.entity.ActualPosition()))
                    < visitThresholdSqr;

            if (!reachable)
            {
                memory.MakeUnreachable(navigationState.currentDest.entity);
                navigationState.ResetDestinationSelf(this);
            }

            navigationState.currentDest.accurate = true;
            navigationState.RouteDestination(this);
        }

        private void PerceptionUpdate() => navigationState.UpdateLookTime(this);

        public Vector3 GetTargetPosition() => navigationState.currentDest.pos;

        public bool IsTargeted(PerceivedEntity entity) => navigationState.currentDest.entity == entity;

        public float GetHealth() => healthState.health;

        public bool IsDead() => healthState.dead;

        // GABO: Set all unreachable positions (memory entities not included) as possibly reachable again
        public void ResetUnreachablePositionReferences() => explorationState.TryReset();
    }

}

