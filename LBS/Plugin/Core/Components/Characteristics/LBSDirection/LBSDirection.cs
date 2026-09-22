using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic containing 4-connected directioned labels and a center label. Mainly used for spatial tiled modules (E.g.: Interior, Exterior).
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Directions", "")]
    public class LBSDirection : LBSCharacteristic, ICloneable
    {
        #region FIELDS
        [Tooltip("4-Connected: 0: Right, 1: Up, 2: Left, 3: Down")]
        [SerializeField, JsonRequired]
        private List<string> connections = new List<string>();

        [SerializeField, JsonRequired]
        private string center;

        public const string Right = "Right";
        public const string Left = "Left";
        public const string Up = "Up";
        public const string Down = "Down";
        //public const string Center = "Center";
        public static readonly List<string> Directions = new() { Right, Up, Left, Down };

        #endregion

        #region PROPERTIES
        /// <summary>
        /// 4-connected directioned labels. This property retrieves a copy of the real values.
        /// </summary>
        [JsonIgnore]
        public List<string> Connections => new List<string>(connections);

        /// <summary>
        /// Center label.
        /// </summary>
        [JsonIgnore]
        public string Center => center ?? string.Empty;

        // Size should always be 4; therefore, this property should be removed or reworked.
        [JsonIgnore]
        public int Size
        { 
            get => connections.Count;
            set
            {
                
                if(connections.Count < value)
                {
                    connections.AddRange(new string[value - connections.Count]);
                }
                else if(connections.Count > value)
                {
                    connections.RemoveRange(value - 1, value - connections.Count);
                }
            }
        }
        #endregion

        #region CONSTRUCTORS
        public LBSDirection() : base() {  }
        

        public LBSDirection(List<string> tags)
        {
            this.connections = tags;
            Size = tags.Count;
        }
        #endregion

        #region METHODS

        /// <summary>
        /// Right: 0
        /// Up: 1
        /// Left: 2
        /// Down: 3
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        // TODO replace hardcoding and magic number search with this
        public static int ToInt(string connection)
        {
            switch (connection)
            {
                case Right: return 0;
                case Up: return 1;
                case Left: return 2;
                case Down: return 3;
                    // should never be default what are you doing!
                default: return -1;
            }
        }

        // TODO replace hardcoding and magic number search with this
        /// <summary>
        /// 0: Right
        /// 1: Up
        /// 2: Left
        /// 3: Down
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static string ToString(int connection)
        {
            switch (connection)
            {
                case 0: return Right;
                case 1: return Up;
                case 2: return Left;
                case 3: return Down;
                // should never be default what are you doing!
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Retrieves an array with the connections rotated by a specified amount.
        /// </summary>
        /// <param name="rotation">Times to rotate the connections.</param>
        /// <returns>An array with the rotated connections.</returns>
        public string[] GetConnection(int rotation = 0)
        {
            var toR = Connections;

            toR = toR.Rotate(rotation);

            return toR.ToArray();
        }

        /// <summary>
        /// Replace a connection with the value of a given tag.
        /// </summary>
        /// <param name="tag">The label from which the connection will be assigned.</param>
        /// <param name="index">Index representing the direction to replace.<br/>Follow the convention: 0: Right, 1: Up, 2: Left, 3: Down</param>
        public void SetConnection(LBSTag tag, int index)
        {
            if(connections.Count <= index || index < 0)
            {
                Debug.LogError("[ISILab] Index out of Range ");
                return;
            }

            try
            {
                connections[index] = tag.Label;
            }
            catch
            {
                Debug.LogError("[ISILab] LBSTag not found. The project's LBS Asset Storage may be outdated.");
                return;
            }
        }

        /// <summary>
        /// Replace the center label with the value of a given tile.
        /// </summary>
        /// <param name="tag">The label from which the center will be asigned.</param>
        public void SetCenter(LBSTag tag)
        {
            try
            {
                center = tag.Label;
            }
            catch
            {
                Debug.LogError("[ISILab] LBSTag not found. The project's LBS Asset Storage may be outdated.");
                return;
            }
        }

        public override object Clone()
        {
            return new LBSDirection(new List<string>(this.connections));
        }

        public override bool Equals(object obj)
        {

            var other = obj as LBSDirection;

            if (other != null)
            {
                if (this.Size != other.Size) return false;
            }
            for(int i = 0; i>this.Size; i++)
            {
                if (this.Connections[i] != other.Connections[i]) { return false; }
            }
            if(!Equals(Center, other.Center)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override List<string> Validate()
        {
            List<string> warnings = new List<string>();
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i] == null)
                {
                    warnings.Add("Connection " + i + " in LBSDirection is null.");
                }
            }
            
            return warnings;
        }
        #endregion

        
    }
}
