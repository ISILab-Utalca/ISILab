using UnityEngine;

namespace PathOS
{
    [System.Serializable]
    public class AgentOS
    {
        [Range(1f, 8f)] public float timeScale = 1f;
        public bool freezeAgent;

        [Range(0f, 1f)] public float experienceScale;

        [Tooltip("How close (in units) does the agent have to get to a goal to mark it as visited?")]
        public float visitThreshold = 1f;

        [Tooltip("Degrees between LOS checks for explorability")]
        public float exploreDegrees = 5f;

        [Tooltip("Degrees between behind-the-back explorability checks")]
        public float invisibleExploreDegrees = 30f;

        [Tooltip("Agent sway when looking around")]
        public float lookDegrees = 60f;

        [Tooltip("Distance for considering two explore goals identical")]
        public float exploreThreshold = 2f;

        [Tooltip("Search radius when picking explore targets")]
        public float exploreTargetMargin = 25f;
    }
}
