using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathOS
{
    public class HeuristicOS : MonoBehaviour
    {
        [HideInInspector] public List<HeuristicScale> heuristicScales = new();
        [HideInInspector] internal List<HeuristicScale> modifiableHeuristicScales = new();

        internal Dictionary<Heuristic, float> heuristicScaleLookup = new();
        internal Dictionary<(Heuristic, EntityType), float> entityScoringLookup = new();

        private static readonly Heuristic[] DefaultHeuristics =
        {
            Heuristic.CURIOSITY,
            Heuristic.ACHIEVEMENT,
            Heuristic.COMPLETION,
            Heuristic.AGGRESSION,
            Heuristic.ADRENALINE,
            Heuristic.CAUTION,
            Heuristic.EFFICIENCY
        };

        private void InitializeHeuristics()
        {
            heuristicScales.Clear();
            modifiableHeuristicScales.Clear();
            heuristicScaleLookup.Clear();
            entityScoringLookup.Clear();

            float baseValue = 0f;

            foreach (var h in DefaultHeuristics)
            {
                var scale = new HeuristicScale(h, baseValue);
                heuristicScales.Add(scale);
                modifiableHeuristicScales.Add(scale);
                heuristicScaleLookup[h] = scale.scale;

                baseValue += 5f;
            }
        }

        internal void Init(PathOSAgent agent)
        {
            InitializeHeuristics();
            CacheHealthIndices(agent);
            BuildEntityScoringLookup();
            ComputeHazardPenalty(agent);
            ApplyMemoryAndNavigation(agent);
        }

        private void CacheHealthIndices(PathOSAgent agent)
        {
            for (int i = 0; i < modifiableHeuristicScales.Count; i++)
            {
                switch (modifiableHeuristicScales[i].heuristic)
                {
                    case Heuristic.CAUTION:
                        agent.healthState.cautionIndex = i;
                        break;
                    case Heuristic.AGGRESSION:
                        agent.healthState.aggressionIndex = i;
                        break;
                    case Heuristic.ADRENALINE:
                        agent.healthState.adrenalineIndex = i;
                        break;
                }
            }
        }

        private void BuildEntityScoringLookup()
        {
            foreach (var set in PathOSAgent.manager.heuristicWeights)
            {
                foreach (var w in set.weights)
                {
                    entityScoringLookup[(set.heuristic, w.entype)] = w.weight;
                }
            }
        }

        private float GetEntityScore(Heuristic h, EntityType t)
        {
            return entityScoringLookup.TryGetValue((h, t), out var v) ? v : 0f;
        }

        private float GetHeuristicValue(Heuristic h)
        {
            return heuristicScaleLookup.TryGetValue(h, out var v) ? v : 0f;
        }

        private void ComputeHazardPenalty(PathOSAgent agent)
        {
            float avgAggression = AverageHazardScore(Heuristic.AGGRESSION);
            float avgAdrenaline = AverageHazardScore(Heuristic.ADRENALINE);
            float avgCaution = AverageHazardScore(Heuristic.CAUTION);

            float hazardScore =
                GetHeuristicValue(Heuristic.AGGRESSION) * avgAggression +
                GetHeuristicValue(Heuristic.ADRENALINE) * avgAdrenaline +
                GetHeuristicValue(Heuristic.CAUTION) * avgCaution;

            agent.hazardPenalty = -hazardScore;
        }

        private float AverageHazardScore(Heuristic h)
        {
            return 0.2f * (
                GetEntityScore(h, EntityType.ET_HAZARD_ENEMY_LOW) +
                GetEntityScore(h, EntityType.ET_HAZARD_ENEMY_MED) +
                GetEntityScore(h, EntityType.ET_HAZARD_ENEMY_HIGH) +
                GetEntityScore(h, EntityType.ET_HAZARD_ENEMY_BOSS) +
                GetEntityScore(h, EntityType.ET_HAZARD_ENVIRONMENT)
            );
        }

        private void ApplyMemoryAndNavigation(PathOSAgent agent)
        {
            float exp = agent.tuning.experienceScale;

            agent.visitThresholdSqr = agent.tuning.visitThreshold * agent.tuning.visitThreshold;

            agent.STMemoryState.forgetTime = Mathf.Lerp(
                Constants.Memory.FORGET_TIME_MIN,
                Constants.Memory.FORGET_TIME_MAX,
                exp);

            agent.STMemoryState.stmSize = Mathf.RoundToInt(Mathf.Lerp(
                Constants.Memory.MEM_CAPACITY_MIN,
                Constants.Memory.MEM_CAPACITY_MAX,
                exp));

            agent.navigationState.baseLookTime = Mathf.Lerp(
                Constants.Behaviour.LOOK_TIME_MIN_EXPLORE,
                Constants.Behaviour.LOOK_TIME_MAX,
                GetHeuristicValue(Heuristic.CURIOSITY));

            float memPathScale =
                (GetHeuristicValue(Heuristic.CAUTION) +
                 1f - GetHeuristicValue(Heuristic.CURIOSITY)) * 0.5f;

            agent.STMemoryState.memPathChance = Mathf.Lerp(
                Constants.Behaviour.MEMORY_NAV_CHANCE_MIN,
                Constants.Behaviour.MEMORY_NAV_CHANCE_MAX,
                memPathScale);
        }

        public void UpdateWeightsBasedOnHealth(PathOSAgent agent)
        {
            var hs = agent.healthState;
            float h = 1.0f - (hs.health / 100.0f);

            UpdateHeuristic(hs.cautionIndex, Heuristic.CAUTION,
                Mathf.Lerp(GetHeuristicValue(Heuristic.CAUTION), 1f, h));

            UpdateHeuristic(hs.aggressionIndex, Heuristic.AGGRESSION,
                Mathf.Lerp(GetHeuristicValue(Heuristic.AGGRESSION),
                    GetHeuristicValue(Heuristic.AGGRESSION) * 0.25f, h));

            UpdateHeuristic(hs.adrenalineIndex, Heuristic.ADRENALINE,
                Mathf.Lerp(GetHeuristicValue(Heuristic.ADRENALINE),
                    GetHeuristicValue(Heuristic.ADRENALINE) * 0.25f, h));
        }

        private void UpdateHeuristic(int index, Heuristic h, float value)
        {
            value = Mathf.Clamp01(value);
            modifiableHeuristicScales[index].scale = value;
            heuristicScaleLookup[h] = value; // keep lookup in sync
        }
    }
}
