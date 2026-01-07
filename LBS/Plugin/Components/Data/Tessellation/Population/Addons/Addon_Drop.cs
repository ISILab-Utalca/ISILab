using ISILab.LBS.Plugin.Components.Bundles;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Drop : BundleTileMapAddons
    {
        [SerializeField, SerializeReference]
        private Bundle onDestroyDrop;
        public Bundle OnDestroyDrop 
        {
            get => onDestroyDrop; 
            set => onDestroyDrop = value; 
        }
    }
}
