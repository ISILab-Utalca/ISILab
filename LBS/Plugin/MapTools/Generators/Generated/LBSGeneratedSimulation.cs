using PathOS;
using System;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedSimulation : LBSGenerated
    {
        [HideInInspector]
        public PathOSAgent agent;
        //[HideInInspector]
        public LevelEntity levelEntity;
        [HideInInspector]
        public bool hideAtStart = false;

        public bool Visible => !agent.eyes.invisibleEntities.Contains(levelEntity);

        private void Start()
        {
            if(Application.isPlaying)
                if (hideAtStart)
                    DeactivateEntity();
        }

        private void DeactivateEntity()
        {
            if (agent.eyes is null) { Debug.LogWarning("Agent eyes component is missing."); return; }
            agent.eyes.AddInvisibleEntity(levelEntity);
        }

        public void ReactivateEntity()
        {
            if (agent.eyes is null) { Debug.LogWarning("Agent eyes component is missing."); return; }
            agent.eyes.RemoveInvisibleEntity(levelEntity);
            PerceivedEntity perceivedEt = agent.eyes.perceptionInfo.Find(et => et.entityRef.Equals(levelEntity));
            if (perceivedEt == null)
                return;
            perceivedEt.perceivedPos = transform.position;
            // These two lines probably have the same effect as agent.memory.entities.Add(new EntityMemory(perceivedEt)), but just in case.
            agent.GetMemory().Memorize(perceivedEt);
            agent.GetMemory().TryCommitLTM(perceivedEt);

            // Possible improvement: Add to memory only if location is registered in the heatmap
        }
    }
}

