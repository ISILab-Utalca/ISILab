using System.Collections.Generic;
using UnityEngine;

namespace PathOS
{
    [System.Serializable]
    public class MemoryState
    {
        public float memPathChance;
        public bool onMemPath;

        public List<Vector3> memPathWaypoints;
        public Vector3 memWaypoint;

        //How quickly does the agent forget something in its memory?
        //This is for testing right now, basically just a flat value.
        public float forgetTime { get; set; }
        public int stmSize { get; set; }
    }
}
