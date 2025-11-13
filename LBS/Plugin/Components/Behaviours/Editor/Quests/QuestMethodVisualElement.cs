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

        public void SetData((GameObject, Component, MethodInfo) methodInfo, BaseQuestNodeData nodeData)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            button.text = $"{method.Name}";
            button.clicked += () =>
            {
                UnityAction action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), target.GetComponent(comp.GetType()), method);
                // during generation we must check that this event is still valid scene wise
                nodeData.OnCompleteEvent.AddListener(action);
                
            };
        }
    }
}