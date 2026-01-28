using System.Collections.Generic;
using UnityEngine;

namespace PathOS
{

    [System.Serializable]
    public class NavigationState
    {
        public float routeTimer;
        public float perceptionTimer;

        public float baseLookTime = PathOS.Constants.Behaviour.LOOK_TIME_MAX;
        public float lookTime = PathOS.Constants.Behaviour.LOOK_TIME_MAX;
        public float lookTimer;

        public bool lookingAround;

        public TargetDest currentDest;
        public List<TargetDest> destList = new();
        public bool pathResolved = true;

        public int changeTargetCount;

        public bool DestinationIsInaccurate()
            => currentDest.entity != null &&
            !currentDest.accurate &&
            currentDest.entity.visible;

        public bool ShouldLookAround() => lookTimer >= lookTime;
    }

}