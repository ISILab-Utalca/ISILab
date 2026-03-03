using ISILab.Extensions;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class BlueprintData
    {
        [SerializeReference] // using reference for polymorphic types modules, behavior, assist..
        object _object;
        [SerializeField]
        private Vector2 min;
        [SerializeField]
        private Vector2 max;

        public object Object => _object;
        public Vector2Int Min => min.ToInt();
        public Vector2Int Max => max.ToInt();

        public BlueprintData(object _object, Vector2Int min, Vector2Int max)
        {
            this._object = _object;
            this.min = min;
            this.max = max;
        }

        public bool Equals(BlueprintData other)
        {
            if (other == null) return false;

            return Object.GetType() == other.Object.GetType();
        }

        public override bool Equals(object obj)
        {
            return obj is BlueprintData other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Object.GetType());
        }
    }
}
