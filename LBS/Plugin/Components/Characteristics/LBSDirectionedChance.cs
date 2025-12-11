using System;
using System.Collections.Generic;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using static ISILab.LBS.Modules.ConnectedTileMapModule;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// The replacement for LBSDirectionedGroup characteristic, allowing to define chances based on each direction
    /// of the tile.
    /// </summary>
    [System.Serializable]
    [LBSCharacteristicAttribute("Directioned Chance", "Define chances based on direction")]

    public class LBSDirectionedChance : LBSCharacteristic, ICloneable
    {
        [SerializeField]
        public class TileDirectionChance
        {
            [SerializeField]
            public Bundle target;

            [SerializeField]
            public int rotation;

            [Range(0f, 1f)]
            public float chance;
        }

        [SerializeField]
        public class TileDirection
        {
            [SerializeField]
            public Bundle mainTarget;

            [SerializeField]
            public int rotation;

            [SerializeField]
            public List<List<TileDirectionChance>> chances = new List<List<TileDirectionChance>>(4);
        }

        //This list holds the different tile directions and their chances. Imagine for each tile placed in the map,
        //it has 4 possible directions (right, up, left, down), and for each direction, there are different bundles that can be placed.
        [SerializeField]
        public List<TileDirection> tileDirections = new List<TileDirection>();

        [SerializeField]
        public ConnectedTileType currentType = ConnectedTileType.EdgeBased;

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

        }

        public override object Clone()
        {
            var childs = Owner.ChildsBundles;
            return new LBSDirectionedGroup();
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


