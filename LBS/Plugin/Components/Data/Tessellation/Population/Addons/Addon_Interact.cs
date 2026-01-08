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
    }
}