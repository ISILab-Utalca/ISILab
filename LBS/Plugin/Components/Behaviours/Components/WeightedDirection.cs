using System;
using UnityEngine;

namespace ISILab.LBS.Behaviours
{

    // the rotation type with which a new tile is created
    public enum TileMakeRot
    {
        Fixed,
        Random,
        Weighted
    }

    [Serializable]
    public class WeightedDirection
    {
        [SerializeField]
        string direction;
        [SerializeField]
        float weight;

        public float Weight { get => weight; set => weight = value; }
        public string Direction { get => direction; }

        public WeightedDirection(string direction, float weight)
        {
            this.direction = direction;
            this.weight = weight;
        }
    }
}
