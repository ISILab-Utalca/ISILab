using ISILab.LBS.Plugin.Components.Behaviours;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    
    /// <summary>
    /// Interior <-> Population
    /// A Keys use these addon to unlock a single connection door
    /// </summary>
    [Serializable]
    public class Addon_SingleUnlock : BundleTileMapAddons
    {
        // to identify the lbs generated interior component in a door
        [SerializeField]
        DirConnection dirConnection; 

        public DirConnection DirConnection { get; set; }
        public Addon_SingleUnlock() { }
    }

    /// <summary>
    /// Triggers may have multiple unlocks assigned to their activation types
    /// </summary>
    [Serializable]
    public class Addon_TriggerUnlock : BundleTileMapAddons
    {
        // to identify the lbs generated interior component in a door
        [SerializeField]
        (TriggerActivationMode, List<DirConnection>) OnEnterUnlocks;
        [SerializeField]
        (TriggerActivationMode, List<DirConnection>) OnExitUnlocks;

        public DirConnection DirConnection { get; set; }

        public Addon_TriggerUnlock() { }
    }
}
