using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Behaviours
{
    [System.Serializable]
    [RequieredModule(typeof(TileMapModule), typeof(BundleTileMap))]
    public class PopulationTileBehaviour : LBSBehaviour
    {
        #region FIELDS
        private BundleTileMap selectedTilemap;
        #endregion


        #region PROPERTIES
        public BundleTileMap SelectedTilemap
        {
            get => selectedTilemap; 
            set => selectedTilemap = value;
        }

        #endregion

        #region CONSTRUCTORS
        public PopulationTileBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }
        #endregion

        #region METHODS
        
        public override void OnGUI()
        {

        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;

        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
        }

        public override object Clone()
        {
            return new PopulationTileBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override bool Equals(object obj)
        {
            var other = obj as PopulationBehaviour;

            if (other == null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}