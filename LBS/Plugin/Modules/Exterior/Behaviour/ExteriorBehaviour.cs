using ISILab.DevTools.Macros;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.Modules.ConnectedTileMapModule;

namespace ISILab.LBS.Behaviours
{
    [Serializable]
    [RequieredModule(typeof(TileMapModule),
                    typeof(ConnectedTileMapModule))]
    public class ExteriorBehaviour : LBSBehaviour, IBlueprintable
    {
        #region CONSTANTS
        private const string defaultBundleGuid = "9d3dac0f9a486fd47866f815b4fefc29";
        #endregion

        #region FIELDS

        [SerializeField]
        private string bundleGuid;
        private Bundle bundle;

        private ConnectedTileType? gridType = null;
        #endregion

        #region META-FIELDS
        [HideInInspector]
        public LBSTag identifierToSet;

        #endregion

        #region PROPERTIES
        [JsonIgnore]
        private TileMapModule TileMap => OwnerLayer.GetModule<TileMapModule>();

        [JsonIgnore]
        private ConnectedTileMapModule Connections => OwnerLayer.GetModule<ConnectedTileMapModule>();

        public Bundle Bundle
        {
            get
            {
                if (bundle != null) return bundle;

                Bundle = AssetMacro.LoadAssetByGuid<Bundle>(bundleGuid)
                      ?? AssetMacro.LoadAssetByGuid<Bundle>(defaultBundleGuid);

                return bundle;
            }
            set
            {
                bundle = value;
                bundleGuid = AssetMacro.GetGuidFromAsset(value);
            }
        }

        public ConnectedTileType GridType
        {
            get
            {
                if (!gridType.HasValue)
                {
                    gridType = Connections.GridType;
                }

                return gridType.Value;
            }
        }

        public List<string> NavigableTags
        {
            get
            {
                var ret = new List<string>();
                Bundle bundle = Bundle;
                if(!bundle)
                {
                    Debug.LogError($"Could not get a bundle for this behaviour.");
                    return ret;
                }
                var navTagsChars = bundle.GetCharacteristics<LBSNavigableTags>();
                if (navTagsChars.Count == 0)
                {
                    Debug.LogError($"Bundle {bundle.BundleName} does not have any LBSNavigableTags characteristic.");
                    return ret;
                }
                navTagsChars[0].SetTags();
                ret.AddRange(navTagsChars[0].GetNavigableTags());
                return ret;
            }
        }

        [JsonIgnore]
        public List<LBSTile> Tiles => TileMap.Tiles;

        [JsonIgnore]
        public List<Vector2Int> Directions => Commons.Directions.Bidimencional.Edges;
        #endregion

        #region CONSTRUCTORS

        public ExteriorBehaviour(string iconGUID, string name, Color colorTint) : base(iconGUID, name, colorTint)
        {

        }

        #endregion

        #region METHODS

        /// Method invoked from the LBSLayer Class, whenever the scriptable object's values are modified
        /// call to reassign bundle from seralized guid or default
        public sealed override void OnGUI() {var bundle = Bundle;}

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
            layer.OnChange += UpdateKeys;
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;
        }
        public override void CheckKeys()
        {
            UpdateKeys(Tiles.ToList<object>());
        }

        public void UpdateKeys()
        {
            UpdateKeys(Tiles.ToList<object>());
        }

        public LBSTile GetTile(Vector2Int pos)
        {
            return TileMap.GetTile(pos);
        }

        public void RemoveTile(LBSTile tile)
        {
            RequestTileRemove(tile);
            OwnerLayer.GetModule<TileMapModule>().RemoveTile(tile);
            OwnerLayer.GetModule<ConnectedTileMapModule>().RemoveTile(tile);
        }

        public void AddTile(LBSTile tile)
        {
            RequestTilePaint(tile);
            
            OwnerLayer.GetModule<TileMapModule>()
                .AddTile(tile);

            OwnerLayer.GetModule<ConnectedTileMapModule>()
                .AddPair(tile, new List<string> { "", "", "", "" }, new List<bool> { false, false, false, false });
        }

        public void SetConnection(LBSTile tile, int direction, string connection, bool canEditedByAI)
        {
            RequestTilePaint(tile);
            var t = OwnerLayer.GetModule<ConnectedTileMapModule>().GetPair(tile);
            t.SetConnection(direction, connection, canEditedByAI);
        }

        public List<string> GetConnections(LBSTile tile)
        {
            return Connections.GetConnections(tile);
        }

        public void RequestTilesRepaint(IEnumerable<LBSTile> tiles)
        {
            foreach (LBSTile tile in tiles)
                RequestTilePaint(tile);
        }

        public override object Clone()
        {
            var clone = new ExteriorBehaviour(IconGuid, Name, ColorTint);
            clone.bundleGuid = bundleGuid;
            return clone; 
        }

        public override bool Equals(object obj)
        {
            var other = obj as ExteriorBehaviour;

            if (other == null) return false;
            
            //if (!GetBundleRef().Equals(other.GetBundleRef())) return false;
            if (!Equals(Bundle, other.Bundle)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool CaptureAreaData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            List<LBSTile> tilesToRemove = TileMap.Tiles;
            List<TileConnectionsPair> connectionsToRemove = Connections.Pairs;

            for (int x = corners.min.x; x <= corners.max.x; x++)
            {
                for (int y = corners.min.y; y <= corners.max.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // Tile
                    LBSTile tile = GetTile(pos);
                    tilesToRemove.Remove(tile);

                     // Connection
                    TileConnectionsPair pair = Connections.GetPair(pos);
                    connectionsToRemove.Remove(pair);
                }
            }

            foreach(var tile in tilesToRemove) TileMap.RemoveTile(tile);
            foreach(var pair in connectionsToRemove) Connections.RemoveTile(pair.Tile);

            return TileMap.TileCount > 0|| Connections.Pairs.Count > 0;
        }

        public void SetPosition(Vector2Int parentAnchor, Vector2Int delta)
        {
 
            foreach (var tile in TileMap.Tiles)
            {
                Vector2Int distanceToAnchor = tile.Position - parentAnchor;
                tile.Position = delta + distanceToAnchor;
            }
        }

        public Vector2Int GetAnchor()
        {
            Vector2Int anchor = new Vector2Int(int.MaxValue, int.MinValue);

            foreach (var tile in TileMap.Tiles)
            {
                if (tile.Position.x < anchor.x) anchor.x = tile.Position.x;
                if (tile.Position.y > anchor.y) anchor.y = tile.Position.y;
            }
            return anchor;
        }

        public override void ChangeLevelRender(int prevLevelIndex, int nextLevelIndex) { }
        public bool MergeLayerData(object incoming, bool overwrite)
        {
            ExteriorBehaviour merger = incoming as ExteriorBehaviour;
            if (merger == null) return false;

            for (int i = 0; i < merger.TileMap.Tiles.Count; i++)
            {
                var incomingTile = merger.TileMap.Tiles[i];

                var originalTile = TileMap.GetTile(incomingTile.Position);

                if (originalTile == null)
                {
                    TileMap.AddTile(incomingTile.Clone() as LBSTile);
                }
                else if (overwrite)
                {
                    originalTile = incomingTile.Clone() as LBSTile;
                }
            }

            for (int i = 0; i < merger.Connections.Pairs.Count; i++)
            {
                var incomingConnectionPair = merger.Connections.Pairs[i];

                var originalConnectionPair = Connections.GetPair(incomingConnectionPair.Tile);

                if (originalConnectionPair == null)
                {
                    var NewPair = incomingConnectionPair.Clone() as TileConnectionsPair;
                    Connections.AddPair(NewPair.Tile, NewPair.Connections, NewPair.EditedByIA);
                }
                else if (overwrite)
                {
                    originalConnectionPair = incomingConnectionPair.Clone() as TileConnectionsPair;
                }
            }

            return true;
        }

        #endregion
    }
}