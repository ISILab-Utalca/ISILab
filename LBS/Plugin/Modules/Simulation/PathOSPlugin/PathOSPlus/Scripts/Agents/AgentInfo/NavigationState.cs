using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace PathOS
{

    [System.Serializable]
    public class NavigationState
    {
        public float routeTimer;
        public float perceptionTimer;

        public float baseLookTime = Constants.Behaviour.LOOK_TIME_MAX;
        public float lookTime = Constants.Behaviour.LOOK_TIME_MAX;
        public float lookTimer;

        public bool lookingAround;

        public TargetDest currentDest;
        public List<TargetDest> potentialDests = new();
        public bool pathResolved = true;

        public int changeTargetCount;

        /// <summary>
        /// A destination is accurate when the agent has a clear line of sight to it,
        /// and is not relying on memory or other information to navigate to it.
        /// If a destination is inaccurate, the agent may need to look around or take
        /// other actions to confirm its location before proceeding.
        /// </summary>
        /// <returns></returns>
        public bool DestinationIsInaccurate()
            => currentDest.entity != null &&
            !currentDest.accurate &&
            currentDest.entity.visible;

        public bool ShouldLookAround() => lookTimer >= lookTime;


        internal void UpdateLookTime(PathOSAgent agent)
        {
            lookTime = baseLookTime;

            //Actual look time can fluctuate based on the agent's caution and the 
            //danger in the current area.
            float lookTimeScale = agent.AgentMemory.ScoreHazards(agent.GetPosition()) *
                agent.heuristics.heuristicScaleLookup[Heuristic.CAUTION];

            float a = baseLookTime;

            float max = Constants.Behaviour.LOOK_TIME_MAX;
            float min = Constants.Behaviour.LOOK_TIME_MIN_CAUTION;
            float alpha = lookTimeScale;

            float b = Mathf.Lerp(max, min, alpha);
            lookTime = Mathf.Min(a, b);
        }

        internal bool NavmeshPathIncomplete(NavMeshAgent navAgent) => 
                    !navAgent.pathPending &&
                    !navAgent.hasPath &&
                    navAgent.pathStatus == NavMeshPathStatus.PathPartial &&
                    !navAgent.isPathStale &&
                    !pathResolved;

        internal void ResetDestinationSelf(PathOSAgent agent)
        {
            currentDest.pos = agent.GetPosition();
            currentDest.entity = null;
            currentDest.accurate = true;
        }

        internal IEnumerator LookAround(PathOSAgent agent)
        {
            var navAgent = agent.navAgent;
            navAgent.isStopped = true;
            navAgent.updateRotation = false;

            //Simple 90-degree sweep centred on current heading.
            Quaternion home = agent.transform.rotation;
            Quaternion right = Quaternion.AngleAxis(agent.tuning.lookDegrees, Vector3.up) * home;
            Quaternion left = Quaternion.AngleAxis(-agent.tuning.lookDegrees, Vector3.up) * home;

            float lookingTime = 0.5f;
            float lookingTimer = 0.0f;

            while (lookingTimer < lookingTime)
            {
                agent.transform.rotation = Quaternion.Slerp(home, right, lookingTimer / lookingTime);
                lookingTimer += Time.deltaTime;
                yield return null;
            }

            lookingTimer = 0.0f;

            while (lookingTimer < lookingTime)
            {
                lookingTimer += Time.deltaTime;
                yield return null;
            }

            lookingTimer = 0.0f;

            while (lookingTimer < lookingTime)
            {
                agent.transform.rotation = Quaternion.Slerp(right, left, lookingTimer / lookingTime);
                lookingTimer += Time.deltaTime;
                yield return null;
            }

            lookingTimer = 0.0f;

            while (lookingTimer < lookingTime)
            {
                lookingTimer += Time.deltaTime;
                yield return null;
            }

            lookingTimer = 0.0f;

            while (lookingTimer < lookingTime)
            {
                agent.transform.rotation = Quaternion.Slerp(left, home, lookingTimer / lookingTime);
                lookingTimer += Time.deltaTime;
                yield return null;
            }

            lookingTimer = 0.0f;
            lookingAround = false;

            navAgent.updateRotation = true;
            navAgent.isStopped = false;
        }

        internal void RouteDestination(NavMeshAgent navAgent)
        {
            pathResolved = false;
            navAgent.SetDestination(currentDest.pos);
        }

    }

}