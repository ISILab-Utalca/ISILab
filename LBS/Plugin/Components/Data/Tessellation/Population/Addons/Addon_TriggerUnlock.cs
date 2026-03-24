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

        public override object Clone()
        {
            Addon_TriggerUnlock clone = new Addon_TriggerUnlock();
            foreach(var tue in Connections)
            {
                clone.Connections.Add(tue.Clone() as TriggerUnlockEntry);
            }
            return clone;
        }
    }

    [Serializable]
    public class TriggerUnlockEntry : ICloneable
    {
        public TriggerActivationMode ActivationMode = TriggerActivationMode.OnEnter;
        public List<Addon_Unlock> Unlocks = new();

        public TriggerUnlockEntry() { }

        public object Clone()
        {
            TriggerUnlockEntry clone = new TriggerUnlockEntry();
            clone.ActivationMode = ActivationMode;
            foreach(var unlock in Unlocks)
            {
                clone.Unlocks.Add(unlock.Clone() as Addon_Unlock);
            }
            return clone;

        }
    }

}
