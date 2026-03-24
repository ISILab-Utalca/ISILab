using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Patrol : BundleTileMapAddons
    {
        [SerializeField]
        private bool loops;

        [SerializeField]
        private List<Vector2> points = new();


        public bool Loops
        {
            get => loops;
            set
            {
                loops = value;
            }
        }
        public List<Vector2> Points
        {
            get => points;
            set
            {
                points = value;
            }
        }


        public Addon_Patrol()
        {
            points = new List<Vector2>();
        }
        public Addon_Patrol(List<Vector2> InPoints, bool Loops = false)
        {
            points = InPoints;
            loops = Loops;
        }

        public override object Clone()
        {
            Addon_Patrol clone = new Addon_Patrol();
            clone.Loops = Loops;
            clone.Points = Points;
            return clone;
        }
    }
}