using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Drop : BundleTileMapAddons
    {
        [SerializeField]
        private string onDestroyDropGuid;

        public Bundle OnDestroyDrop 
        {
            get => LBSAssetMacro.LoadAssetByGuid<Bundle>(onDestroyDropGuid); 
            set => onDestroyDropGuid = LBSAssetMacro.GetGuidFromAsset(value); 
        }

        public override object Clone()
        {
            Addon_Drop clone = new Addon_Drop();
            clone.onDestroyDropGuid = onDestroyDropGuid;
            return clone;
        }
    }
}
