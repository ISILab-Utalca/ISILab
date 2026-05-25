
using Codice.Client.Common.GameUI;
using ISILab.LBS.Plugin.Modules.Simulation.PathOSPlus.OGVis.Scripts;
using NinePenguins;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

        private PathOSAgentMemory _agentMemory;
        public PathOSAgentMemory AgentMemory
        { get => _agentMemory is null ? _agentMemory = GetComponent<PathOSAgentMemory>() : _agentMemory; }

        public PathOSAgentEyes eyes { get; private set; }

        public float visitThresholdSqr { get; set; }

        public bool completed { get; set; }

        public float hazardPenalty { get; set; }
        public int ApproximatedFloor => _agentMemory.CalculateAproximatedFloor(GetPosition().y);

        public float MemPathChance
        {
            get => memoryState.memPathChance;
            set => memoryState.memPathChance = value;
        }

        [SerializeField]
        public float ExperienceScale { get => tuning.experienceScale; internal set => tuning.experienceScale = value; }

        #endregion

        #region MONOBEHAVIOUR METHODS
        private void Awake()
        {
            // Get components
            eyes = GetComponent<PathOSAgentEyes>();
            _agentMemory = GetComponent<PathOSAgentMemory>();
            navAgent = GetComponent<NavMeshAgent>();
            heuristics = GetComponent<HeuristicOS>();
            cameraObject = GameObject.FindWithTag("PathOSCamera");

            // Get singleton instances
            manager ??= PathOSManager.instance;
            logger ??= OGLogManager.instance;

            // Set starting position as current destination 
            navigationState.currentDest = new TargetDest() 
            { 
                pos = GetPosition() 
            };

            // Set initial state
            completed = false;

            heuristics.Init(this);
            healthState.Init();
            Debug.Log(healthState.health);

            memoryState.memPathWaypoints = new List<Vector3>();
            explorationState.unreachableReference = new List<Vector3>();
        }

        private void Start()
        {
            LogAgentData(); 
            navigationState.UpdateLookTime(this);

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

        private void Update()
        {
            // Inactive state toggle for debugging purposes (or if the agent is finished).
            if (freezeAgent || completed)
                return;

            ////////////////////// TIME STUFF //////////////////////

            if (timeScale <= 0.0f) timeScale = 1.0f;

            /* GABO: Ignoring this line for temporary batch agents, since you're not supposed
             to control their timeScale in the inspector or when batching ends, while also
             allowing use of PathOSBatchingWindow's time scale slider which doesn't work
             properly when this line is set since entering Game Mode calls this object
             default timeScale for some reason.*/
            if (!name.Contains("Temporary Batch Agent"))
            {
                Time.timeScale = timeScale;
            }

            // Updates timers for periodic updates (rerouting, perception, look-around).
            UpdateTimers();

            ////////////// DESTINATION REACHED CHECKS //////////////

            // Agent arrived to goal destination.
            if (DestinationReached(Constants.Navigation.GOAL_EPSILON_SQR, out bool isEntity))
            {
                // Updates dead condition and interaction with health or enemy entities.
                HealthStateUpdate();

                if(isEntity && 
                    (navigationState.currentDest.entity.entityType == EntityType.ET_STAIR_UP ||
                    navigationState.currentDest.entity.entityType == EntityType.ET_STAIR_DOWN))
                {
                    var otherStair = new PerceivedEntity(navigationState.currentDest.entity.entityRef.OtherStairRef);
                    if(!_agentMemory.Visited(otherStair.entityRef))
                    {
                        navigationState.currentDest = new TargetDest()
                        {
                            entity = otherStair,
                            pos = navigationState.currentDest.entity.entityRef.OtherStairRef.Position,
                            accurate = true
                        };
                        navigationState.RouteDestination(navAgent);
                    }
                }
                else
                {
                    // Computes new destionation.
                    ComputeNewDestination();
                }
            }

            // Agent arrived to a memory path waypoint.
            if(memoryState.onMemPath && 
                DestinationReached(Constants.Navigation.WAYPOINT_EPSILON_SQR, out isEntity, memoryState.memWaypoint))
            {
                memoryState.GoNextWaypoint(navAgent, navigationState);
            }

            // Update visited spatial memory.
            _agentMemory.memoryMap.Fill(navAgent.transform.position);

            /////////////////// REROUTING CHANCE ///////////////////

            // Rerouting update.
            var rerouteTime = 
                Constants.Navigation.ROUTE_COMPUTE_BASE + 
                Constants.Memory.RETRIEVAL_TIME * _agentMemory.entities.Count;

            if (navigationState.routeTimer >= rerouteTime)
            {
                ChanceReroute();
            }

            //////////////// PATH AND ACCURACY FIX /////////////////

            // If destination is an inaccurate entity,
            // make it accurate if it's visible now
            navigationState.FixInaccurateDestination(this);

            // Navmesh targeting update.
            // This prevents the agent from getting stuck.
            if (navigationState.NavmeshPathIncomplete(navAgent))
            {
                FixNavmeshPath();
            }

            ////////////////// PERCEPTION STUFF ///////////////////

            //Perception update.
            //This will allow the agent's eyes to "process" nearby entities
            //and also update the time threshold for looking around based 
            //on nearby hazards.
            if (navigationState.perceptionTimer >= Constants.Perception.PERCEPTION_COMPUTE_TIME)
            {
                navigationState.perceptionTimer = 0.0f;
                navigationState.UpdateLookTime(this);
            }

            //Look-around update.
            if (navigationState.ShouldLookAround())
            {
                navigationState.lookTimer = 0.0f;
                navigationState.lookingAround = true;
                StartCoroutine(navigationState.LookAround(this));
            }

            //Camera follow update
            if (cameraFollow)
            {
                if (cameraObject != null) cameraObject.transform.position = new Vector3(transform.position.x, 15.0f, transform.position.z);
            }

            ///////////////// FINALIZATION CHECK //////////////////

            //Set the agent's completion flag.
            if (manager.endOnCompletionGoal
                && _agentMemory.FinalGoalCompleted())
            {
                completed = true;
                gameObject.SetActive(false);
            }

            #region UPDATE METHODS
            void HealthStateUpdate()
            {
                if (navigationState.changeTargetCount > 0) return;
                navigationState.changeTargetCount = 0;

                if (navigationState.currentDest.entity == null) return;

                healthTuning.CalculateHealth(
                    tuning,
                    healthState,
                    navigationState.currentDest.entity.entityType);
                healthState.UpdateDeadState();

                //Updates weights based on the player's health
                heuristics.UpdateWeightsBasedOnHealth(this);
            }

            bool DestinationReached(float radius, out bool isEntity, Vector3? overrideDestination = null)
            {
                var destination = overrideDestination != null ? 
                    new TargetDest() { pos = overrideDestination.Value } :
                    navigationState.currentDest;

                isEntity = destination.entity != null && destination.entity.entityRef != null;

                //If we've reached our destination, reset the number of times
                //we've "changed our mind" without doing anything.
                var distanceToDest = Vector3.SqrMagnitude(GetPosition() - destination.pos);
                var isDestVisited = isEntity && _agentMemory.Visited(destination.entity);

                return radius >= distanceToDest || isDestVisited;
            }

            void UpdateTimers()
            {
                //Update of periodic actions.
                navigationState.routeTimer += Time.deltaTime;
                navigationState.perceptionTimer += Time.deltaTime;

                if (!navigationState.lookingAround)
                    navigationState.lookTimer += Time.deltaTime;
            }

            void ChanceReroute()
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

            void FixNavmeshPath()
            {
                //If we're following a memory path,
                //abort and route to the final target on the Navmesh.
                if (memoryState.onMemPath)
                {
                    memoryState.onMemPath = false;
                    navigationState.RouteDestination(navAgent);
                }
                //If we're dealing with an entity...
                else if (navigationState.currentDest.entity != null)
                {
                    PerceivedEntity entity = navigationState.currentDest.entity;

                    if (!navigationState.currentDest.accurate)
                    {
                        navigationState.MakeEntityDestinationAccurate(this);
                    }
                    else
                    {
                        var sameFloor = 
                            _agentMemory.CalculateAproximatedFloor(entity.perceivedPos.y) ==
                            _agentMemory.CalculateAproximatedFloor(GetPosition().y);

                        Vector3 agentPos = Vector3.zero;
                        Vector3 targetPos = Vector3.zero;
                        float adjVisitSqr = 0;

                        //Compress unreachability check to XZ plane.
                        if (sameFloor)
                        {
                            adjVisitSqr = (entity.entityRef.overrideVisitRadius) ?
                                entity.entityRef.visitRadiusSqr : visitThresholdSqr;

                            agentPos = PathOSNavUtility.XZPos(GetPosition());
                            targetPos = PathOSNavUtility.XZPos(entity.perceivedPos);
                        }

                        // Make unreachable if it's not on the same floor,
                        // or if it's far away on the same floor.
                        if (!sameFloor || Vector3.SqrMagnitude(agentPos - targetPos) >= adjVisitSqr)
                            _agentMemory.MakeUnreachable(entity);

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
            #endregion
        }
        #endregion

        #region PRIVATE METHODS
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

        /// <summary>
        /// Calculates and selects a new navigation destination for the agent based on current goals, memory, and
        /// exploration heuristics.
        /// </summary>
        /// <remarks>This method evaluates potential destinations by scoring entities, exploration
        /// directions, and known paths, then updates the agent's current destination if a better option is found. If a
        /// new destination is selected, the navigation path is recalculated and the destination entity is committed to
        /// long-term memory. The method is intended to be called internally as part of the agent's navigation update
        /// cycle.</remarks>
        private void ComputeNewDestination()
        {
            // Set current destination as base target.

            TargetDest dest = new TargetDest(navigationState.currentDest);

            // Clear potential destination list,
            // which will be re-populated with scored options.

            navigationState.potentialDests.Clear();

            // Reset scores

            float maxScore = -10000.0f;
            explorationState.pastCumulativeEntityScore = explorationState.cumulativeEntityScore;
            explorationState.cumulativeEntityScore = 0.0f;

            // Get eyes' forward, up and right vectors.

            GetEyesAxes(out Vector3 eyesForward, out Vector3 yRotationAxis, out Vector3 xRotationAxis, eyes.camType);

            // Calculate score for the current goal (if it's entity).
            // (else) Calculate goal distance and visibility for exploration direction scoring.

            EntityMemory currentGoalMemory = null;
            if (navigationState.currentDest.entity != null)
            {
                currentGoalMemory = _agentMemory.GetMemory(navigationState.currentDest.entity);

                if (null == currentGoalMemory)
                {
                    NPDebug.LogError("Something went wrong! Targeting " +
                        navigationState.currentDest.entity.entityRef.objectRef.name +
                        " but it could not be found in agent memory!",
                        typeof(PathOSAgent));
                }
                else ScoreEntity(currentGoalMemory, ref maxScore);
            }
            else if (_agentMemory.CalculateAproximatedFloor(navigationState.currentDest.pos.y) == ApproximatedFloor)
            {
                // ROD: I can't really imagine a way to calculate the score for an exploration direction
                // in a floor the agent isn't on, so let's not calculate it at all! (genius, ik).

                Vector3 goalForward = default;
                Vector3 goalDistance = navigationState.currentDest.pos - GetPosition();

                switch (eyes.camType)
                {
                    case PathOSAgentEyes.CamType.FreeMode:
                        goalForward = navigationState.currentDest.pos - GetEyesPosition();
                        break;

                    case PathOSAgentEyes.CamType.FirstPerson:
                        goalForward = goalDistance;
                        goalForward.y = 0.0f;
                        break;
                }

                if (goalDistance.sqrMagnitude > 0.1f)
                {
                    goalForward.Normalize();
                    float angleToGoal = Vector3.Angle(eyesForward, goalForward);
                    bool goalVisible = Mathf.Abs(angleToGoal) < (eyes.XFOV() * 0.5f); // FP. Generalizar.

                    ScoreExploreDirection(GetOriginPos(), goalForward, goalVisible, ref maxScore,
                        navigationState.currentDest.pos);
                }
            }

            // Calculate score for each entity in memory that isn't the current goal.

            for (int i = 0; i < _agentMemory.entities.Count; ++i)
            {
                if (!ReferenceEquals(currentGoalMemory, _agentMemory.entities[i]))
                    ScoreEntity(_agentMemory.entities[i], ref maxScore);
            }

            // Calculate score for paths' directions in memory
            // Treated as not visible since they are based on the player's "idea" of the space.

            for (int i = 0; i < _agentMemory.paths.Count; ++i)
            {
                ScoreExploreDirection(_agentMemory.paths[i].originPoint,
                    _agentMemory.paths[i].direction,
                    false, ref maxScore);
            }
            int estupidoFlanders = _agentMemory.paths.Count;
            // Explore and score many directions in view, step by step.

            float halfX = eyes.XFOV() * 0.5f;
            int steps = (int)(halfX / tuning.exploreDegrees);

            float halfY = eyes.cam.fieldOfView * 0.5f;
            int stepsY = eyes.camType == PathOSAgentEyes.CamType.FreeMode ? (int)(halfY / tuning.exploreDegrees) : 0;

            for (int j = 0; j <= stepsY; ++j)
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

            // Explore and score directions behind the agent (from memory).

            Vector3 XZBack = -eyesForward;
            ScoreExploreDirection(GetOriginPos(), XZBack, false, ref maxScore);
            halfX = (360.0f - eyes.XFOV()) * 0.5f;
            steps = eyes.camType == PathOSAgentEyes.CamType.FirstPerson ? (int)(halfX / tuning.invisibleExploreDegrees) : 0;

            for (int i = 1; i <= steps; ++i)
            {
                ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * tuning.invisibleExploreDegrees, yRotationAxis) * XZBack,
                    false, ref maxScore);
                ScoreExploreDirection(GetOriginPos(), Quaternion.AngleAxis(i * -tuning.invisibleExploreDegrees, yRotationAxis) * XZBack,
                    false, ref maxScore);
            }

            // Pick a destination from the list, weighted by score.
            // If no destinations were added to the list, the old target will be used.

            if (navigationState.potentialDests.Count != 0)
                dest = ScoringUtility.PickTarget(navigationState.potentialDests, maxScore);

            // Recompute goal if new destination is different from the current one.

            if (navigationState.currentDest.entity != dest.entity ||
                Vector3.SqrMagnitude(navigationState.currentDest.pos - dest.pos)
                > Constants.Navigation.GOAL_EPSILON_SQR)
            {
                ++navigationState.changeTargetCount;

                navigationState.currentDest = dest;

                float memChanceRoll = Random.Range(0.0f, 1.0f);
                memoryState.onMemPath = false;

                if (memChanceRoll <= memoryState.memPathChance)
                    memoryState.onMemPath = _agentMemory.memoryMap.NavigateAStar(
                        GetPosition(), navigationState.currentDest.pos, ref memoryState.memPathWaypoints);

                if (memoryState.onMemPath)
                {
                    navAgent.SetDestination(memoryState.memPathWaypoints[0]);
                    navigationState.pathResolved = false;
                }
                else navigationState.RouteDestination(navAgent);

                // Once an entity has been selected as a destination,
                // commit it to long-term memory.
                if (null != navigationState.currentDest.entity)
                    _agentMemory.CommitLTM(navigationState.currentDest.entity);
            }

            explorationState.assessedGoalsInit = true;

            //if (verboseDebugging)
            {
                string destName =
                    navigationState.currentDest.entity == null ? "Null" : navigationState.currentDest.entity.entityType.ToString();

                var destPos = navigationState.currentDest.pos;
                NPDebug.LogMessage($"Position: {navAgent.transform.position}, Destination: {destName}, Destination Position: ({destPos.x:F2}, {destPos.y:F2}, {destPos.z:F2})");
            }

            // Get eyes' forward, up and right vectors
            // Used in the calculation of exploration directions.
            void GetEyesAxes(out Vector3 eyesForward, out Vector3 yRotationAxis, out Vector3 xRotationAxis, PathOSAgentEyes.CamType eyesType)
            {
                eyesForward = default;
                yRotationAxis = default;
                xRotationAxis = default;

                switch (eyesType)
                {
                    case PathOSAgentEyes.CamType.FreeMode:
                        eyesForward = eyes.cam.transform.forward;
                        yRotationAxis = eyes.cam.transform.up;
                        xRotationAxis = eyes.cam.transform.right;
                        break;

                    case PathOSAgentEyes.CamType.FirstPerson:
                        eyesForward = transform.forward;
                        eyesForward.y = 0.0f;
                        eyesForward.Normalize();
                        yRotationAxis = Vector3.up;
                        xRotationAxis = Quaternion.AngleAxis(90.0f, Vector3.up) * eyesForward;
                        break;
                }
            }
        }

        /// <summary>
        /// Scores an entity based on various biases and updates the maximum score if necessary.
        /// </summary>
        /// <param name="entityMem">The memory of the entity to be scored.</param>
        /// <param name="maxScore">The current maximum score, which may be updated.</param>
        private void ScoreEntity(EntityMemory entityMem, ref float maxScore)
        {
            // Don't proceed if the entity has already been visited or deemed unreachable.

            if (entityMem.visited || entityMem.unreachable)
                return;

            // Calculate if entity is on the same floor, and distance to the entity.

            float distanceToEntity = 0;
            Vector3 entityPosition = entityMem.RecallPos();

            int agentFloor = ApproximatedFloor;
            int entityFloor = _agentMemory.CalculateAproximatedFloor(entityPosition.y);
            int deltaFloor = Mathf.Abs(entityFloor - agentFloor);
            bool sameFloor = entityFloor == agentFloor;

            EntityMemory[] closestStair = new EntityMemory[deltaFloor];
            Vector3?[] stairPos = new Vector3?[deltaFloor];
            Vector3?[] otherStairPos = new Vector3?[deltaFloor];
            bool validStairPath = true;

            // If destination entity is not on the same floor,
            // we must find a path to it using the stairs stored in memory.
            if (!sameFloor)
            {
                validStairPath = CalculateStairPath(
                    ref closestStair, ref stairPos, ref otherStairPos, 
                    agentFloor, entityFloor, out distanceToEntity);

                // If the stair path couldn't be completed, score = 0
                // entity isn't marked as unreachable because the agent might find a stair later.
                if (!validStairPath) return;
            }
            else
            {
                distanceToEntity += Vector3.Distance(GetPosition(), entityPosition);
            }

            // Calculate final goal and entity biases.

            float finalGoalBias = FinalGoalBias(entityMem.entity, out bool isFinalGoal);
            float entityBias = EntityBias(entityMem, distanceToEntity);
            float bias = finalGoalBias + entityBias;

            // Calculate entity direction's score.

            float score = 0;

            // If the entity isn't on the same floor, the total score will be the sum of the scores
            // for each direction between stairs until reaching the entity.
            if (!sameFloor)
            {
                Vector3 auxOrigin = GetPosition();

                for (int i = 0; i < deltaFloor; i++)
                {
                    Vector3 auxDirection = stairPos[i].Value - auxOrigin;

                    score += ScoreDirectionXZ(auxOrigin, auxDirection, 0, auxDirection.magnitude, agentFloor + 0);

                    auxOrigin = otherStairPos[i].Value;
                }

                // Bias is added out of the looop to prevent over-scoring.
                score += bias;
            }
            else
            {
                Vector3 toEntity = entityPosition - GetPosition();
                score += ScoreDirectionXZ(GetPosition(), toEntity, bias, toEntity.magnitude, agentFloor);
            }

            // Bias for preferring interactive objects (if they are favourable).

            if (entityBias > 0.0f && score > 0.0f)
                score += Constants.Behaviour.INTERACTIVITY_BIAS;

            // Accumulate entity score for the exploration system,
            // which will be used to penalize the final goal if the agent
            // has assessed high benefit for many unvisited entities.

            if (!isFinalGoal && score > 0.0f)
                explorationState.cumulativeEntityScore += score;

            // Bias for preferring the goal we have already set
            // (If we haven't already reached it).

            if (entityMem.entity == navigationState.currentDest.entity
                && Vector3.SqrMagnitude(GetPosition() - navigationState.currentDest.pos)
                > Constants.Navigation.GOAL_EPSILON_SQR)
            {
                score += Constants.Behaviour.EXISTING_GOAL_BIAS;
            }

            // Check if the destination should be added to the candidate list.

            if (score > maxScore || 
                (maxScore - score) < Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD)
            {
                TargetDest newDest = new TargetDest();

                // We only need to update the destination position
                // if we're targeting an entity other than the current target.

                if (entityMem.entity == navigationState.currentDest.entity)
                {
                    newDest.pos = navigationState.currentDest.pos;
                    newDest.accurate = navigationState.currentDest.accurate;
                }
                else
                {
                    // Calculate real reachability.

                    Vector3 closestPos = Vector3.zero;

                    bool reachable = PathOSNavUtility.CanAgentReachTarget(
                        navAgent, entityMem.entity.ActualPosition(),
                        navAgent.height * Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                        ref closestPos);

                    if (reachable)
                    {
                        reachable = 
                            Vector3.SqrMagnitude(closestPos - entityMem.entity.ActualPosition()) 
                            < visitThresholdSqr;
                    }

                    if (!reachable)
                    {
                        entityMem.MakeUnreachable();
                        return;
                    }

                    // If the entity is visible/always known to the player, ensure 
                    // its position is set to the actual position of the entity.

                    if (entityMem.entity.visible || entityMem.entity.entityRef.alwaysKnown)
                    {
                        newDest.pos = closestPos;
                        newDest.accurate = true;
                    }

                    // Otherwise, fetch its position from memory.
                    // (Imperfect recall, done when the decision is made).

                    else
                    {
                        // Calculate guessed reachability.

                        Vector3 guessPos = Vector3.zero;

                        reachable = PathOSNavUtility.CanAgentReachTarget(
                            navAgent,
                            entityMem.RecallPos(),
                            navAgent.height * Constants.Navigation.NAV_SEARCH_RADIUS_FAC,
                            ref guessPos);

                        newDest.pos = (reachable) ? guessPos : closestPos;
                        newDest.accurate = !reachable;
                    }
                }

                // Only update maxScore if the new score is actually higher.
                // (Prevent over-accumulation of error.)
                // This will only execute if the destination is reachable.

                if (score > maxScore)
                    maxScore = score;

                // Set entity reference and score for the destination and add it to the candidate list.

                newDest.score = score;
                newDest.entity = entityMem.entity;
                navigationState.potentialDests.Add(newDest);
            }

        }

        private bool CalculateStairPath(
            ref EntityMemory[] closestStair, ref Vector3?[] stairPos, ref Vector3?[] otherStairPos,
            int agentFloor, int entityFloor, out float distanceToEntity)
        {
            distanceToEntity = 0;
            bool validStairPath = true;
            int deltaFloor = Mathf.Abs(entityFloor - agentFloor);

            FillWith(ref closestStair, null);
            FillWith(ref stairPos, null);
            FillWith(ref otherStairPos, null);

            Vector3 auxOrigin = GetPosition();
            int auxFloor = agentFloor;

            for (int i = 0; i < deltaFloor; i++)
            {
                // find next stair
                closestStair[i] = _agentMemory.GetClosestStair(auxOrigin, entityFloor > auxFloor ? 1 : -1);
                if (closestStair[i] == null || closestStair[i].entity.entityRef.OtherStairRef == null)
                {
                    validStairPath = false;
                    break;
                }

                // save stair positions
                stairPos[i] = closestStair[i].entity.perceivedPos;
                otherStairPos[i] = closestStair[i].entity.entityRef.OtherStairRef.Position;

                // add new distance
                distanceToEntity += Vector3.Distance(auxOrigin, stairPos[i].Value);

                // update aux values
                auxOrigin = otherStairPos[i].Value;
                auxFloor += closestStair[i].entity.entityRef.DirectionSign;
            }

            return validStairPath;
        }
        private void FillWith<T>(ref T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        uint estupidoFlanders = 0;
        /// <summary>
        /// Scores and explores a direction based on its potential information gain and various biases, 
        /// and updates the maximum score if necessary.
        /// </summary>
        /// <param name="origin">The starting position for the exploration.</param>
        /// <param name="dir">The direction to explore.</param>
        /// <param name="visible">Indicates whether the direction is visible and stores the exploration in memory.</param>
        /// <param name="maxScore">The current maximum score, which may be updated.</param>
        /// <param name="overrideDest">The position to override the target with, if applicable.</param>
        private void ScoreExploreDirection(Vector3 origin, Vector3 dir, bool visible, ref float maxScore,
            Vector3 overrideDest = default)
        {
            float distance = 0.0f;
            Vector3 newTarget = origin;

            // SEBA: Commenting this prevents the agent from getting stuck on an unreachable target. 
            // Or maybe not...
            if (overrideDest != default)
            {
                newTarget = overrideDest;
            }

            // Calculate the distance and position of the "extent" of the direction on the navmesh,
            // which will be used for scoring and targeting if this direction is selected.

            else
            {
                //Debug.Log("Hola hola vecinillo " + estupidoFlanders++);
                
                // If direction is visibile, explore and map tiles along the direction.
                // Stores the distance to the "extent" of the direction on the navmesh,
                // and the position of that "extent" (or the original position if not reachable).
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

                // If the direction isn't visible, we want to give it the benefit of the doubt
                // and allow the agent to explore towards it, unless the agent has already deemed it unreachable.

                else
                {
                    // Grab the "extent" of the direction on our memory model of the navmesh.
                    PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
                    _agentMemory.memoryMap.XZRaycastMemoryMap(origin, dir, eyes.navmeshCastDistance, out hit);
                    distance = hit.distance;

                    bool reachable = PathOSNavUtility.CanAgentReachTarget(
                        navAgent,
                        origin + distance * dir,
                        tuning.exploreTargetMargin,
                        ref newTarget);

                    //Disqualify a target if the agent has determined it to be unreachable.
                    if (!reachable || explorationState.IsUnreachable(newTarget))
                        return;
                }
            }

            // If for any reason, the new target isn't on the same floor as the agent, disqualify it.
            if (_agentMemory.CalculateAproximatedFloor(newTarget.y) != ApproximatedFloor)
                return;

            //Bias for preferring the goal we have already set.
            //(If we haven't reached it already.)

            float bias = 0.0f;
            bool distanceToTarget = Vector3.SqrMagnitude(
                newTarget - navigationState.currentDest.pos) < Constants.Navigation.GOAL_EPSILON_SQR;
            bool distanceToThreshold = (GetPosition() - navigationState.currentDest.pos).magnitude > tuning.exploreThreshold;

            if (distanceToTarget && distanceToThreshold)
            {
                bias += Constants.Behaviour.EXISTING_GOAL_BIAS;
            }

            // Calculate the score for this direction based on the bias
            // and the potential information gain of exploring in this direction.

            float score = ScoreDirectionXZ(origin, dir, bias, distance, ApproximatedFloor);

            //Same inclusion logic as for entity goals.

            if (score > maxScore
                || (maxScore - score)
                < Constants.Behaviour.SCORE_UNCERTAINTY_THRESHOLD)
            {
                // Store new max score and create new destination.

                if (score > maxScore)
                    maxScore = score;

                TargetDest newDest = new TargetDest()
                {
                    score = score,
                    accurate = true,
                    entity = null
                };

                // If we're originating from where we stand, target the "end" point.
                // Else, target the "start" point, and the agent will re-assess its 
                // options when it gets there.

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

                navigationState.potentialDests.Add(newDest);
            }

            // Add this direction to memory as a potential path,
            // with its score as the "impression" of the path.

            _agentMemory.AddPath(new ExploreMemory(origin, dir, newTarget, score));
        }

        /// <summary>
        /// Calculates the score for exploring in a given direction based on the potential
        /// information gain and various biases.
        /// </summary>
        /// <param name="origin">The starting point of the exploration direction.</param>
        /// <param name="dir">The direction to explore.</param>
        /// <param name="bias">The bias to apply to the score.</param>
        /// <param name="maxDistance">The maximum distance to consider for exploration.</param>
        /// <returns></returns>
        private float ScoreDirectionXZ(Vector3 origin, Vector3 dir, float bias, float maxDistance, int floorIndex)
        {
            // Normalize direction and set bias as base score.
            dir.y = 0;
            dir.Normalize();
            float score = bias;

            // Add to the score based on our curiosity and the potential to 
            // "fill in our map" as we move in this direction.
            // This is similar to the scaling created by assessing an exploration direction.

            PathOSNavUtility.NavmeshMemoryMapper.NavmeshMemoryMapperCastHit hit;
            _agentMemory.memoryMap.XZRaycastMemoryMap(origin, dir, maxDistance, out hit);

            score += (heuristics.heuristicScaleLookup[Heuristic.CURIOSITY])
                * hit.numUnexplored / PathOSNavUtility.NavmeshMemoryMapper.maxCastSamples
                * hit.distance / eyes.navmeshCastDistance;

            // Enumerate over all entities the agent knows about, and use them
            // to affect our assessment of the potential target.

            for (int i = 0; i < _agentMemory.entities.Count; ++i)
            {
                if (_agentMemory.entities[i].visited || _agentMemory.entities[i].unreachable ||
                    _agentMemory.CalculateAproximatedFloor(_agentMemory.entities[i].RecallPos().y) != floorIndex)
                    continue;

                //Vector to the entity.
                Vector3 entityVec = _agentMemory.entities[i].RecallPos() - origin;

                //Scale our factor by inverse square of distance.
                float distFactor = (entityVec.sqrMagnitude < Constants.Behaviour.DIST_SCORE_FACTOR_SQR) ?
                1.0f : Constants.Behaviour.DIST_SCORE_FACTOR_SQR / entityVec.sqrMagnitude;

                entityVec.y = 0;
                Vector3 dir2entity = entityVec.normalized;

                float dot = Vector3.Dot(dir, dir2entity);
                dot = Mathf.Clamp(dot, 0.0f, 1.0f);

                //Weighted scoring function.
                foreach (HeuristicScale heuristicScale in heuristics.modifiableHeuristicScales)
                {
                    (Heuristic, EntityType) key = (heuristicScale.heuristic,
                        _agentMemory.entities[i].entity.entityType);

                    if (!heuristics.entityScoringLookup.ContainsKey(key))
                    {
                        NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                        continue;
                    }

                    score += heuristicScale.scale * heuristics.entityScoringLookup[key] * dot * distFactor;
                }
            }

            return score;
        }
        

        /// <summary>
        /// Calculates the bias for a given entity if it's the final goal.
        /// </summary>
        /// <param name="entity">The perceived entity.</param>
        /// <param name="isFinalGoal">Output parameter indicating if the entity is the final goal.</param>
        /// <returns>The calculated bias for the final goal.</returns>
        private float FinalGoalBias(PerceivedEntity entity, out bool isFinalGoal)
        {
            float bias = 0.0f;
            isFinalGoal = entity.entityType == EntityType.ET_GOAL_COMPLETION;
            if (isFinalGoal)
            {
                //If mandatory goals remain, the final goal can't be targeted.
                if (this._agentMemory.MandatoryGoalsLeft() || !explorationState.assessedGoalsInit)
                    return bias;

                bias += Mathf.Lerp(Constants.Behaviour.FINAL_GOAL_BONUS_MIN,
                    Constants.Behaviour.FINAL_GOAL_BONUS_MAX,
                    heuristics.heuristicScaleLookup[Heuristic.EFFICIENCY]);

                //Penalize for the agent's assessment of benefit for all unvisited
                //positive entities.
                bias -= explorationState.pastCumulativeEntityScore;
            }
            return bias;
        }

        /// <summary>
        /// Calculates the bias for a given entity based on its type and distance.
        /// </summary>
        /// <param name="memory">The memory of the entity.</param>
        /// <param name="toEntity">The vector from the agent to the entity.</param>
        /// <returns>The calculated bias for the entity.</returns>
        private float EntityBias(EntityMemory memory, float distance)
        {
            // Calculate distance to entity and distance factor for scoring function.

            float distFactor = 0.1f;
            float sqrDistance = distance * distance;

            distFactor = sqrDistance < Constants.Behaviour.DIST_SCORE_FACTOR_SQR ?
                1.0f : Constants.Behaviour.DIST_SCORE_FACTOR_SQR / sqrDistance;

            // Calculate bias for entity type and heuristics, scaled by distance.

            float entityBias = 0.0f;
            foreach (HeuristicScale heuristicScale in heuristics.modifiableHeuristicScales)
            {
                (Heuristic, EntityType) key = (heuristicScale.heuristic, memory.entity.entityType);

                if (!heuristics.entityScoringLookup.ContainsKey(key))
                {
                    NPDebug.LogError("Couldn't find key " + key.ToString() + " in heuristic scoring lookup!", typeof(PathOSAgent));
                    continue;
                }

                entityBias += heuristicScale.scale * heuristics.entityScoringLookup[key] * distFactor;
            }
            return entityBias;
        }
        #endregion

        #region PUBLIC METHODS
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

        public Vector3 GetPosition() => navAgent.transform.position;

        public Vector3 GetEyesPosition() => eyes.cam.transform.position;

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
            if (cameraObject == null) return;
            cameraObject.transform.position = new Vector3(transform.position.x, 15.0f, transform.position.z);
        }
        
        public void ToggleCameraFollow() => cameraFollow = !cameraFollow;
        
        public PerceivedEntity GetDestinationEntity() => navigationState.currentDest.entity;

        public Vector3 GetTargetPosition() => navigationState.currentDest.pos;

        public bool IsTargeted(PerceivedEntity entity) => navigationState.currentDest.entity == entity;

        public float GetHealth() => healthState.health;

        public bool IsDead() => healthState.dead;

        // GABO: Set all unreachable positions (memory entities not included) as possibly reachable again
        public void ResetUnreachablePositionReferences() => explorationState.TryReset();

        #endregion

    }

}

