using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using UnityEngine;


namespace ISILab.LBS.Characteristics
{
    [System.Serializable]
    //[LBSCharacteristic("Directions", "")]
    public class LBSDirection : LBSCharacteristic, ICloneable
    {

        #region SUB-STRUCTURE
        /*
        [System.Serializable]
        public class weightStruct
        {
            [SerializeField]
            public GameObject target;

            [Range(0f, 1f)]
            public float weigh;
        };*/
        #endregion

        #region FIELDS
        [Tooltip("4-Conected: 0: Right, 1: Up, 2: Left, 3: Down")]
        [SerializeField, JsonRequired]
        private List<string> connections = new List<string>();


        public const string Right = "Right";
        public const string Left = "Left";
        public const string Up = "Up";
        public const string Down = "Down";


        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public List<string> Connections => new List<string>(connections);

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

        //[SerializeField]
        //public List<weightStruct> Weights => new List<weightStruct>(weights); 

        //public float TotalWeight => weights.Sum( w => w.weigth);
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

        public string[] GetConnection(int rotation = 0)
        {
            var conections = connections;
            var toR = new List<string>(connections);

            toR = toR.Rotate(rotation);

            return toR.ToArray();
        }

        public void SetConnection(LBSTag tag, int index)
        {
            if(connections.Count <= index)
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
