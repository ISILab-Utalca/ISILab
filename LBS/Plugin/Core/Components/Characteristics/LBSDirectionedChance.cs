using ISILab.Commons.Utility;
using ISILab.LBS.Plugin.Components.Bundles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.Modules.ConnectedTileMapModule;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Meant to register a pool of tiles from a Main Exterior <see cref="Bundle"/> and the neighbourhood probabilities for each of them.
    /// The tiles and its neighbourhood probabilities are consulted when running the <see cref="Plugin.Core.AI.Assistant.AssistantWFC"/>.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristicAttribute("Directioned Chance", "Define chances based on direction")]

    public class LBSDirectionedChance : LBSCharacteristic, ICloneable
    {
        /// <summary>
        /// Represents a tile that could be a valid neighbour of another tile.
        /// </summary>
        [System.Serializable]
        public class TileDirectionChance
        {
            /// <summary>
            /// The target bundle defining a neighbour tile.
            /// </summary>
            [SerializeField]
            public Bundle target;

            /// <summary>
            /// The rotation of the neighbour tile, where each unit represents +90 degrees.
            /// </summary>
            [SerializeField]
            public int rotation;

            /// <summary>
            /// The weighted probability this tile has to convert into a definitive neighbour tile.
            /// </summary>
            [Range(0f, 1f)]
            public float chance;

            /// <summary>
            /// Non-rotated connection labels of the target bundle.
            /// </summary>
            public List<string> Connections => target.GetCharacteristics<LBSDirection>()[0].GetConnection().ToList();

            public override bool Equals(object obj)
            {
                if(obj is not TileDirectionChance other) return false;
                return Equals(target, other.target) && rotation == other.rotation;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(target, rotation);
            }
        }

        /// <summary>
        /// Represents a tile and its possible neighbours at each direction.
        /// </summary>
        [System.Serializable]
        public class TileDirection
        {
            /// <summary>
            /// The target bundle defining a tile.
            /// </summary>
            [SerializeField]
            public Bundle mainTarget;

            /// <summary>
            /// The rotation of the tile, where each unit represents +90 degrees.
            /// </summary>
            [SerializeField]
            public int rotation;

            /// <summary>
            /// Lists of possible neighbours for each direction (Right, Up, Left, Down).
            /// </summary>
            [SerializeField]
            public List<NestedList<TileDirectionChance>> chances = new List<NestedList<TileDirectionChance>>(4);

            /// <summary>
            /// Non-rotated connection labels of the target bundle.
            /// </summary>
            public List<string> Connections => mainTarget.GetCharacteristics<LBSDirection>()[0].GetConnection().ToList();
        }

        /// <summary>
        /// Tiles registered and its neighbourhood probabilities.<br/>
        /// Imagine for each tile placed in the map, it has 4 possible directions (right, up, left, down), and for each direction, there are different tiles that can be placed as neighbours.
        /// </summary>
        [SerializeField]
        public List<TileDirection> tileDirections = new List<TileDirection>();

        /// <summary>
        /// What type of grid is this bundle for.
        /// </summary>
        [SerializeField]
        public ConnectedTileType currentType = ConnectedTileType.EdgeBased;

        /// <summary>
        /// Utility that clamps probabilities of every <see cref="TileDirectionChance"/>. Modifying this value does not permanently overwrite the probabilities unless they are manually changed.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        public float maxLimit = 1f;

        /// <summary>
        /// Indicates whether a <see cref="Bundle"/> with 'Empty' tagged connections exists in this Main Bundle or not.
        /// </summary>
        [JsonIgnore]
        public bool UsesEmpties
        {
            get => null != tileDirections.Find(td =>
            {
                var dir = td.mainTarget.GetCharacteristics<LBSDirection>();
                if (dir.Count == 0) return false;
                return dir[0].Connections.Contains("Empty");
            });
        }

        public override void OnEnable()
        {
            //Owner.ClearEvents();
            //Owner.OnAddChild += OnAddChildToOwner;
            //Owner.OnRemoveChild += OnRemoveChildToOwner;

            //_Update();
        }

        /// <summary>
        /// Reinitializes <see cref="tileDirections"/> with the current children bundles.
        /// </summary>
        public void _Update()
        {
            if (Owner == null)
                return;

            tileDirections.Clear();

            var bundles = Owner.ChildsBundles;

            while (bundles.Count < tileDirections.Count)
            {
                for (int i = 0; i < tileDirections.Count; i++)
                {
                    if (!bundles.Equals(tileDirections[i].mainTarget))
                    {
                        tileDirections.RemoveAt(i);
                        break;
                    }
                }
            }

            for (int i = 0; i < bundles.Count; i++)
            {
                if (i == tileDirections.Count)
                    tileDirections.Add(new TileDirection() { mainTarget = bundles[i] });

                if (bundles[i] != null && !bundles[i].Equals(tileDirections[i].mainTarget))
                {
                    tileDirections[i].mainTarget = bundles[i];
                }
            }
        }

        public override object Clone()
        {
            var childs = Owner.ChildsBundles;
            return new LBSDirectionedChance();
        }

        /// <summary>
        /// Creates a deep copy of a list of <see cref="TileDirection"/>.
        /// </summary>
        /// <param name="original">The list to be copied.</param>
        /// <returns>A safe to use deep copy of the list.</returns>
        public static List<TileDirection> DeepCopy(List<TileDirection> original)
        {
            List<TileDirection> copy = new(original.Select(td => new TileDirection()
            {
                mainTarget = td.mainTarget,
                rotation = td.rotation,
                chances = new(td.chances.Select(nested => new NestedList<TileDirectionChance>()
                {
                    list = new(nested.list.Select(tdc => new TileDirectionChance()
                    {
                        target = tdc.target,
                        rotation = tdc.rotation,
                        chance = tdc.chance
                    }))
                }))
            }));

            return copy;
        }

        /// <summary>
        /// Gets a <see cref="LBSDirection"/> characteristic from every <see cref="TileDirection"/> stored.
        /// </summary>
        /// <returns>A list of <see cref="LBSDirection"/> characteristics.</returns>
        public List<LBSDirection> GetDirs()
        {
            var r = new List<LBSDirection>();
            foreach (var td in tileDirections)
            {
                r.Add(td.mainTarget.GetCharacteristics<LBSDirection>()[0]);
            }
            return r;
        }

        public override bool Equals(object obj)
        {
            return false; // TODO: implement this method
        }

        public override List<string> Validate()
        {
            //throw new System.NotImplementedException();
            return  new List<string>();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
