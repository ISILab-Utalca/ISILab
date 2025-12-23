using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using System;
using System.Collections.Generic;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.EventSystems.EventTrigger;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedEventHook : MonoBehaviour
    {
        [Serializable]
        public class TriggerOnceEntry
        {
            public LBSEventType eventType;
            public List<UnityActionStored> actions = new();
        }

        #region FIELDS
        [SerializeField, HideInInspector]
        private List<TriggerOnceEntry> triggerOnceList = new();

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
                if(entry.TriggerOnce)AddTriggerOnceEvent(entry);

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

        internal void BroadcastEvent(LBSEventType eventType) => InvokeEvent(eventType);

        private void AddTriggerOnceEvent(UnityActionStored entry)
        {
            TriggerOnceEntry container = triggerOnceList.Find(e => e.eventType == entry.eventType);

            if (container == null)
            {
                container = new TriggerOnceEntry
                {
                    eventType = entry.eventType
                };
                triggerOnceList.Add(container);
            }

            container.actions.Add(entry);
        }



        private void InvokeEvent(LBSEventType eventType)
        {
            UnityEvent eventToInvoke = null;
            switch (eventType)
            {
                case LBSEventType.TriggerEnter:
                    eventToInvoke = onEnterEvent;
                    break;
                case LBSEventType.TriggerExit:
                    eventToInvoke = onExitEvent;
                    break;
                case LBSEventType.TriggerStay:
                    eventToInvoke = onStayEvent;
                    break;
                case LBSEventType.Interact:
                    eventToInvoke = OnInteract;
                    break;
                case LBSEventType.Destroy:
                    eventToInvoke = OnDeathEvent;
                    break;
                case LBSEventType.Complete:
                    eventToInvoke = onCompleteEvent;
                    break;
                default:
                    break;
            }

            if (eventToInvoke is null) return;
            eventToInvoke?.Invoke();

            TriggerOnceEntry container = triggerOnceList.Find(e => e.eventType == eventType);
            if (container == null)
                return;

            // 🔹 Remove persistent listeners AFTER invoke
            for (int i = eventToInvoke.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEngine.Object target = eventToInvoke.GetPersistentTarget(i);
                string method = eventToInvoke.GetPersistentMethodName(i);

                foreach (UnityActionStored entry in container.actions)
                {
                    bool SameName = entry.objectName == target.name;
                    bool SameMethod = entry.methodName == method;
                    if (SameName && SameMethod)
                    {
                        UnityEventTools.RemovePersistentListener(eventToInvoke, i);
                        break;
                    }
                }
            }

            triggerOnceList.Remove(container);
        }


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