using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace ISILab.LBS.Behaviours
{
    [System.Serializable]
    [RequieredModule(typeof(TileMapModule), typeof(BundleTileMap))]
    public class TileGroupBehavior : LBSBehaviour
    {
        #region FIELDS
        private TileBundleGroup selectedTilemap;
        #endregion

        #region PROPERTIES
        public TileBundleGroup SelectedTilemap
        {
            get => selectedTilemap; 
            set 
            {
                selectedTilemap = value;
                OnSelectedChanged?.Invoke(selectedTilemap);
            }
        }

        public Action<TileBundleGroup> OnSelectedChanged;

        #endregion

        #region CONSTRUCTORS
        public TileGroupBehavior(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }
        #endregion

        #region METHODS
        
        public override void OnGUI()
        {

        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
            OwnerLayer.OnChange += () =>
            {
             //   SelectedTilemap = null;
            };
         
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
        }

        public override object Clone()
        {
            return new TileGroupBehavior(this.IconGuid, this.Name, this.ColorTint);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TileGroupBehavior;

            if (other == null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void CheckKeys() { }


        #endregion
    }
}