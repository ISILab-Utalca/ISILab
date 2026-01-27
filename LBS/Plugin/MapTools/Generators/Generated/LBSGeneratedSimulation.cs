using PathOS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedSimulation : LBSGenerated
    {
        [HideInInspector]
        public PathOSAgent agent;
        [HideInInspector]
        public LevelEntity levelEntity;
        [HideInInspector]
        public bool hideAtStart = false;

        private void Start()
        {
            if(Application.isPlaying)
                if (hideAtStart)
                    DeactivateEntity();
        }

        private void DeactivateEntity()
        {
            agent.eyes.AddInvisibleEntity(levelEntity);
        }

        public void ReactivateEntity()
        {
            agent.eyes.RemoveInvisibleEntity(levelEntity);
        }
    }
}

