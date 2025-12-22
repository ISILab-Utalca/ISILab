
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSTriggerHandler : MonoBehaviour
    {
        #region FIELDS
        [SerializeField, SerializeReference, HideInInspector]
        private LBSEventHooker hooker;
        [SerializeField, SerializeReference, HideInInspector]
        private LBSGeneratedEventHook genEventHook;
        [SerializeField]
        private TriggerActivationMode activationMode;
        #endregion

        #region PROPERTIES
        public LBSEventHooker Hooker
        {
            get => hooker;
            set
            {
                hooker = value;
                GenEventHook.AssignEvents(hooker);
            }
        }

        public LBSGeneratedEventHook GenEventHook
        {
            get
            {
                genEventHook = gameObject.GetComponentInParent<LBSGeneratedEventHook>();
                genEventHook ??= gameObject.transform.parent.gameObject.AddComponent<LBSGeneratedEventHook>();
                return genEventHook;
            }
        }

        public TriggerActivationMode ActivationMode { get => activationMode; internal set => activationMode = value; }
        #endregion

        #region CONSTRUCTORS

        #endregion

        #region METHODS

        private void Start()
        {
            //GenEventHook.AssignEvents(hooker);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (GenEventHook != null && ActivationMode == TriggerActivationMode.OnEnter)
            { 
                GenEventHook.BroadcastOnEnter(); 
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (GenEventHook != null && ActivationMode == TriggerActivationMode.OnExit)
            { 
                GenEventHook.BroadcastOnExit(); 
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if (GenEventHook != null && ActivationMode == TriggerActivationMode.OnStay)
            { 
                GenEventHook.BroadcastOnStay(); 
            }
        }
        #endregion
    }
}