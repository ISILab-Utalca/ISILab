using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class BundleTileMapAddons : ICloneable
    {

        [SerializeField]
        public List<TileTrigger> trigger = new();
        [SerializeField]
        public TilePatrol patrol;

        public BundleTileMapAddons()
        {
            patrol = new TilePatrol(new List<Vector2>());
        }

        public BundleTileMapAddons(List<TileTrigger> trigger, TilePatrol patrol)
        {
            this.trigger = trigger;
            this.patrol = patrol;
        }

        object ICloneable.Clone()
        {
            return new BundleTileMapAddons(
                trigger.ToList(),                      
                new TilePatrol(patrol.Points.ToList()) 
            );
        }

    }


    [Serializable]
    public struct TilePatrol
    {
        [SerializeField]
        public bool Loop;

        [SerializeField]
        public List<Vector2> Points;

        public TilePatrol(List<Vector2> points)
        {
            Loop = false;

            Points = new();
            Points = points;
        }
    }

    #region TileTrigger

    public enum TileTriggerType
    {
        Box,
        Circle,
        Capsule,
        Cone
    }

    [Serializable]
    public abstract class TileTrigger
    {
        private static readonly Dictionary<TileTriggerType, Type> TriggerTypes = new()
        {
            { TileTriggerType.Box,     typeof(TileBoxTrigger) },
            { TileTriggerType.Circle,  typeof(TileCircleTrigger) },
            { TileTriggerType.Capsule, typeof(TileCapsuleTrigger) },
            { TileTriggerType.Cone,    typeof(TileConeTrigger) }
        };

        public static TileTrigger GetNewInstance(TileTriggerType type)
        {
            if (TriggerTypes.TryGetValue(type, out var triggerType))
            {
                return Activator.CreateInstance(triggerType) as TileTrigger;
            }

            return null;
        }

    }


    [Serializable]
    public class TileBoxTrigger : TileTrigger
    {
        public float Length;
        public TileBoxTrigger() 
        {
        }
    }
    [Serializable]
    public class TileCircleTrigger : TileTrigger
    {
        public float Radius;

        public TileCircleTrigger()
        {
        }
    }
    [Serializable]
    public class TileCapsuleTrigger : TileTrigger
    {
        public float Height;
        public float Radius;

        public TileCapsuleTrigger()
        {
        }
    }
    [Serializable]
    public class TileConeTrigger : TileTrigger
    {
        public float Angle;
        public float Range;

        public TileConeTrigger()
        {
        }
    }


    #endregion
}
