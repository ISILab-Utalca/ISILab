using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.MapTools.Generators;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators {
    public class TerrainGridCharacteristicRule : LBSCharacteristicRule
    {
        public TerrainGridCharacteristicRule(LBSCharacteristic owner) : base(owner)
        {
            priority = 2;
        }

        public override IOrderedEnumerable<LBSTile> Sort(List<LBSTile> tiles)
        {
            var reorderedTiles = tiles.OrderByDescending(c => new bool[] {
            (tiles.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x - 1, c.Position.y))) == null),
            (tiles.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x + 1, c.Position.y))) == null),
            (tiles.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x, c.Position.y + 1))) == null),
            (tiles.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x, c.Position.y - 1))) == null)
            }.Count(t => t));

            return reorderedTiles;
        }

        public override Dictionary<LBSTile, GameObject> AssignGameObject(List<LBSTile> tiles)
        {
            throw new System.NotImplementedException();
        }

    }
}