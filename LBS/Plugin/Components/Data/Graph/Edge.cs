using System;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class Edge : ICloneable
    {
        #region FIELDS
        
        [SerializeField, SerializeReference, JsonRequired]
        private object from;

        [SerializeField, SerializeReference, JsonRequired]
        private object to;
        #endregion
        
        #region PROPERTIES
        
        public object From
        {
            get => from;
            set => from = value;
        }

        public object To
        {
            get => to;
            set => to = value;
        }
        #endregion

        #region CONSTRUCTORS
        public Edge()
        {
        }

        public Edge(object from, object to)
        {
            this.from = from;
            this.to = to;
        }

        #endregion

        #region METHODS
        public object Clone()
        {
            var clonedFrom = CloneRefs.Get(from);
            var clonedTo = CloneRefs.Get(to);

            return new Edge
            {
                From = clonedFrom,
                To = clonedTo
            };
        }
        public override bool Equals(object obj)
        {
            if (obj is not Edge other) return false;

            bool fromEquals = From == null ? other.From == null : From.Equals(other.From);
            bool toEquals = To == null ? other.To == null : To.Equals(other.To);

            return fromEquals && toEquals;
        }

        public override int GetHashCode()
        {
            int fromHash = From != null ? From.GetHashCode() : 0;
            int toHash = To != null ? To.GetHashCode() : 0;

            return HashCode.Combine(fromHash, toHash);
        }
        #endregion
    }
}
