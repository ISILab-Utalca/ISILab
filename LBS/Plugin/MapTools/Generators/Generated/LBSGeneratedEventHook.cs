using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using System;
using System.Collections.Generic;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedEventHook : MonoBehaviour
    {
        #region FIELDS
        // To store the bundle data
        [SerializeField, SerializeReference, HideInInspector]
        private LBSEventHooker eventHooker;

        [SerializeField]
        private UnityEvent onCompleteEvent = new();

        [SerializeField]
        private UnityEvent onStayEvent = new();

        [SerializeField]
        private UnityEvent onExitEvent = new();

        [SerializeField]
        private UnityEvent onEnterEvent = new();

        [SerializeField]
        private UnityEvent OnDeathEvent = new();

        [SerializeField]
        private UnityEvent OnInteract = new();
        #endregion

        #region METHODS
        private void Start()
        {
           // AssignEvents(eventHooker);
        }

        internal void AssignEvents(LBSEventHooker eventHooker)
        {
            this.eventHooker ??= eventHooker;
            if(this.eventHooker is null)
            { 
                Debug.LogErrorFormat("LBSGeneratedEventHook on GameObject {0} has no EventHooker assigned!", gameObject.name); 
            }

            onCompleteEvent ??= new UnityEvent();
            onStayEvent ??= new UnityEvent();
            onExitEvent ??= new UnityEvent();
            onEnterEvent ??= new UnityEvent();
            OnDeathEvent ??= new UnityEvent();
            OnInteract ??= new UnityEvent();

            foreach (UnityActionStored entry in eventHooker.RegisteredActions)
            {
                UnityAction completeAction = entry.MakeAction(eventHooker.Target);
#if UNITY_EDITOR
                if (completeAction != null)
                {
                    switch (entry.eventType)
                    {
                        case LBSEventType.TriggerEnter:
                            UnityEventTools.AddPersistentListener(onEnterEvent, completeAction);
                            break;
                        case LBSEventType.TriggerExit:
                            UnityEventTools.AddPersistentListener(onExitEvent, completeAction);
                            break;
                        case LBSEventType.TriggerStay:
                            UnityEventTools.AddPersistentListener(onStayEvent, completeAction);
                            break;
                        case LBSEventType.Interact:
                            UnityEventTools.AddPersistentListener(OnInteract, completeAction);
                            break;
                        case LBSEventType.Destroy:
                            UnityEventTools.AddPersistentListener(OnDeathEvent, completeAction);
                            break;
                        case LBSEventType.Complete:
                            UnityEventTools.AddPersistentListener(onCompleteEvent, completeAction);
                            break;
                        default:
                            break;
                    }
                   
                }
#else
                onCompleteEvent.AddListener(entry.MakeAction(paramNode.Data.Target));
#endif
            }
        }

        internal void BroadcastOnComplete() => onCompleteEvent?.Invoke();
        internal void BroadcastOnEnter() => onEnterEvent?.Invoke();
        internal void BroadcastOnExit() => onExitEvent?.Invoke();
        internal void BroadcastOnStay() => onStayEvent?.Invoke();
        internal void BroadcastOnDeathEvent() => OnDeathEvent?.Invoke();
        internal void BroadcastOnInteractEvent() => OnInteract?.Invoke();

        internal void AssignTriggerEvents(List<TileTrigger> triggers)
        {
            foreach (TileTrigger trigger in triggers)
            {
                AssignEvents(trigger._eventHooker);
            }
        }

        #endregion
    }
}