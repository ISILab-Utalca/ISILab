using System;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Components
{

    [Serializable]
    public class BundleTarget
    {
        [SerializeField] private string guid = string.Empty;
        public string GUID => guid;

        public BundleTarget(Bundle bundle)
        {
            if (bundle == null) return;
            guid = LBSAssetMacro.GetGuidFromAsset(bundle);
        }

        public BundleTarget(TileBundleGroup bundle)
        {
            guid = bundle?.GetGuid() ?? string.Empty;
        }

        public virtual bool IsValid() => !string.IsNullOrEmpty(guid);

        public override bool Equals(object obj) => obj is BundleTarget other && other.guid == guid;
        public override int GetHashCode() => HashCode.Combine(guid);

        // quick BundleTarget target = someTileBundle;
        public static implicit operator BundleTarget(TileBundleGroup b) => new(b);
    }

    /// <summary>
    /// Saves the bundle guid and the position in the graph to get in the scene
    /// </summary>
    /// 
    [Serializable]
    public class BundleTargetGraph : BundleTarget
    {
        [SerializeField, Commons.Attributes.ReadOnlyIncludeChildren] 
        private LBSLayer layer;
        [SerializeReference, Commons.Attributes.ReadOnlyIncludeChildren] 
        private TileBundleGroup tileBundleGroup;


        public LBSLayer Layer => layer;
        public TileBundleGroup TileBundleGroup => tileBundleGroup;

        public BundleTargetGraph(LBSLayer layer, TileBundleGroup bundle)
            : base(bundle)
        {
            this.layer = layer;
            this.tileBundleGroup = bundle;

            if (tileBundleGroup != null)
                tileBundleGroup.OnRemoved += RemoveBundle;
        }

        private void RemoveBundle()
        {
            if (tileBundleGroup != null) tileBundleGroup.OnRemoved -= RemoveBundle;
            tileBundleGroup = null;
        }

        public Vector2Int Position => tileBundleGroup != null ?
            new Vector2Int((int)tileBundleGroup.AreaRect.x, (int)tileBundleGroup.AreaRect.y) :
            Vector2Int.zero;

        public Rect Area => tileBundleGroup?.AreaRect ?? Rect.zero;
        public override bool IsValid() => base.IsValid() && layer != null;

    }

}