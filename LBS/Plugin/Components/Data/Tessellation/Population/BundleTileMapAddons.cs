using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public abstract class BundleTileMapAddons : ICloneable
    {
        public abstract object Clone();
    }

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
    public class TileTrigger : ISerializationCallbackReceiver, ICloneable
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

        public object Clone()
        {
            TileTrigger clone = new TileTrigger();
            clone.areaColor = areaColor;
            clone.isVisible = isVisible;
            clone.Range = Range;
            clone.activationMode = activationMode;
            clone.Ttype = Ttype;
            clone._eventHooker = _eventHooker.Clone() as LBSEventHooker;
            
            return clone;
        
        }

        public void OnAfterDeserialize()
        {
            _eventHooker ??= new LBSEventHooker();
        }

        public void OnBeforeSerialize()
        {
        }
    }
}
