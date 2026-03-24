using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
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
    }


    [Serializable]
    public class ConnectionData: ICloneable
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

        public object Clone()
        {
            ConnectionData clone = new ConnectionData();
            clone.tile = tile.Clone() as LBSTile;
            clone.layer = layer.Clone() as LBSLayer;
            clone.connections = connections;
            return clone;
        }
    }
}