using OGVis;
using UnityEngine;

namespace PathOS
{
    [System.Serializable]
    public class HealthOS
    {
        public float maxHealth = 100f;

        public TimeRange lowEnemyDamage = new(10, 30);
        public TimeRange medEnemyDamage = new(30, 50);
        public TimeRange highEnemyDamage = new(50, 70);
        public TimeRange bossEnemyDamage = new(70, 100);

        public TimeRange hazardDamage = new(10, 20);

        public TimeRange lowHealthGain = new(10, 30);
        public TimeRange medHealthGain = new(30, 60);
        public TimeRange highHealthGain = new(70, 100);

        //Get damage values
        private float GetEnemyDamage(float min, float max, AgentOS tuning)
        {
            float experienceAdjustment = 1.0f - tuning.experienceScale;
            experienceAdjustment = experienceAdjustment <= 0 ? 0.1f : experienceAdjustment;

            return Random.Range(
                min * experienceAdjustment,
                max * experienceAdjustment);
        }

        private float GetHealthGain(float min, float max)
        {
            return Random.Range(min, max);
        }

        //Computes the player health when interacting with enemies or resources
        //Needs to be improved/edited
        internal void CalculateHealth(AgentOS tuning, HealthState healthState, EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.ET_HAZARD_ENEMY_LOW:
                    healthState.health -= GetEnemyDamage(lowEnemyDamage.min, lowEnemyDamage.max, tuning);
                    break;
                case EntityType.ET_HAZARD_ENEMY_MED:
                    healthState.health -= GetEnemyDamage(medEnemyDamage.min, medEnemyDamage.max, tuning);
                    break;
                case EntityType.ET_HAZARD_ENEMY_HIGH:
                    healthState.health -= GetEnemyDamage(highEnemyDamage.min, highEnemyDamage.max, tuning);
                    break;
                case EntityType.ET_HAZARD_ENEMY_BOSS:
                    healthState.health -= GetEnemyDamage(bossEnemyDamage.min, bossEnemyDamage.max, tuning);
                    break;
                case EntityType.ET_HAZARD_ENVIRONMENT:
                    healthState.health -= GetEnemyDamage(hazardDamage.min, hazardDamage.max, tuning);
                    break;
                case EntityType.ET_RESOURCE_PRESERVATION_LOW:
                    healthState.health += GetHealthGain(lowHealthGain.min, lowHealthGain.max);
                    break;
                case EntityType.ET_RESOURCE_PRESERVATION_MED:
                    healthState.health += GetHealthGain(medHealthGain.min, medHealthGain.max);
                    break;
                case EntityType.ET_RESOURCE_PRESERVATION_HIGH:
                    healthState.health += GetHealthGain(highHealthGain.min, highHealthGain.max);
                    break;
                default:
                    break;
            }

        }

    }
}
