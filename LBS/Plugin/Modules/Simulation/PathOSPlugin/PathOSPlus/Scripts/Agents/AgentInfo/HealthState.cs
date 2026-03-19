using UnityEngine;


namespace PathOS
{
    [System.Serializable]
    public class HealthState
    {
        public float health = 100.0f;
        public bool dead = false;

        public int cautionIndex;
        public int aggressionIndex;
        public int adrenalineIndex;

        public void Init()
        {
            health = 100.0f;
            dead = false;
        }

        public void UpdateDeadState()
        {
            if (health <= 0 && !dead) dead = true;
        }
    }
}
