using System.Linq;
using System.Reflection;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public partial class EventHookEntryView : VisualElement
    {
        private readonly LBSCustomButton _button;
        
        public EventHookEntryView()
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EventHookEntryView");
            visualTree.CloneTree(this);
            _button = this.Q<LBSCustomButton>("Button");
        }

        public void AddListener((GameObject, Component, MethodInfo) methodInfo, LBSEventHooker hooker)
        {
            _button.text = $"{methodInfo.Item3.Name}";
            _button.clicked += () =>
            {
                // during generation we must check that this event is still valid scene wise
                UnityActionStored entryKey = new(methodInfo);
                if(hooker.RegisteredActions.Contains(entryKey)) return;
                hooker.RegisteredActions.Add(entryKey);

            };
        }
        
        public void RemoveListener((GameObject, Component, MethodInfo) methodInfo, LBSEventHooker hooker)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            _button.text = $"{method.Name}";
            _button.clicked += () =>
            {
                UnityActionStored entryKey = new(methodInfo);
                foreach (UnityActionStored t in hooker.RegisteredActions.ToList())
                {
                    UnityActionStored entry = t;
                    if (entry.componentName == comp.GetType().Name &&
                        entry.methodName == method.Name &&
                        entry.objectName == target.name)
                    {
                        hooker.RegisteredActions.Remove(entryKey);
                    }
                }
            };
        }
    }
}