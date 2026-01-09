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
        [SerializeField]
        List<TriggerUnlockEntry> connections = new();

        public List<TriggerUnlockEntry> Connections 
        {
            get => connections; 
            set => connections = value; 
        }

        public Addon_TriggerUnlock() { }


    }

    [Serializable]
    public class TriggerUnlockEntry
    {
        public TriggerActivationMode Mode = TriggerActivationMode.OnEnter;
        public List<Addon_Unlock> Unlocks = new();

        public TriggerUnlockEntry() { }
    }

}
