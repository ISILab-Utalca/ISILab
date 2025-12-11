using ISILab.Commons.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class BundleTileMapAddons : ICloneable
    {

        [SerializeReference]
        public List<TileTrigger> triggers = new();
        [SerializeField]
        public TilePatrol patrol;

        public BundleTileMapAddons()
        {
            patrol = new TilePatrol(new List<Vector2>());
        }

        public BundleTileMapAddons(List<TileTrigger> trigger, TilePatrol patrol)
        {
            this.triggers = trigger;
            this.patrol = patrol;
        }

        object ICloneable.Clone()
        {
            return new BundleTileMapAddons(
                triggers.ToList(),                      
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
       // Capsule,
       // Cone
    }

    [Serializable]
    public abstract class TileTrigger
    {
        [SerializeField]
        public Color areaColor;

        [SerializeField]
        public bool isVisible;

        private static readonly Dictionary<TileTriggerType, Type> TriggerTypes = new()
        {
            { TileTriggerType.Box,     typeof(TileBoxTrigger) },
            { TileTriggerType.Circle,  typeof(TileCircleTrigger) }
           // { TileTriggerType.Capsule, typeof(TileCapsuleTrigger) },
           // { TileTriggerType.Cone,    typeof(TileConeTrigger) }
        };

        public static TileTrigger GetNewInstance(TileTriggerType type)
        {
            if (TriggerTypes.TryGetValue(type, out var triggerType))
            {
                TileTrigger instance = Activator.CreateInstance(triggerType) as TileTrigger;
                instance.areaColor = new Color().RandomColorHSV();
                instance.isVisible = true;
                return instance;

            }

            return null;
        }


        public static TileTriggerType GetType(Type triggerClass)
        {
            foreach(var entry in TriggerTypes)
            {
                if (entry.Value == triggerClass) return entry.Key;
            }
            return TileTriggerType.Circle;
        }
    }


    [Serializable]
    public class TileBoxTrigger : TileTrigger
    {
        [SerializeField] public float Length = 1;
        public TileBoxTrigger() 
        {
        }
    }
    [Serializable]
    public class TileCircleTrigger : TileTrigger
    {
        [SerializeField] public float Radius = 1;

        public TileCircleTrigger()
        {
        }
    }
    [Serializable]
    public class TileCapsuleTrigger : TileTrigger
    {
        [SerializeField] public float Height;
        [SerializeField] public float Radius;

        public TileCapsuleTrigger()
        {
        }
    }
    [Serializable]
    public class TileConeTrigger : TileTrigger
    {
        [SerializeField] public float Angle;
        [SerializeField] public float Range;

        public TileConeTrigger()
        {
        }
    }


    #endregion
}
