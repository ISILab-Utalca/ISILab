using System;
using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Components
{
    
    [Serializable]
    public abstract class LayerTarget
    {
        [SerializeReference][SerializeField]protected LBSLayer layer;
        
        public LBSLayer GetLayer() => layer;
        
        public string GetLayerName()
        {
            return layer?.Name ?? "";
        }

        public abstract string GetGuid();
        public abstract bool Valid();
        
        public override bool Equals(object obj)
        {
            LayerTarget other =  obj as LayerTarget;
            return other?.GetGuid() == GetGuid();
        }

    }
    
    /// <summary>
    /// Saves the bundle guid and the position in the graph to get in the scene
    /// </summary>
    /// 
    [Serializable]
    public class BundleGraph : LayerTarget
    {
        [SerializeReference] [SerializeField] private TileBundleGroup tileBundle;
        [SerializeField] private string guid = string.Empty;
        [SerializeField] private QuestActionData _actionData;
        // must be assigned on all bundleGraphs to the Resize Function
        
        public BundleGraph(QuestActionData actionData, LBSLayer layer = null, TileBundleGroup tileBundle = null)
        {
            this.layer = layer;
            
            this.tileBundle = tileBundle;
            _actionData = actionData;
            
            if(this.tileBundle is null) return;
            guid = this.tileBundle.GetGuid();
            
            if(_actionData is null) return;
            this.tileBundle!.OnRemoved += ClearTileBundle;
        }

        private void ClearTileBundle()
        {
            tileBundle = null;
        }

        public Vector2Int Position => new((int)Area.x, (int)Area.y);
        public Rect Area
        {
            get
            {   
                if(tileBundle is null) return Rect.zero;
                return tileBundle.AreaRect;
            }
        }

        public override bool Valid() => GetGuid() != string.Empty;
        public override string GetGuid()
        {
            return guid;
        }
        
    }
    
    
    /// <summary>
    /// Saves the bundle type
    /// </summary>
    [Serializable]
    public class BundleType : LayerTarget
    {
        [SerializeField]private string guid = string.Empty;
        
        public BundleType(
            LBSLayer layer = null, 
            TileBundleGroup tileBundle = null)
        {
            this.layer = layer;
            if (tileBundle != null) guid = tileBundle.GetGuid();
        }

        public override string GetGuid()
        {
            return guid;
        }

        public override bool Valid()
        {
            return GetGuid()!= string.Empty;
        }
        
    }
}