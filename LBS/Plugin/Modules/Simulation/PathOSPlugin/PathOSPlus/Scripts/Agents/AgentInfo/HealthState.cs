using UnityEngine;


namespace PathOS
{
    [System.Serializable]
    public class HealthState
    {
        public float health;
        public bool dead;

        public int cautionIndex;
        public int aggressionIndex;
        public int adrenalineIndex;

        public void UpdateDeadState()
        {
            if (health <= 0 && !dead) dead = true;
        }
    }
}
