using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Extensions;
using ISILab.Commons.Utility;
using ISILab.Extensions;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using Newtonsoft.Json;
using UnityEngine;
using static ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators.EvaluatorHelper;

namespace ISILab.LBS.Modules
{
    [System.Serializable]
    public class ConnectedTileMapModule : LBSModule
    {
        public enum ConnectedTileType { EdgeBased, VertexBased }

        #region FIELDS
        [SerializeField, JsonRequired]
        private int connectedDirections = 4;

        [SerializeField, JsonRequired]
        private ConnectedTileType gridType;

        [SerializeField, JsonRequired, SerializeReference]
        private List<TileConnectionsPair> pairs = new List<TileConnectionsPair>();
        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public int ConnectedDirections
        {
            get => connectedDirections;
            set => connectedDirections = value;
        }

        [JsonIgnore]
        public List<TileConnectionsPair> Pairs => new List<TileConnectionsPair>(pairs);

        [JsonIgnore]
        public ConnectedTileType GridType => gridType;

        [JsonIgnore]
        public AStarNode[] PathfindNodes { get => Pathfind.PathfindNodes; set => Pathfind.PathfindNodes = value; }// = new AStarNode[0];
        [JsonIgnore]
        public Dictionary<Vector2Int, int[]> JPSDistances { get => Pathfind.JPSDistances; set => Pathfind.JPSDistances = value; }// = new();
        private bool PathfindInitialized { get => Pathfind.pathfindInitialized; set => Pathfind.pathfindInitialized = value; }

        public PathfindInfo Pathfind = new();
        #endregion

        #region EVENTS
        public event Action<ConnectedTileMapModule, TileConnectionsPair> OnAddPair;
        public event Action<ConnectedTileMapModule, TileConnectionsPair> OnRemovePair;
        #endregion

        #region CONSTRUCTORS
        public ConnectedTileMapModule() : base()
        {
            id = GetType().Name;
        }

        public ConnectedTileMapModule(IEnumerable<TileConnectionsPair> tiles, int connectedDirections, ConnectedTileType gridType, PathfindInfo pathfindInfo, string id = "ConnectedTileMapModule") : base(id)
        {
            this.connectedDirections = connectedDirections;
            this.gridType = gridType;
            foreach (var t in tiles)
            {
                AddPair(t.Tile, t.Connections, t.EditedByIA);
            }
            Pathfind = pathfindInfo;
            //OnChanged += (m, old, pair) =>
            //{
            //    Pathfind = new();
            //};
        }
        #endregion

        #region METHODS
        public void SetConnection(LBSTile tile, int direction, string connection, bool editedByIA)
        {
            var pair = GetPair(tile);

            var old = new TileConnectionsPair(tile, pair.Connections, pair.EditedByIA);

            pair.SetConnection(direction, connection, editedByIA);

            OnChanged?.Invoke(this, new List<object>() { old }, new List<object>() { pair });
        }

        public void SetConnections(LBSTile tile, List<string> connections, List<bool> canBeEditedByIA)
        {
            var pair = GetPair(tile);

            var old = new TileConnectionsPair(tile, pair.Connections, pair.EditedByIA);

            pair.SetConnections(connections, canBeEditedByIA);

            OnChanged?.Invoke(this, new List<object>() { old }, new List<object>() { pair });
        }

        public void AddPair(LBSTile tile, List<string> connections, List<bool> canBeEditedByIA)
        {
            var pair = new TileConnectionsPair(tile, connections, canBeEditedByIA);
            var current = GetPair(pair.Tile);
            if (current != null)
            {
                pairs.Remove(current);
                OnRemovePair?.Invoke(this, current);
            }
            pairs.Add(pair);

            OnChanged?.Invoke(this, null, new List<object>() { pair });
            OnAddPair?.Invoke(this, pair);
        }

        public TileConnectionsPair GetPair(LBSTile tile)
        {
            if (pairs.Count <= 0)
                return null;
            return pairs.Find(t => t.Tile.Equals(tile));
        }

        public TileConnectionsPair GetPair(Vector2Int pos)
        {
            return pairs.Find(t => t.Tile.Position == pos);
        }

        public List<Tuple<TileConnectionsPair, int>> GetAllPairsWithConnections(params string[] connections)
        {
            var ret = new List<Tuple<TileConnectionsPair, int>>();
            foreach(TileConnectionsPair pair in pairs)
            {
                for(int i = 0; i < 4; i++)
                {
                    if (connections.Contains(pair.Connections[i]))
                    {
                        ret.Add(new Tuple<TileConnectionsPair, int>(pair, i));
                    }
                }
            }
            return ret;
        }

        public List<string> GetConnections(LBSTile tile)
        {
            TileConnectionsPair p = GetPair(tile);
            if(p is null) return new List<string>();
            return p.Connections;
        }

        public List<string> GetConnections(Vector2Int pos)
        {
            TileConnectionsPair p = GetPair(pos);
            if (p is null) return new List<string>();
            return p.Connections;
        }

        public void RemoveTile(TileConnectionsPair pair)
        {
            pairs.Remove(pair);
            OnChanged?.Invoke(this, new List<object>() { pair }, null);
            OnRemovePair?.Invoke(this, pair);
        }

        public void RemoveTile(LBSTile tile)
        {
            var pair = GetPair(tile);
            pairs.Remove(pair);
            OnChanged?.Invoke(this, new List<object>() { pair }, null);
            OnRemovePair?.Invoke(this, pair);
        }

        public void RemoveTile(int index)
        {
            var pair = pairs[index];
            pairs.RemoveAt(index);

            OnChanged?.Invoke(this, new List<object>() { pair }, null);
            OnRemovePair?.Invoke(this, pair);
        }

        public void InitializePathfinding(PathfindingAlgorithm searchType = PathfindingAlgorithm.JPS_Plus) => InitializePathfinding(GetBounds(), searchType);

        public void InitializePathfinding(Rect selection, PathfindingAlgorithm searchType = PathfindingAlgorithm.JPS_Plus)
        {
            //if (PathfindInitialized) return;

            bool jps = searchType == PathfindingAlgorithm.JPS_Plus;

            if (!jps && searchType != PathfindingAlgorithm.A_Star) 
                return;

            int w = (int)selection.width;
            int h = (int)selection.height;
            int l = w * h;

            PathfindNodes = jps ? new JPSNode[l] : new AStarNode[l];
            for(int i = (int)selection.xMin; i < (int)selection.xMax; i++)
            {
                for(int j = (int)selection.yMin; j < (int)selection.yMax; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j);
                    if (GetPair(pos) is not null)
                    {
                        PathfindNodes[selection.GlobalToIndex(pos)] = jps ? new JPSNode(pos) : new AStarNode(pos);
                    }
                }
            }

            if(jps) JPSDistances = JPSPlus.JPSPreprocessDistances(selection, this);

            //PathfindInitialized = true;
        }



        public override Rect GetBounds()
        {
            if (pairs.Count == 0)
            {
                return default(Rect);
            }
            return pairs.Select(t => t.Tile).GetBounds();
        }

        public override bool IsEmpty()
        {
            return pairs.Count <= 0;
        }

        public override void Clear()
        {
            pairs.Clear();
        }

        public override object Clone()
        {
            var pairs = this.pairs.Select(t => t.Clone()).Cast<TileConnectionsPair>();
            var clone = new ConnectedTileMapModule(pairs, connectedDirections, GridType, Pathfind, ID);
            return clone;
        }

        public override void Rewrite(LBSModule module)
        {
            var connectedTileMap = module as ConnectedTileMapModule;
            if (connectedTileMap == null)
            {
                return;
            }
            Clear();
            connectedDirections = connectedTileMap.connectedDirections;
            foreach (var t in connectedTileMap.pairs)
            {
                AddPair(t.Tile, t.Connections, t.EditedByIA);
            }
        }

        public override void Print()
        {
            string msg = "";
            msg += "Type: " + GetType() + "\n";
            msg += "Hash code: " + GetHashCode() + "\n";
            msg += "ID: " + ID + "\n";
            msg += "\n";
            foreach (var pair in pairs)
            {
                msg += pair.Tile.Position + " - ";
                foreach (var connect in pair.Connections)
                {
                    msg += connect + " | ";
                }
                msg += "\n";
            }
            Debug.Log(msg);
        }

        public override bool Equals(object obj)
        {
            var other = obj as ConnectedTileMapModule;

            if (other == null) return false;

            if(other.ID != this.ID) return false;

            if (other.connectedDirections != this.connectedDirections) return false;

            if(other.gridType != this.gridType) return false;

            var pCount = this.pairs.Count;

            if (pCount != other.pairs.Count) return false;

            for (int i = 0; i < pCount; i++)
            {
                var p1 = other.pairs[i];
                var p2 = this.pairs[i];

                if (!p1.Equals(p2)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

    }

    [System.Serializable]
    public class TileConnectionsPair : ICloneable
    {
        #region FIELDS
        [SerializeField, SerializeReference, JsonRequired]
        private LBSTile tile;

        [SerializeField, SerializeReference, JsonRequired]
        private List<string> connections = new List<string>();

        [SerializeField, SerializeReference, JsonRequired]
        private List<bool> editedByIA = new List<bool>();
        #endregion

        #region PROEPRTIES
        [JsonIgnore]
        public LBSTile Tile
        {
            get => tile;
        }

        [JsonIgnore]
        public List<string> Connections
        {
            get => connections;
        }

        public List<bool> EditedByIA => editedByIA;
        #endregion

        #region CONSTRUCTORS
        public TileConnectionsPair(LBSTile tile, IEnumerable<string> connections, List<bool> editedByIA)
        {
            this.tile = tile;
            this.connections = connections.ToList();
            this.editedByIA = editedByIA;
        }
        #endregion

        #region METHODS
        public void SetConnections(IEnumerable<string> connections, List<bool> editedByIA)
        {
            this.connections = new List<string>(connections);
            this.editedByIA = new List<bool>(editedByIA);
        }

        public void SetConnection(int index, string connection, bool editedByIA)
        {
            this.connections[index] = connection;
            this.editedByIA[index] = editedByIA;
        }

        public object Clone()
        {
            return new TileConnectionsPair(
                CloneRefs.Get(tile) as LBSTile,
                connections.Select(c => c.Clone() as string),
                new List<bool>(editedByIA)
                );
        }

        public override bool Equals(object obj)
        {
            var other = obj as TileConnectionsPair;

            if (other == null) return false;
            if (other.tile == null) return false;

            if (!tile.Equals(other.tile)) return false;

            var cCount = other.connections.Count;

            for (int i = 0; i < cCount; i++)
            {
                var c1 = this.connections[i];
                var c2 = other.connections[i];

                if (!c1.Equals(c2)) return false;
            }

            var eCount = other.editedByIA.Count;

            for (int i = 0; i < eCount; i++)
            {
                var e1 = this.editedByIA[i];
                var e2 = other.editedByIA[i];

                if (!e1.Equals(e2)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return LBSHashUtilities.CustomListHash(connections);
        }

        public override string ToString()
        {
            string s = Tile + " {";
            foreach(string conn in connections)
            {
                s += $"'{conn}', "; 
            }
            s.Remove(s.Length - 2);
            s += "}";
            return s;
        }
        #endregion
    }

    public static class TileConnectionsPairExtensions
    {
        public static List<int> HasConnections(this TileConnectionsPair pair, params string[] connections)
        {
            var ret = new List<int>();
            if (pair is null) return ret;
            for (int i = 0; i < 4; i++)
            {
                if (connections.Contains(pair.Connections[i]))
                    ret.Add(i);
            }
            return ret;
        }

        public static bool IsFloor(this TileConnectionsPair current, List<string> floorTags)
        {
            if (current == null) return false;

            const int minFloorCount = 3;
            int floorCount = 0;
            foreach (string connection in current.Connections)
            {
                foreach (string floorTag in floorTags)
                {
                    if (connection.Equals(floorTag))
                    {
                        floorCount++;
                        if (floorCount >= minFloorCount)
                            return true;
                        break;
                    }
                }
            }

            return false;
        }
    }

    public class PathfindInfo
    {
        public AStarNode[] PathfindNodes { get; set; } = new AStarNode[0];
        public Dictionary<Vector2Int, int[]> JPSDistances { get; set; } = new();
        internal bool pathfindInitialized = false;

        public PathfindInfo()
        {
            PathfindNodes = new AStarNode[0];
            JPSDistances = new();
            pathfindInitialized = false;
        }
    }
}