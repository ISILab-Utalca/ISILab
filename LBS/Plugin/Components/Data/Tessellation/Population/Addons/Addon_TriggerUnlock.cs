using ISILab.LBS.Plugin.Components.Behaviours;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_TriggerUnlock : BundleTileMapAddons
    {
        // triggers may unlock multiple connections at one
        [SerializeReference]
        (TriggerActivationMode, List<Addon_Unlock>) connections;

        public (TriggerActivationMode, List<Addon_Unlock>) Connections 
        {
            get => connections; 
            set => connections = value; 
        }

        public Addon_TriggerUnlock() { }


    }
}
