using ISILab.Commons.Extensions;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.MemoryProfiler;
using UnityEditor.TerrainTools;
using UnityEngine;
namespace ISILab.LBS.Plugin.Components.Behaviours
{
    

    [System.Serializable]
    [RequieredModule(typeof(TileMapModule),
        typeof(ConnectedTileMapModule),
        typeof(SectorizedTileMapModule),
        typeof(ConnectedZonesModule),
        typeof(StairsModule),
        typeof(NoteModule))]
    public class SchemaBehaviour : LBSBehaviour, IBlueprintable
    {
        #region READONLY-FIELDS
        
        [JsonIgnore]
        private static List<string> connections = new List<string>() // esto puede ser remplazado despues (!)
        {
            Empty, // for clearing a wall
            Wall, // default wall connection 
            Door, // within wall connection
            Window, // within wall connection
            LockedDoor, // within wall connection. Opened by key
            StairsUp,
            StairsDown
       //     BlockedDoor // within wall connection. Opened by trigger
        };

        public const string Empty = "Empty";
        public const string Wall = "Wall";
        public const string Door = "Door";
        public const string Window = "Window";
        public const string LockedDoor = "LockedDoor";
        public const string StairsUp = "Stairs Up";
        public const string StairsDown = "Stairs Down";
        //      public const string BlockedDoor = "BlockedDoor";

        #endregion

        #region FIELDS
        [JsonIgnore]
        private TileMapModule tileMap => OwnerLayer.GetModule<TileMapModule>();
        [JsonIgnore]
        private ConnectedTileMapModule tileConnections => OwnerLayer.GetModule<ConnectedTileMapModule>();
        [JsonIgnore]
        private SectorizedTileMapModule areas => OwnerLayer.GetModule<SectorizedTileMapModule>();
        [JsonIgnore]
        private StairsModule stairs => OwnerLayer.GetModule<StairsModule>();

        [SerializeField, HideInInspector]
        private string pressetInsideStyleGuid = "c61b774ce5ee4c640b93988da7937edc";
        [SerializeField, HideInInspector]
        private string pressetOutsideStyleGuid = "c0e28f3a70727474a81b860669e32870";

        [SerializeField, HideInInspector]
        private bool multiLayerConnections = true;

        #endregion

        #region META-FIELDS

        private Zone roomToSet;
        [JsonIgnore]
        public Zone RoomToSet
        {
            get => roomToSet; 
            set => roomToSet = value; 
        }
        [JsonIgnore, HideInInspector]
        public string conectionToSet;
        #endregion

        #region PROEPRTIES
        [JsonIgnore]
        public Bundle PressetInsideStyle
        {
            get => AssetMacro.LoadAssetByGuid<Bundle>(pressetInsideStyleGuid);
            set => pressetInsideStyleGuid = AssetMacro.GetGuidFromAsset(value);
        }

        [JsonIgnore]
        public Bundle PressetOutsideStyle
        {
            get => AssetMacro.LoadAssetByGuid<Bundle>(pressetOutsideStyleGuid);
            set => pressetOutsideStyleGuid = AssetMacro.GetGuidFromAsset(value);
        }

        [JsonIgnore]
        public bool MultiLayerConnections { get => multiLayerConnections; set => multiLayerConnections = value; }

        [JsonIgnore]
        public bool ValidArea => OwnerLayer.GetModule<SectorizedTileMapModule>() is null;
        
        [JsonIgnore]
        public List<Zone> Zones => areas.Zones;

        [JsonIgnore]
        public List<Zone> ZonesWithTiles => areas.ZonesWithTiles;
        
        [JsonIgnore]
        public List<LBSTile> Tiles => tileMap.Tiles;
        [JsonIgnore]
        public List<LBSStair> Stairs => stairs.Stairs;

        public TileMapModule TileMap => tileMap;

        public ConnectedTileMapModule TileConnections => tileConnections;

        public static List<string> Connections => connections;

        [JsonIgnore]
        public List<Vector2Int> Directions => ISILab.Commons.Directions.Bidimencional.Edges;

        #endregion

        #region CONSTRUCTORS
        public SchemaBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }
        #endregion

        #region METHODS
        
        public override void OnGUI()
        {

        }
        
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
            var tiles = Tiles.ToList<object>();
            var stairs = Stairs.ToList<object>();
            foreach (var s in stairs)
            {
                tiles.Add(s);
            }
            UpdateKeys(tiles);
        }

        public void UpdateKeys()
        {
            var tiles = Tiles.ToList<object>();
            var stairs = Stairs.ToList<object>();
            foreach(var s in stairs)
            {
                tiles.Add(s);
            }
            UpdateKeys(tiles);
        }

        public LBSTile AddTile(Vector2Int position, Zone zone)
        {
            if (tileMap.Contains(position)) return null;
                
            var tile = new LBSTile(position);//, "Tile: " + position.ToString());
            
            tileMap.AddTile(tile);
            areas.AddTile(tile, zone);
            
            RequestTilePaint(tile);

            return tile;
        }

        public Zone AddZone()
        {
            string prefix = "Zone: ";
            int counter = 0;
            string suffix = " (" + OwnerLayer.Name + ")";
            string name = prefix + counter;
            IEnumerable<string> names = areas.Zones.Select(z => z.ID);
            while (names.Contains(name))
            {
                counter++;
                name = prefix;
                
                if (counter < 10) name += "0" + counter;
                else   name += counter;
               
            }

            var c = new Color().RandomColorHSV();
            var zone = new Zone(name, c);

            areas.AddZone(zone);
            return zone;
        }

        public void RemoveZone(Zone zone)
        {
            var tiles = areas.GetTiles(zone);
            foreach (var tile in tiles)
            {
                RequestTileRemove(tile);
            }
            
            tileMap.RemoveTiles(tiles);
            areas.RemoveZone(zone);
        }

        public void RemoveTile(Vector2Int position)
        {
            var tile = tileMap.GetTile(position);

            RequestTileRemove(tile);
            
            tileMap.RemoveTile(tile);
            tileConnections.RemoveTile(tile);
            areas.RemovePair(tile);
        }

        public void SetConnection(LBSTile tile, int direction, string connection, bool editedByIA)
        {
            TileConnectionsPair t = tileConnections.GetPair(tile);
            t.SetConnection(direction, connection, editedByIA);
            RequestTilePaint(tile);
        }

        public void AddConnections(LBSTile tile, List<string> connections, List<bool> editedByIA)
        {
            tileConnections.AddPair(tile, connections, editedByIA);
        }

        public void PlaceStair(LBSStair stair, int floor)
        {
            var stairsMod = OwnerLayer.Modules(floor).FirstOrDefault(
                m => m.GetType() == typeof(StairsModule)) as StairsModule;

            if (stairsMod is null) return;
            stairsMod.AddStair(stair);

            if (floor != OwnerLayer.ActiveFloor) return;
            RequestTilePaint(stair);
        }

        public LBSTile GetTile(Vector2Int position)
        {
            return tileMap.GetTile(position);
        }

        public List<LBSTile> GetTiles(Zone zone)
        {
            return areas.GetTiles(zone);
        }

        public Rect GetBound(Zone zone)
        {
            return areas.GetBounds(zone);
        }

        public List<string> GetConnections(LBSTile tile)
        {
            var pair = tileConnections.GetPair(tile);
            return pair?.Connections;
        }

        public Zone GetZone(LBSTile tile)
        {
            var pair = areas.GetPairTile(tile);
            return pair.Zone;
        }

        public Zone GetZone(Vector2 position)
        {
            return GetZone(GetTile(position.ToInt()));
        }

        public List<LBSTile> GetTileNeighbors(LBSTile tile, List<Vector2Int> dirs)
        {
            var tor = new List<LBSTile>();
            foreach (var dir in dirs)
            {
                var t = GetTile(dir + tile.Position);
                tor.Add(t);
            }
            return tor;
        }

        public void RecalculateWalls(List<LBSTile> tiles = null)
        {
            tiles ??= Tiles;
            tiles.ForEach(t => RequestTilePaint(t));

            //foreach (var tile in Tiles)
            for(int i = 0; i < tiles.Count; i++)
            {
                var currZone = GetZone(tiles[i]);

                var currConnects = GetConnections(tiles[i]);
                UnityEngine.Assertions.Assert.IsNotNull(currConnects);

                var neigs = GetTileNeighbors(tiles[i], Directions);

                var edt = tileConnections.GetPair(tiles[i]).EditedByIA;

                for (int j = 0; j < Directions.Count; j++)
                {
                    if (!edt[j])
                        continue;

                    if (neigs[j] == null)
                    {
                        if (currConnects[j] != "Door")
                        {
                            SetConnection(tiles[i], j, "Wall", true);
                        }
                        continue;
                    }

                    var otherZone = GetZone(neigs[j]);
                    if (otherZone.Equals(currZone))
                    {
                        SetConnection(tiles[i], j, "Empty", true);
                    }
                    else
                    {
                        if (currConnects[j] != "Door")
                        {
                            SetConnection(tiles[i], j, "Wall", true);
                        }
                    }
                }
            }
        }

        public void RecalculateWallsAtFloor(int floor, List<LBSTile> tiles = null)
        {
            bool isActiveFloor = floor == OwnerLayer.ActiveFloor;
            var tilemapMod = OwnerLayer.GetModule<TileMapModule>("", floor);
            var sectorizedMod = OwnerLayer.GetModule<SectorizedTileMapModule>("", floor);
            var connectedMod = OwnerLayer.GetModule<ConnectedTileMapModule>("", floor);

            tiles ??= tilemapMod.Tiles;
            if (isActiveFloor) tiles.ForEach(t => RequestTilePaint(t));

            //foreach (var tile in Tiles)
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                var currZone = sectorizedMod.GetPairTile(tile).Zone;
                var currConnects = connectedMod.GetPair(tile).Connections;

                UnityEngine.Assertions.Assert.IsNotNull(currConnects);

                // Get tile neighbors
                var neigs = new List<LBSTile>();
                foreach (var dir in Directions)
                {
                    var t = tilemapMod.GetTile(dir + tile.Position);
                    neigs.Add(t);
                }

                var edt = connectedMod.GetPair(tile).EditedByIA;

                for (int j = 0; j < Directions.Count; j++)
                {
                    if (!edt[j])
                        continue;

                    if (neigs[j] == null)
                    {
                        if (currConnects[j] != "Door")
                        {
                            TileConnectionsPair t = connectedMod.GetPair(tile);
                            t.SetConnection(j, "Wall", true);
                            if (isActiveFloor) RequestTilePaint(tile);
                        }
                        continue;
                    }

                    var otherZone = sectorizedMod.GetPairTile(neigs[j]).Zone;
                    if (otherZone.Equals(currZone))
                    {
                        TileConnectionsPair t = connectedMod.GetPair(tile);
                        t.SetConnection(j, "Empty", true);
                        if (isActiveFloor) RequestTilePaint(tile);
                    }
                    else
                    {
                        if (currConnects[j] != "Door")
                        {
                            TileConnectionsPair t = connectedMod.GetPair(tile);
                            t.SetConnection(j, "Wall", true);
                            if (isActiveFloor) RequestTilePaint(tile);
                        }
                    }
                }
            }
        }

        public override object Clone()
        {
            return new SchemaBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override bool Equals(object obj)
        {
            var other = obj as SchemaBehaviour;

            if (other == null) return false;

            if (!this.Name.Equals(other.Name)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        private class VirtualZoneTile
        {
            public Vector2Int TargetPosition;
            public List<DirConnection> RotatedConnections;
            public List<bool> EditedFlags;
        }

        public bool MoveZone(Zone zone, Vector2Int offset)
        {
            return TransformZone(zone, offset, 0);
        }

        public bool RotateZone(Zone zone, int direction)
        {
            return TransformZone(zone, Vector2Int.zero, direction);
        }

        private bool TransformZone(Zone zone, Vector2Int offset, int rotationDir)
        {
            var currentTiles = GetTiles(zone);
            if (currentTiles.Count == 0) return false;

            var graph = OwnerLayer.GetModule<ConnectedZonesModule>();
            HashSet<Zone> preservedNeighbors = new HashSet<Zone>();

            if (graph != null)
            {
                foreach (var edge in graph.Edges)
                {
                    if (edge.First == zone) preservedNeighbors.Add(edge.Second);
                    else if (edge.Second == zone) preservedNeighbors.Add(edge.First);
                }
            }

            // Calculate old bounds
            Vector2Int oldMin = new Vector2Int(int.MaxValue, int.MaxValue); // bottom-left
            Vector2Int oldMax = new Vector2Int(int.MinValue, int.MinValue); // top-right

            foreach (var t in currentTiles)
            {
                oldMin = Vector2Int.Min(oldMin, t.Position);
                oldMax = Vector2Int.Max(oldMax, t.Position);
            }

            Vector2Int oldSize = oldMax - oldMin;

            List<VirtualZoneTile> virtualTiles = new List<VirtualZoneTile>();
            List<Vector2Int> rotatedRelativePositions = new List<Vector2Int>();
            // Calculate virtual tiles positions and rotated connections
            foreach (var tile in currentTiles)
            {
                Vector2Int localPos = tile.Position - oldMin;
                Vector2Int rotatedPos = localPos;

                if (rotationDir != 0)
                {
                    if (rotationDir > 0)
                        rotatedPos = new Vector2Int(localPos.y, -localPos.x);
                    else
                        rotatedPos = new Vector2Int(-localPos.y, localPos.x);
                }

                rotatedRelativePositions.Add(rotatedPos);
            }

            Vector2Int newRotatedMin = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int newRotatedMax = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var p in rotatedRelativePositions)
            {
                newRotatedMin = Vector2Int.Min(newRotatedMin, p);
                newRotatedMax = Vector2Int.Max(newRotatedMax, p);
            }
            Vector2Int newSize = newRotatedMax - newRotatedMin;

            Vector2Int centeringOffset = (oldSize - newSize) / 2; // to keep the zone centered

            // Calculate final positions and rotated connections
            for (int i = 0; i < currentTiles.Count; i++)
            {
                // Final position after rotation and offset
                Vector2Int finalPos = oldMin + (rotatedRelativePositions[i] - newRotatedMin) + centeringOffset + offset;

                var pair = tileConnections.GetPair(currentTiles[i]);
                List<DirConnection> newConns = new List<DirConnection>();

                // Rotate connections
                if (pair != null && pair.Connections != null)
                {
                    for (int k = 0; k < pair.Connections.Count; k++)
                    {
                        Vector2Int dirVector = ISILab.Commons.Directions.Bidimencional.Edges[k];
                        Vector2Int rotatedDirVector = dirVector;
                        if (rotationDir != 0)
                        {
                            if (rotationDir > 0) rotatedDirVector = new Vector2Int(dirVector.y, -dirVector.x);
                            else rotatedDirVector = new Vector2Int(-dirVector.y, dirVector.x);
                        }
                        int newIndex = ISILab.Commons.Directions.Bidimencional.Edges.IndexOf(rotatedDirVector);
                        if (newIndex != -1)
                        {
                            newConns.Add(new DirConnection(newIndex, pair.Connections[k]));
                        }
                    }
                }

                virtualTiles.Add(new VirtualZoneTile
                {
                    TargetPosition = finalPos,
                    RotatedConnections = newConns,
                    EditedFlags = pair.EditedByIA
                });
            }

            // Check for collisions
            foreach (var vt in virtualTiles)
            {
                if (!tileMap.Contains(vt.TargetPosition)) continue;
                var existingTile = GetTile(vt.TargetPosition);
                if (existingTile != null)
                {
                    var existingZone = GetZone(existingTile);
                    if (existingZone != zone)
                    {
                        Debug.LogWarning($"Colisi�n con zona: {existingZone.ID}");
                        return false;
                    }
                }
            }

            foreach (var t in currentTiles) RemoveTile(t.Position);

            // Create new tiles and set connections
            List<LBSTile> createdTiles = new List<LBSTile>();
            foreach (var vt in virtualTiles)
            {
                var newTile = AddTile(vt.TargetPosition, zone);
                createdTiles.Add(newTile);
                AddConnections(newTile, new List<string> { "Empty", "Empty", "Empty", "Empty" }, new List<bool> { false, false, false, false });
            }

            for (int i = 0; i < virtualTiles.Count; i++)
            {
                var vt = virtualTiles[i];
                var newTile = createdTiles[i];

                foreach (var conn in vt.RotatedConnections)
                {
                    SetConnection(newTile, conn.direction, conn.connection, true);

                    Vector2Int dirVec = ISILab.Commons.Directions.Bidimencional.Edges[conn.direction];
                    var neighbor = GetTile(vt.TargetPosition + dirVec);

                    if (neighbor != null)
                    {
                        int oppDir = (conn.direction + 2) % 4;
                        SetConnection(neighbor, oppDir, conn.connection, true);
                    }
                }
            }

            if (graph != null)
            {
                foreach (var neighbor in preservedNeighbors)
                {
                    if (!graph.EdgesConnected(zone, neighbor))
                    {
                        graph.AddEdge(zone, neighbor);
                    }
                }
            }

            RequestFullRepaint(currentTiles, createdTiles);
            return true;
        }

        public bool CaptureAreaData(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            List<LBSTile> tilesToRemove = tileMap.Tiles;
            List<TileZonePair> tileZonePairsToRemove = areas.PairTiles;
            List<TileConnectionsPair> tileConnectionsPairsToRemove = TileConnections.Pairs;
            List<Zone> zonesToRemove = areas.Zones;

            for (int x = corners.min.x; x <= corners.max.x; x++)
            {
                for (int y = corners.min.y; y <= corners.max.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    LBSTile tile = GetTile(pos);
                    tilesToRemove.Remove(tile);
                    TileConnectionsPair pair = TileConnections.GetPair(pos);
                    tileConnectionsPairsToRemove.Remove(pair);
                    TileZonePair tzp = areas.GetPairTile(pos);
                    tileZonePairsToRemove.Remove(tzp);
                    zonesToRemove.Remove(tzp?.Zone);
                }
            }

            foreach (var tile in tilesToRemove) tileMap.RemoveTile(tile);
            foreach (var tzp in tileZonePairsToRemove) areas.RemovePair(tzp.Tile);
            foreach (var pair in tileConnectionsPairsToRemove) TileConnections.RemoveTile(pair.Tile);
            foreach (var zone in zonesToRemove) areas.RemoveZone(zone);

            return TileMap.Tiles.Count > 0 ||
                areas.Zones.Count > 0 || 
                TileConnections.Pairs.Count > 0 ||
                areas.PairTiles.Count > 0;
        }



        public void SetPosition(Vector2Int parentAnchor, Vector2Int delta)
        {
            foreach (var tile in TileMap.Tiles)
            {
                var distanceToAnchor = tile.Position - parentAnchor;
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
        

        override public void ChangeLevelRender(int prevLevelIndex, int nextLevelIndex)
        {
            List<LBSTile> oldTiles = new List<LBSTile>();
            List<LBSTile> newTiles = new List<LBSTile>();
            List<LBSStair> oldStairs = new List<LBSStair>();
            List<LBSStair> newStairs = new List<LBSStair>();

            var prevModuleList = OwnerLayer.Modules(prevLevelIndex);
            var nextModuleList = OwnerLayer.Modules(nextLevelIndex);

            var prevSectorizedMod = prevModuleList.Find(
                m => m.GetType() == typeof(SectorizedTileMapModule)) as SectorizedTileMapModule;
            var nextSectorizedMod = nextModuleList.Find(
                m => m.GetType() == typeof(SectorizedTileMapModule)) as SectorizedTileMapModule;
            var prevStairMod = prevModuleList.Find(
                m => m.GetType() == typeof(StairsModule)) as StairsModule;
            var nextStairMod = nextModuleList.Find(
                m => m.GetType() == typeof(StairsModule)) as StairsModule;

            foreach (var pTile in prevSectorizedMod.PairTiles)
            {
                oldTiles.Add(pTile.Tile);
            }
            foreach (var pTile in nextSectorizedMod.PairTiles)
            {
                newTiles.Add(pTile.Tile);
            }
            foreach (var s in prevStairMod.Stairs)
            {
                oldStairs.Add(s);
            }
            foreach (var s in nextStairMod.Stairs)
            {
                newStairs.Add(s);
            }

            RequestFullRepaint(oldTiles, newTiles);
            RequestFullRepaint(oldStairs, newStairs);
        }//*/

        public bool MergeLayerData(object incoming, bool overwrite)
        {
            SchemaBehaviour merger = incoming as SchemaBehaviour;
            if (merger == null) return false;

            for (int i = 0; i < merger.areas.Zones.Count; i++)
            {
                var incomingZone = merger.areas.Zones[i];

                var originalZone = areas.Zones.FirstOrDefault(z => z.ID == incomingZone.ID);

                if (originalZone == null)
                {
                    areas.AddZone(incomingZone.Clone() as Zone);
                }
                else if (overwrite)
                {
                    areas.RemoveZone(originalZone);
                    areas.AddZone(incomingZone.Clone() as Zone);
                }
            }

            for (int i = 0; i < merger.areas.PairTiles.Count; i++)
            {
                var incomingPair = merger.areas.PairTiles[i];

                var incomingTile = incomingPair.Tile;
                var incomingZone = incomingPair.Zone;

                TileZonePair originalTile = areas.GetPairTile(incomingTile.Position);

                if (originalTile == null)
                {
                    var newTile = incomingTile.Clone() as LBSTile;
                    tileMap.AddTile(newTile);

                    var zone = areas.Zones.First(z => z.ID == incomingZone.ID);
                    areas.AddTile(newTile, zone);
                }
                else if (overwrite)
                {
                    areas.RemovePair(originalTile.Tile);

                    var newTile = incomingTile.Clone() as LBSTile;
                    tileMap.AddTile(newTile);

                    var zone = areas.Zones.First(z => z.ID == incomingZone.ID);
                    areas.AddTile(newTile, zone);
                }
            }

            for (int i = 0; i < merger.TileConnections.Pairs.Count; i++)
            {
                var incomingConnectionPair = merger.TileConnections.Pairs[i];

                var tile = tileMap.GetTile(incomingConnectionPair.Tile.Position);
                if (tile == null) continue;

                var originalPair = TileConnections.GetPair(tile);

                if (originalPair == null)
                {
                    var newPair = incomingConnectionPair.Clone() as TileConnectionsPair;
                    TileConnections.AddPair(tile, newPair.Connections, newPair.EditedByIA);
                }
                else if (overwrite)
                {
                    originalPair = incomingConnectionPair.Clone() as TileConnectionsPair;
                }
            }

            return true;
        }

        #endregion
    }
}