using System;
using System.Collections.Generic;
using UnityEngine;


namespace PathOS
{
    
    public class HeuristicOS : MonoBehaviour
    {
        [SerializeField]
        public List<HeuristicScale> heuristicScales, modifiableHeuristicScales = new();
        public Dictionary<Heuristic, float> heuristicScaleLookup = new();
        public Dictionary<(Heuristic, EntityType), float> entityScoringLookup = new();

        private void Awake()
        {
            heuristicScaleLookup ??= new Dictionary<Heuristic, float>();
            entityScoringLookup ??= new Dictionary<(Heuristic, EntityType), float>();

            modifiableHeuristicScales.Clear();

            foreach (HeuristicScale curScale in heuristicScales)
            {
                modifiableHeuristicScales.Add(curScale);
                heuristicScaleLookup.Add(curScale.heuristic, curScale.scale);
            }


        }

        internal void Init(PathOSAgent agent)
        {

            //TODO: Clean this up...
            for (int i = 0; i < modifiableHeuristicScales.Count; i++)
            {
                if (modifiableHeuristicScales[i].heuristic == Heuristic.CAUTION)
                {
                    agent.healthState.cautionIndex = i;
                }
                else if (modifiableHeuristicScales[i].heuristic == Heuristic.AGGRESSION)
                {
                    agent.healthState.aggressionIndex = i;
                }
                else if (modifiableHeuristicScales[i].heuristic == Heuristic.ADRENALINE)
                {
                    agent.healthState.adrenalineIndex = i;
                }
            }

            foreach (HeuristicWeightSet curSet in PathOSAgent.manager.heuristicWeights)
            {
                for (int j = 0; j < curSet.weights.Count; ++j)
                {
                    entityScoringLookup.Add((curSet.heuristic, curSet.weights[j].entype), curSet.weights[j].weight);
                }
            }

            float avgAggressionScore = 0.2f *
            (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_LOW)] +
            (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_MED)]) +
            (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_HIGH)]) +
            (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_BOSS)]) +
            entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENVIRONMENT)]);

            float avgAdrenalineScore = 0.2f
                * (entityScoringLookup[(Heuristic.ADRENALINE, EntityType.ET_HAZARD_ENEMY_LOW)] +
                  (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_MED)]) +
                  (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_HIGH)]) +
                  (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_BOSS)]) +
                  entityScoringLookup[(Heuristic.ADRENALINE, EntityType.ET_HAZARD_ENVIRONMENT)]);

            float avgCautionScore = 0.2f
                * (entityScoringLookup[(Heuristic.CAUTION, EntityType.ET_HAZARD_ENEMY_LOW)] +
                (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_MED)]) +
                (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_HIGH)]) +
                (entityScoringLookup[(Heuristic.AGGRESSION, EntityType.ET_HAZARD_ENEMY_BOSS)]) +
                entityScoringLookup[(Heuristic.CAUTION, EntityType.ET_HAZARD_ENVIRONMENT)]);

            float hazardScore = heuristicScaleLookup[Heuristic.AGGRESSION] * avgAggressionScore
                + heuristicScaleLookup[Heuristic.ADRENALINE] * avgAdrenalineScore
                + heuristicScaleLookup[Heuristic.CAUTION] * avgCautionScore;

            agent.hazardPenalty = -hazardScore;

            float threhold = agent.tuning.visitThreshold;
            agent.visitThresholdSqr = threhold * threhold;

            //Duration of working memory for game entities is scaled by experience level.
            agent.memoryState.forgetTime = Mathf.Lerp(PathOS.Constants.Memory.FORGET_TIME_MIN,
                PathOS.Constants.Memory.FORGET_TIME_MAX,
                agent.tuning.experienceScale);

            //Capacitiy of working memory is also scaled by experience level.
            agent.memoryState.stmSize = Mathf.RoundToInt(Mathf.Lerp(PathOS.Constants.Memory.MEM_CAPACITY_MIN,
                PathOS.Constants.Memory.MEM_CAPACITY_MAX,
                agent.tuning.experienceScale));

            //Base look time is scaled by curiosity.
            agent.navigationState.baseLookTime = Mathf.Lerp(PathOS.Constants.Behaviour.LOOK_TIME_MIN_EXPLORE,
                PathOS.Constants.Behaviour.LOOK_TIME_MAX,
                heuristicScaleLookup[Heuristic.CURIOSITY]);


            float memPathScale = (heuristicScaleLookup[Heuristic.CAUTION]
                + 1.0f - heuristicScaleLookup[Heuristic.CURIOSITY])
                * 0.5f;


            agent.memoryState.memPathChance = Mathf.Lerp(PathOS.Constants.Behaviour.MEMORY_NAV_CHANCE_MIN,
                PathOS.Constants.Behaviour.MEMORY_NAV_CHANCE_MAX,
                memPathScale);
        }
    }
}
