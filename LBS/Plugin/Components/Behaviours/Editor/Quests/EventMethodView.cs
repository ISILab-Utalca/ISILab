using System.Linq;
using System.Reflection;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.Components.Data;

namespace ISILab.LBS.VisualElements
{
    public partial class EventMethodView : VisualElement
    {
        private readonly LBSCustomButton _button;
        
        public EventMethodView()
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EventMethodView");
            visualTree.CloneTree(this);
            _button = this.Q<LBSCustomButton>("Button");
        }
        /*
        public void AddListener((GameObject, Component, MethodInfo, LBSEventType) methodInfo, QuestActionData actionData)
        {
            _button.text = $"{methodInfo.Item3.Name}";
            _button.clicked += () =>
            {
                // during generation we must check that this event is still valid scene wise
                UnityActionStored entryKey = new UnityActionStored(methodInfo);
                if(actionData.EventHooker.RegisteredActions.Contains(entryKey)) return;
                actionData.EventHooker.RegisteredActions.Add(entryKey);

            };
        }
        
        public void RemoveListener((GameObject, Component, MethodInfo, LBSEventType) methodInfo, QuestActionData actionData)
        {
            (GameObject target, Component comp, MethodInfo method, LBSEventType eventType) = methodInfo;
            _button.text = $"{method.Name}";
            _button.clicked += () =>
            {
                UnityActionStored entryKey = new UnityActionStored(methodInfo);
                foreach (UnityActionStored t in actionData.EventHooker.RegisteredActions.ToList())
                {
                    UnityActionStored entry = t;
                    if (entry.componentName == comp.GetType().Name &&
                        entry.methodName == method.Name &&
                        entry.objectName == target.name)
                    {
                        actionData.EventHooker.RegisteredActions.Remove(entryKey);
                    }
                }
            };
        }
        */
    }
}