using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public abstract class BundleTileMapAddons
    {
    }

    [Serializable]
    public struct TilePatrol
    {
        
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
}
