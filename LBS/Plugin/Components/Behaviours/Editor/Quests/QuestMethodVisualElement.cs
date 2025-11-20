using System.Linq;
using System.Reflection;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public partial class QuestMethodVisualElement : VisualElement
    {
        private readonly LBSCustomButton _button;
        
        public QuestMethodVisualElement()
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestMethodVisualElement");
            visualTree.CloneTree(this);
            _button = this.Q<LBSCustomButton>("Button");
        }

        public void AddListener((GameObject, Component, MethodInfo) methodInfo, QuestActionData actionData)
        {
            _button.text = $"{methodInfo.Item3.Name}";
            _button.clicked += () =>
            {
                // during generation we must check that this event is still valid scene wise
                UnityActionStored entryKey = new(methodInfo);
                if(actionData.RegisteredActions.Contains(entryKey)) return;
                actionData.RegisteredActions.Add(entryKey);

            };
        }
        
        public void RemoveListener((GameObject, Component, MethodInfo) methodInfo, QuestActionData actionData)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            _button.text = $"{method.Name}";
            _button.clicked += () =>
            {
                UnityActionStored entryKey = new(methodInfo);
                foreach (UnityActionStored t in actionData.RegisteredActions.ToList())
                {
                    UnityActionStored entry = t;
                    if (entry.componentName == comp.GetType().Name &&
                        entry.methodName == method.Name &&
                        entry.objectName == target.name)
                    {
                        actionData.RegisteredActions.Remove(entryKey);
                    }
                }
            };
        }
    }
}