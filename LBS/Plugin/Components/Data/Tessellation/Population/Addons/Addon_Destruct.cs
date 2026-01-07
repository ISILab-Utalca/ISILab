using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Destruct : BundleTileMapAddons
    {
        [SerializeReference]
        private LBSEventHooker destroyed = new();

        public LBSEventHooker Destroyed { get => destroyed; set => destroyed = value; }
    }
}
