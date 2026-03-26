using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Interact : BundleTileMapAddons
    {
        [SerializeReference]
        private LBSEventHooker interact = new();

        public LBSEventHooker Interact { get => interact; set => interact = value; }

        public override object Clone()
        {
            Addon_Interact clone = new Addon_Interact();
            clone.Interact = interact.Clone() as LBSEventHooker;
            return clone;
        }
    }
}