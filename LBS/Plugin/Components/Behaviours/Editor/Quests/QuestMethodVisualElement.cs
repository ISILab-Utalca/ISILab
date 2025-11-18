using System;
using System.Reflection;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public partial class QuestMethodVisualElement : VisualElement
    {
        private LBSCustomButton button;
        
        public QuestMethodVisualElement()
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestMethodVisualElement");
            visualTree.CloneTree(this);
            button = this.Q<LBSCustomButton>("Button");
        }

        public void AddListener((GameObject, Component, MethodInfo) methodInfo, BaseQuestNodeData nodeData)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            button.text = $"{method.Name}";
            button.clicked += () =>
            {
                UnityAction action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), target.GetComponent(comp.GetType()), method);
                // during generation we must check that this event is still valid scene wise
                UnityActionStored entryKey = new(methodInfo, action);
                
                if(nodeData.RegisteredListeners.Contains(entryKey)) return;
                nodeData.RegisteredListeners.Add(entryKey);

            };
        }
        
        public void RemoveListener((GameObject, Component, MethodInfo) methodInfo, BaseQuestNodeData nodeData)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            button.text = $"{method.Name}";
            button.clicked += () =>
            {
                UnityActionStored entryKey = new(methodInfo);
                foreach (UnityActionStored t in nodeData.RegisteredListeners)
                {
                    UnityActionStored entry = t;
                    if (entry.componentName == comp.GetType().Name &&
                        entry.methodName == method.Name &&
                        entry.objectName == target.name)
                    {
                        UnityAction action = entry.action;
                        nodeData.OnCompleteEvent.RemoveListener(action);
                        nodeData.RegisteredListeners.Remove(entryKey);
                    }
                }
            };
        }
    }
}