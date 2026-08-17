using ISILab.LBS.Modules;
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
    /// The replacement for LBSDirectionedGroup characteristic, allowing to define chances based on each direction
    /// of the tile.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristicAttribute("Directioned Chance", "Define chances based on direction")]

    public class LBSDirectionedChance : LBSCharacteristic, ICloneable
    {
        [System.Serializable]
        public class TileDirectionChance
        {
            [SerializeField]
            public Bundle target;

            [SerializeField]
            public int rotation;

            [Range(0f, 1f)]
            public float chance;

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

        [System.Serializable]
        public class TileDirection
        {
            [SerializeField]
            public Bundle mainTarget;

            [SerializeField]
            public int rotation;

            [SerializeField]
            public List<NestedList<TileDirectionChance>> chances = new List<NestedList<TileDirectionChance>>(4);

            public List<string> Connections => mainTarget.GetCharacteristics<LBSDirection>()[0].GetConnection().ToList();
        }

        //This list holds the different tile directions and their chances. Imagine for each tile placed in the map,
        //it has 4 possible directions (right, up, left, down), and for each direction, there are different bundles that can be placed.
        [SerializeField]
        public List<TileDirection> tileDirections = new List<TileDirection>();

        [SerializeField]
        public ConnectedTileType currentType = ConnectedTileType.EdgeBased;

        [SerializeField, Range(0f, 1f)]
        public float maxLimit = 1f;

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

            tileDirections.OrderBy(td => td.mainTarget.BundleName).ThenBy(td => td.rotation);
        }

        public override object Clone()
        {
            var childs = Owner.ChildsBundles;
            return new LBSDirectionedChance();
        }

        public static List<TileDirection> DeepCopy(List<TileDirection> original)
        {
            List<TileDirection> copy = new(original.Select(td =>
            {
                return new TileDirection()
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
                };
            }));

            return copy;
        }

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


