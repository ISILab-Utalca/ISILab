using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace ISILab.LBS.Plugin.Components.Data
{

    public enum LBSEventType
    {
        Unassigned, // default value throws exception 
        TriggerEnter,
        TriggerExit,
        TriggerStay,
        Interact,
        Destroy,
        Complete
    }

    /// <summary>
    /// Meant to store necessary data to create Unity Actions.
    /// Has function to generate a UnityAction
    /// </summary>
    [Serializable]
    public struct UnityActionStored : IEquatable<UnityActionStored>
    {
        [SerializeField]
        public string objectName;
        [SerializeField]
        public string componentName;
        [SerializeField]
        public string methodName;
        [SerializeField]
        public LBSEventType eventType;


        public UnityAction MakeAction(GameObject go)
        {
            if (go == null || go.name != objectName) return null;

            foreach (MonoBehaviour comp in go.GetComponents<MonoBehaviour>())
            {
                if (comp == null || comp.GetType().Name != componentName) continue;

                var methods = comp.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != methodName) continue;
                    // Must be public void
                    if (method.ReturnType != typeof(void)) continue;
                    if (method.GetParameters().Length != 0) continue;

                    // Build UnityAction delegate
                    return (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), go.GetComponent(comp.GetType()), method);
                }
            }

            return null;
        }


        public UnityActionStored((GameObject, Component, MethodInfo, LBSEventType) actionInfo)
        {
            (GameObject target, Component comp, MethodInfo method, LBSEventType eventType) = actionInfo;

            objectName = target.name;
            componentName = comp.GetType().Name;
            methodName = method.Name;
            this.eventType = eventType;
        }

        public bool Equals(UnityActionStored other)
        {
            return objectName == other.objectName && componentName == other.componentName && methodName == other.methodName && eventType == other.eventType;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityActionStored other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(objectName, componentName, methodName, eventType);
        }
    }
}