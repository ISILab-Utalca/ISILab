using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.MapTools.Generators;
using System;
using System.Collections.Generic;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class LBSGeneratedPatrol : MonoBehaviour
{
    [SerializeField]
    private List<Vector3> points;


    public List<Vector3> Points
    {
        get => points;
        set => points = value;
    }

    public LBSGeneratedPatrol(List<Vector3> points)
    {
        Points = points;
    }

    internal void AssignPoints(List<Vector2> points, bool hitSurface = false)
    {
        foreach (Vector2 point in points)
        {
            Vector3 worldPos = new Vector3(point.x, 0f, point.y);

            if (hitSurface)
            {
                var fromVector = worldPos + Vector3.up * 1000f;
                Ray ray = new Ray(fromVector, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) worldPos.y = hit.point.y;
            }

            Points.Add(worldPos);
        }
    }

}


[Serializable]
public class LBSGeneratedEventHook : MonoBehaviour
{
    // To store the bundle data
    [SerializeReference]
    private LBSEventHooker eventHooker;


    [SerializeField, SerializeReference]
    private UnityEvent onCompleteEvent = new();

    [SerializeField, SerializeReference]
    private UnityEvent onEnterEvent = new();

    [SerializeField, SerializeReference]
    private UnityEvent onLeaveEvent = new();

    [SerializeField, SerializeReference]
    private UnityEvent OnDeathEvent = new();

    [SerializeField, SerializeReference]
    private UnityEvent OnInteract = new();

    public LBSGeneratedEventHook(LBSEventHooker eventHooker) => AssignEvents(eventHooker);

    internal void AssignEvents(LBSEventHooker eventHooker)
    {
        this.eventHooker ??= eventHooker;

        foreach (UnityActionStored entry in eventHooker.RegisteredActions)
        {
            UnityAction completeAction = entry.MakeAction(eventHooker.Target);
#if UNITY_EDITOR
            if (completeAction != null)
            {
                UnityEventTools.AddPersistentListener(onCompleteEvent, completeAction);
            }
            #else
                onCompleteEvent.AddListener(entry.MakeAction(paramNode.Data.Target));
            #endif
        }
    }

    internal void BroadcastOnComplete() => onCompleteEvent?.Invoke();
    internal void BroadcastOnEnter() => onEnterEvent?.Invoke();
    internal void BroadcastOnLeave() => onLeaveEvent?.Invoke();
    internal void BroadcastOnDeathEvent() => OnDeathEvent?.Invoke();
    internal void BroadcastOnInteractEvent() => OnInteract?.Invoke();

    internal void AssignTriggerEvents(List<TileTrigger> triggers)
    {
        foreach (TileTrigger trigger in triggers)
        {
            AssignEvents(trigger._eventHooker);
        }
    }
}
