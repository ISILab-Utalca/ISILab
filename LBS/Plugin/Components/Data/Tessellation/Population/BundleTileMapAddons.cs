using ISILab.Commons.Extensions;
using ISILab.LBS.Plugin.Components.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class BundleTileMapAddons : ICloneable
    {

        #region FIELDS
        [SerializeReference]
        private List<TileTrigger> triggers = new();
        [SerializeField]
        private TilePatrol patrol;
        [SerializeReference]
        private LBSEventHooker interact = new();
        [SerializeReference]
        private LBSEventHooker destroyed = new();
        #endregion

        #region PROPERTIES
        public List<TileTrigger> Triggers { get => triggers; set => triggers = value; }
        public TilePatrol Patrol { get => patrol; set => patrol = value; }
        public LBSEventHooker Interact { get => interact; set => interact = value; }
        public LBSEventHooker Destroyed { get => destroyed; set => destroyed = value; }

        #endregion


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

        public void SetLoop(bool newValue) => Loop = newValue;
    }

    #region TileTrigger

    public enum TriggerActivationMode
    {
        OnEnter,
        OnExit,
        OnStay
    }

    public enum TileTriggerType
    {
        Box,
        Circle
    }

    [Serializable]
    public class TileTrigger : ISerializationCallbackReceiver
    {
        [SerializeField] public Color areaColor;

        [SerializeField] public bool isVisible;

        [SerializeField] public float Range;

        [SerializeField] public TriggerActivationMode activationMode = TriggerActivationMode.OnEnter;

        [SerializeField] public TileTriggerType Ttype;

        [SerializeReference] public LBSEventHooker _eventHooker;

        public TileTrigger()
        {
            areaColor = Color.white;
            isVisible = true;
            Range = 1;
            Ttype = TileTriggerType.Box;
            _eventHooker = new LBSEventHooker();
        }

        public void OnAfterDeserialize()
        {
            _eventHooker ??= new LBSEventHooker();
        }

        public void OnBeforeSerialize()
        {
        }
    }


    #endregion
}
