using System.Collections.Generic;
using UnityEngine;

namespace PathOS
{
    [System.Serializable]
    public class ExplorationState
    {
        public float cumulativeEntityScore;
        public float pastCumulativeEntityScore;
        public bool assessedGoalsInit;

        public List<Vector3> unreachableReference;

        internal bool IsUnreachable(Vector3 target)
        {
            for (int i = 0; i < unreachableReference.Count; ++i)
            {
                if (Vector3.SqrMagnitude(target - unreachableReference[i])
                    < PathOS.Constants.Navigation.UNREACHABLE_POS_CHECK_SQR) // 16
                    return true;
            }

            return false;
        }

        internal void TryReset()
        {
            if (unreachableReference.Count > 0) unreachableReference.Clear();
        }

        internal void AddUnreachable(Vector3 target)
        {
            for (int i = 0; i < unreachableReference.Count; ++i)
            {
                if (Vector3.SqrMagnitude(target - unreachableReference[i])
                    < Constants.Navigation.UNREACHABLE_POS_SIMILARITY_SQR)
                    return;
            }

            unreachableReference.Add(target);
        }


    }
}
