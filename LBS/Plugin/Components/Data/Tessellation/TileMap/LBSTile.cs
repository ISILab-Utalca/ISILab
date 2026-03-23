using System;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap
{
    [Serializable]
    public class LBSTile : ICloneable
    {
        #region FIELDS
        [SerializeField, JsonRequired]
        public int x, y;

        //NOTE: The tag was causing problems with serialization. It may cause problems in the future.
        //If it needs to be reimplemented, it should be reimplemented in ExteriorDrawer (the script that uses it) and not in LBSTile.
        
        #endregion

        #region PROPERTIES

        [JsonIgnore]
        public Vector2Int Position
        {
            get => new Vector2Int(x, y);
            set { x = value.x; y = value.y; }
        }
        #endregion

        #region CONSTRUCTORS
        public LBSTile(Vector2 _position)
        {
            this.x = (int)_position.x;
            this.y = (int)_position.y;
        }
        #endregion

        #region METHODS
        public override bool Equals(object _obj)
        {
            if (_obj == null) return false;

            LBSTile other = (LBSTile)_obj;

            if (other == null) return false;

            if (!other.Position.Equals(this.Position)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x.GetHashCode(), y.GetHashCode());
        }

        public virtual object Clone()
        {
            return new LBSTile(Position);
        }

        public override string ToString()
        {
            return "LBSTile " + Position;
        }
        #endregion
    }
}
