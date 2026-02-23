using ISILab.Commons.Extensions;
using ISILab.DevTools.Macros;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.Plugin.Components.Behaviours
{
    

    [Serializable]
    public struct DirConnection
    {
        public int direction;
        public string connection;

        public DirConnection(int direction, string connection)
        {
            this.direction = direction;
            this.connection = connection;
        }

        public override bool Equals(object other)
        {
            if (other is DirConnection od)
            {
                return od.connection == connection && od.direction == direction;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(direction, connection);
        }
    }


    [Serializable]
    public class ConnectionData
    {
        // addons from the tilegroup that was used to generate this object in the LBS tool
        [SerializeField, SerializeReference]
        public LBSTile tile;

        [SerializeField, SerializeReference]
        public LBSLayer layer;

        /// <summary>
        /// First value is the direction <see cref="LBSDirection.Connections"/> index.
        /// Second value is the connection <see cref="SchemaBehaviour.Connections"/>.
        /// </summary>
        /// 
        [SerializeField]
        public List<DirConnection> connections;

        public ConnectionData()
        {
            connections = new();
            layer = null;
            tile = null;
        }

        public ConnectionData(LBSLayer layer ,LBSTile tile, List<DirConnection> connections = null)
        {
            this.connections = new();
            if(connections is not null) this.connections = connections;
            this.tile = tile;
            this.layer = layer;
        }

        public bool Equals(ConnectionData other)
        {
            foreach (DirConnection conn in other.connections)
            {
                if (Equals(other.tile, conn)) return true;
            }
          
            return false;
        }

        private bool Equals(LBSTile otherTile, DirConnection connection)
        {
            // no tile cant be equal
            if (tile is null) return false;
            if (!tile.Equals(otherTile)) return false;
            foreach (DirConnection conn in connections)
            {
                if (conn.Equals(connection)) return true;
            }

            return false;
        }

        public bool IsConected(List<DirConnection> otherConns)
        {
            foreach(var conn in connections)
            {
                foreach(var oConn in otherConns)
                {
                    if (oConn.connection != conn.connection) continue;

                    bool bIsConnected = false;
                    switch (LBSDirection.ToString(conn.direction))
                    {
                        case LBSDirection.Up: 
                            bIsConnected = oConn.direction == LBSDirection.ToInt(LBSDirection.Down);
                            break;
                        case LBSDirection.Down:
                            bIsConnected = oConn.direction == LBSDirection.ToInt(LBSDirection.Up);
                            break;
                        case LBSDirection.Right:
                            bIsConnected = oConn.direction == LBSDirection.ToInt(LBSDirection.Left);
                            break;
                        case LBSDirection.Left:
                            bIsConnected = oConn.direction == LBSDirection.ToInt(LBSDirection.Right);
                            break;
                    }
                    if (bIsConnected) return true;
                }
            }
            return false;

        }
    }

    [System.Serializable]
    [RequieredModule(typeof(TileMapModule),
        typeof(ConnectedTileMapModule),
        typeof(SectorizedTileMapModule),
        typeof(ConnectedZonesModule))]
    public class SchemaBehaviour : LBSBehaviour, IObjectData
    {
        #region READONLY-FIELDS
        
        [JsonIgnore]
        private static List<string> connections = new List<string>() // esto puede ser remplazado despues (!)
        {
            Empty, // for clearing a wall
            Wall, // default wall connection 
            Door, // within wall connection
            Window, // within wall connection
            LockedDoor // within wall connection. Opened by key
       //     BlockedDoor // within wall connection. Opened by trigger

        };

        public const string Empty = "Empty";
        public const string Wall = "Wall";
        public const string Door = "Door";
        public const string Window = "Window";
        public const string LockedDoor = "LockedDoor";
  //      public const string BlockedDoor = "BlockedDoor";

        #endregion

        #region FIELDS
        [JsonIgnore]
        private TileMapModule tileMap => OwnerLayer.GetModule<TileMapModule>();
        [JsonIgnore]
        private ConnectedTileMapModule tileConnections => OwnerLayer.GetModule<ConnectedTileMapModule>();
        [JsonIgnore]
        private SectorizedTileMapModule areas => OwnerLayer.GetModule<SectorizedTileMapModule>();

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
            UpdateKeys(Tiles.ToList<object>());
        }

        public void UpdateKeys()
        {
            UpdateKeys(Tiles.ToList<object>());
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

        public void RequestFullRepaint(List<LBSTile> olds, List<LBSTile> news)
        {
            olds.ForEach(t => RequestTileRemove(t));
            news.ForEach(t => RequestTilePaint(t));
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
                        Debug.LogWarning($"Colisión con zona: {existingZone.ID}");
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

        public object[] GetObjects(Vector2Int StartPosition, Vector2Int EndPosition)
        {
            (Vector2Int min, Vector2Int max) corners = OwnerLayer.ToFixedPosition(StartPosition, EndPosition);

            TileMapModule tileMapClone = TileMap.Clone() as TileMapModule;
            tileMapClone.Clear();

            SectorizedTileMapModule areasClone = areas.Clone() as SectorizedTileMapModule;
            areasClone.Clear();

            ConnectedTileMapModule tileConnectionsClone = TileConnections.Clone() as ConnectedTileMapModule;
            TileConnections.Clear();


            foreach (Zone zone in ZonesWithTiles)
            {
                for (int x = corners.min.x; x <= corners.max.x; x++)
                {
                    for (int y = corners.min.y; y <= corners.max.y; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (zone.Positions.Contains(pos))
                        {

                            // Tile
                            LBSTile tile = GetTile(pos);
                            if (tile != null)
                            {
                                LBSTile tileClone = tile.Clone() as LBSTile;
                                tileMapClone.AddTile(tileClone);

                                // Area
                                Zone zoneClone = zone.Clone() as Zone;
                                areasClone.AddTile(tileClone, zoneClone);

                            }

                            // Connection
                            TileConnectionsPair pair = TileConnections.GetPair(pos);
                            if (pair != null)
                            {
                                TileConnectionsPair pairClone = pair.Clone() as TileConnectionsPair;
                                tileConnectionsClone.AddPair(
                                    pairClone.Tile,
                                    pairClone.Connections,
                                    pairClone.EditedByIA
                                );
                            }
                        }
                    }
                }

            }

            return new object[] 
            { 
                tileConnectionsClone,
                areasClone,
                tileMapClone
            };
        }

        public void LoadObjects(object[] objects)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}