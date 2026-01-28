using UnityEngine;

namespace PathOS
{

    [System.Serializable]
    public class MemoryOS
    {
        public float forgetTime;
        public int stmSize;

        public float baseMemPathChance = Constants.Behaviour.BASE_MEMORY_NAV_CHANCE;
    }
}
