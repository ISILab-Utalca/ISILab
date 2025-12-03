using ISILab.Extensions;
using ISILab.LBS.Modules;
using LBS.Bundles;
using LBS.Components;
using LBS.Components.TileMap;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using ISILab.DevTools.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.Serialization;

namespace ISILab.LBS.Behaviours
{
    [System.Serializable]
    [RequieredModule(typeof(TileMapModule), typeof(BundleTileMap))]
    public class PopulationBehaviour : LBSBehaviour
    {
        #region FIELDS
        [SerializeField, JsonIgnore] 
        private TileMapModule tileMap;
        [SerializeField, JsonIgnore] 
        private BundleTileMap _bundleTileMap;

        [SerializeField,JsonRequired]
        private string bundleRefGui = "3e607c0f80297b849a6ea0d7f98c73a3";

        [SerializeField, JsonRequired]
        private string bundleRefGuid = "668e6768d7619b3459df4f6378dfa3bb";
        private string DefaultBundleRefGuid { get => "668e6768d7619b3459df4f6378dfa3bb"; }

        private HashSet<TileBundleGroup> _newRotations = new ();
        #endregion

        #region META-FIELDS
        [JsonIgnore]
        public Bundle selectedToSet;
        
        [SerializeField, JsonIgnore]
        private BundleCollection bundleCollection;

        [SerializeField, JsonIgnore]
        private Bundle mainBundle;

        public string allFilter = "All";
        
        [FormerlySerializedAs("selectedTypetoSet")] [JsonIgnore]
        public string selectedTypeFilter;
        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public List<TileBundleGroup> Tilemap => _bundleTileMap is not null ? _bundleTileMap.Groups : new List<TileBundleGroup>();

        [JsonIgnore]
        public BundleTileMap BundleTilemap => _bundleTileMap;
        
        public BundleCollection BundleCollection 
        {
            get => GetBundleCollection();
            set
            {
                bundleCollection = value;
                bundleRefGui = AssetMacro.GetGuidFromAsset(value);
            }
        }

        public Bundle MainBundle
        {
            get => GetMainBundle();
            set
            {
                mainBundle = value;
                bundleRefGuid = AssetMacro.GetGuidFromAsset(value);
            }
        }
        
        public string SelectedFilter 
        {
            get => GetFilter();
            set => selectedTypeFilter = value;
        }

        private string GetFilter()
        {
            return selectedTypeFilter ?? allFilter;
        }

        #endregion

        #region CONSTRUCTORS
        public PopulationBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }
        #endregion

        #region METHODS
        
        public override void OnGUI()
        {

        }

        public TileBundleGroup AddTileGroup(Vector2Int position, Bundle bundle) 
        {
            return AddTileGroup(position, new BundleData(bundle)); }
        public TileBundleGroup AddTileGroup(Vector2Int position, BundleData bundleData) 
        {

            if (!_bundleTileMap.ValidNewGroup(position, bundleData, Vector2.right)) return null;

            //Create group
            TileBundleGroup group = _bundleTileMap.CreateGroup(position, bundleData, Vector2.right);
            RequestTilePaint(group);
            
            //Add all tiles from the group
            foreach(LBSTile tile in group.TileGroup)
            {
                tileMap.AddTile(tile);
            }

            return group;
        }

        public void AddTileGroup(Vector2Int position, TileBundleGroup expired)
        {
            if (expired is null) return;
            if (!_bundleTileMap.ValidNewGroup(position, expired.BundleData, Vector2.right)) return;

            TileBundleGroup group = expired.Clone() as TileBundleGroup;
            group.Translate(position - expired.TileGroup[0].Position);
            _bundleTileMap.AddGroup(group);
            RequestTilePaint(group);

            //Add all tiles from the group
            foreach (LBSTile tile in group.TileGroup)
            {
                tileMap.AddTile(tile);
            }
        }

        public bool ValidMoveGroup(Vector2Int position, TileBundleGroup group)
        {
            //RequestTilePaint(group);
            return _bundleTileMap.ValidMoveGroup(position, group, Vector2.right);
        }

        public void MoveGroup(TileBundleGroup group, Vector2Int offset)
        {
            if (offset.Equals(Vector2Int.zero)) return;

            Vector2Int oldPos = group.TileGroup[0].Position;
            AddTileGroup(oldPos + offset, group);
            RemoveTileGroup(oldPos);
            //RequestTileRemove(group);
            ////group.Translate(offset);
            //group.LocationKey = null;
            //RequestTilePaint(group);
        }

        public TileBundleGroup RemoveTileGroup(Vector2Int position)
        {
            //var tile = tileMap.GetTile(position);   // Is this supposed to do something?
            TileBundleGroup group = _bundleTileMap.GetGroup(position);
            
            //CHANGE FROM HERE
            if (group == null) return null;
            group.Removed();
            
            foreach (var gTile in group.TileGroup)
            {
                tileMap.RemoveTile(gTile);
            }
            _bundleTileMap.RemoveGroup(group);
            RequestTileRemove(group);

            return group;
        }
        public bool ValidNewGroup(Vector2Int position, Bundle bundle)
        {
            return _bundleTileMap.ValidNewGroup(position, new BundleData(bundle), Vector2.right);
        }
        
        public void ReplaceTileMap(BundleTileMap map)
        {
            //Remove everything
            if (_bundleTileMap.Groups.Count > 0)
            {
                foreach (TileBundleGroup group in _bundleTileMap.Groups)
                {
                    _bundleTileMap.RemoveGroup(group);
                    RequestTileRemove(group);
                }
            }
            foreach(TileBundleGroup group in map.Groups)
            {
                _bundleTileMap.AddGroup(group);
                RequestTilePaint(group);
            }
        }

        public void SetBundle(TileBundleGroup group, Bundle bundle)
        {
            group.BundleData = new BundleData(bundle);
            ReplaceTile(group);
        }
        public void SetBundle(LBSTile tile, Bundle bundle) => SetBundle(_bundleTileMap.GetGroup(tile), bundle);

        public LBSTile GetTile(Vector2Int position)
        {
            return tileMap.GetTile(position);
        }

        public TileBundleGroup GetTileGroup(Vector2Int position)
        {
            return _bundleTileMap.GetGroup(position);
        }

        public BundleData GetBundleData(LBSTile tile)
        {
            return _bundleTileMap.GetGroup(tile).BundleData;
        }

        public bool RotateTile(Vector2Int pos, Vector2 rotation)
        {
            TileBundleGroup t = GetTileGroup(pos);
            if (t is null || !t.Rotatable)
                return false;
            t.Rotation = rotation;

            _newRotations.Add(t);

            return true;
        }

        public Vector2 GetTileRotation(Vector2Int pos)
        {
            TileBundleGroup t = GetTileGroup(pos);
            return t?.Rotation ?? default;
        }

        public BundleData GetBundleData(Vector2 position)
        {
            return GetBundleData(tileMap.GetTile(position.ToInt()));
        }

        public void Clear()
        {
            if (Tilemap.Count == 0) return;
            foreach(TileBundleGroup group in Tilemap)
            {
                _bundleTileMap.RemoveGroup(group);
            }
        }
        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;

            tileMap = OwnerLayer.GetModule<TileMapModule>();
            _bundleTileMap = OwnerLayer.GetModule<BundleTileMap>();
            layer.OnChange += UpdateKeys;

        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            OwnerLayer = null;
            layer.OnChange -= UpdateKeys;

        }

        public void UpdateKeys()
        {
            UpdateKeys(Tilemap.ToList<object>());
        }

        private void ReplaceTile(TileBundleGroup tile)
        {
            RequestTileRemove(tile);
            RequestTilePaint(tile);
        }

        public override object[] RetrieveExpiredTiles()
        {
            return base.RetrieveExpiredTiles().Cast<TileBundleGroup>().Select(t => t.LocationKey).ToArray();
        }
        
        public override object Clone()
        {
            return new PopulationBehaviour(this.IconGuid, this.Name, this.ColorTint);
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

        private BundleCollection GetBundleCollection()
        {
            if (bundleCollection == null)
            {
                bundleCollection = AssetMacro.LoadAssetByGuid<BundleCollection>(bundleRefGui);
            }

            return bundleCollection;
        }

        private Bundle GetMainBundle()
        {
            if(mainBundle == null)
            {
                mainBundle = AssetMacro.LoadAssetByGuid<Bundle>(bundleRefGuid ?? DefaultBundleRefGuid);
            }

            return mainBundle;
        }        
        
        /// <summary>
        /// Get all tileBundleGroups that were rotated since the last time they were retrieved.
        /// The memory of new tiles will be cleared after calling this method.
        /// </summary>
        public TileBundleGroup[] RetrieveNewRotations()
        {
            // If null create a new one
            _newRotations ??= new HashSet<TileBundleGroup>();
            
            // Turn into array
            TileBundleGroup[] o = _newRotations.ToArray();
            
            // Clear memory
            _newRotations.Clear();
            
            // Return array
            return o;
        }
        
        #endregion
    }
}