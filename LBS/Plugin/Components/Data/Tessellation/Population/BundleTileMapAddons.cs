using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    public class BundleTileMapAddons : ICloneable
    {
        TileTier tier;
        List<TileTrigger> trigger = new();
        TilePatrol patrol;

        public BundleTileMapAddons(){}

        public BundleTileMapAddons(TileTier tier, List<TileTrigger> trigger, TilePatrol patrol)
        {

            this.tier = tier;
            this.trigger = trigger;
            this.patrol = patrol;
        }

        object ICloneable.Clone()
        {
            return new BundleTileMapAddons(this.tier, this.trigger, this.patrol);
        }
    }

    [Serializable]
    public enum TileTier
    {
        Low,
        Mid,
        High
    }

    [Serializable]
    public struct TilePatrol
    {
        [SerializeField]
        public Vector3[] Points;
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
