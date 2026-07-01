using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    /// <summary>
    /// <b>Characteristic Rules</b> allow for characteristics to influence on the generation process for tilemaps based on bundles.
    /// </summary>
    [System.Serializable]
    public abstract class LBSCharacteristicRule
    {
        LBSCharacteristic owner;
        private GeneratedBundle toGenerate;
        /// <summary>
        /// A numerical value that decides the order the rules will apply their effects in.
        /// </summary>
        public int priority;

        public LBSCharacteristicRule(LBSCharacteristic owner)
        {
            this.owner = owner;
        }

        #region PROPERTIES
        /// <summary>
        /// References the characteristic necessary to work.
        /// </summary>
        public LBSCharacteristic Owner => owner;
        /// <summary>
        /// The characteristic's bundle for easy access.
        /// </summary>
        public Bundle OwnerBundle => owner.Owner;
        public GeneratedBundle ToGenerate
        {
            get => ToGenerate;
            set => ToGenerate = value;
        }

        #endregion

        /// <summary>
        /// The <b>Characteristic Rule's</b> method to sort the tilemap to their needs, then modify accordingly.
        /// </summary>
        /// <returns>An ordered list of LBS Tiles in which to work on.</returns>
        public virtual IOrderedEnumerable<LBSTile> Sort(List<LBSTile> tiles)
        {
            return null;
        }

        /// <summary>
        /// The <b>Characteristic Rule's</b> method to assign bundles to each particular LBS Tile available.
        /// </summary>
        /// <returns>A dictionary containing every LBS Tile and their assigned Bundle.</returns>
        public virtual Dictionary<LBSTile, Bundle> AssignBundle(List<LBSTile> tiles)
        {
            return null;
        }

        /// <summary>
        /// Assigns a GameObject to generate to each LBS Tile.
        /// </summary>
        /// <returns>A dictionary containing every LBS Tile and their assigned GameObject.</returns>
        public virtual Dictionary<LBSTile, GameObject> AssignGameObject(List<LBSTile> tiles)
        {
            return null;
        }
    }

    public class GeneratedBundle
    {
        private LBSTile tile;
        private Bundle bundle;
        private GameObject assignedObject;

        #region PROPERTIES
        public LBSTile Tile => tile;
        public Bundle Bundle => bundle;
        public GameObject AssignedObject => assignedObject;
        #endregion

        public GeneratedBundle()
        {

        }
        public GeneratedBundle(LBSTile tile, Bundle bundle, GameObject assign)
        {
            this.tile = tile;
            this.bundle = bundle;
            this.assignedObject = assign;
        }
    }
}