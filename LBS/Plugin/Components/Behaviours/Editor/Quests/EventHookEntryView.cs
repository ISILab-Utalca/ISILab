using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Data;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace ISILab.LBS.VisualElements
{
    public partial class EventHookEntryView : VisualElement
    {
        #region FIELDs
        private readonly LBSCustomButton _button;
        private readonly LBSCustomToggleField triggerOnce;

        private UnityActionStored entryKey;
        private LBSEventHooker hooker;
        #endregion

        #region PROPERTIES
        public LBSCustomToggleField TriggerOnce => triggerOnce;

        #endregion

        #region CONSTRUCTOR

        public EventHookEntryView(bool ChangeTrigger = false, bool TriggerOnce = true)
        {
            Clear();
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EventHookEntryView");
            visualTree.CloneTree(this);
            _button = this.Q<LBSCustomButton>("Button");
            triggerOnce = this.Q<LBSCustomToggleField>("TriggerOnceToggle");

            triggerOnce.SetEnabled(ChangeTrigger);
            triggerOnce.style.display = ChangeTrigger ? DisplayStyle.Flex : DisplayStyle.None;

            triggerOnce.SetValueWithoutNotify(TriggerOnce);

            triggerOnce.RegisterValueChangedCallback(evt =>
            {
                triggerOnce.SetValueWithoutNotify(evt.newValue);

                if (hooker is null || !hooker.RegisteredActions.Any()) return;

                for (int i = 0; i < hooker.RegisteredActions.Count; i++)
                {
                    UnityActionStored action = hooker.RegisteredActions[i];
                    if (action.Equals(entryKey))
                    {
                        action.TriggerOnce = evt.newValue;
                    }
                }
            });

        }

        #endregion

        #region METHODS

        public void AddListener((GameObject, Component, MethodInfo, LBSEventType, bool) methodInfo, LBSEventHooker hooker)
        {
            this.hooker = hooker;
            _button.text = $"{methodInfo.Item3.Name}";
            _button.clicked += () =>
            {
                // during generation we must check that this event is still valid scene wise
                this.hooker = hooker;
                methodInfo.Item5 = triggerOnce.value;
                entryKey = new UnityActionStored(methodInfo);
                if(hooker.RegisteredActions.Contains(entryKey)) return;
                hooker.RegisteredActions.Add(entryKey);

            };
        }
        
        /*
        public void RemoveListener()
        {
            if(hooker == null || !hooker.RegisteredActions.Any()) return;
            hooker.RegisteredActions.Remove(entryKey);
        }*/


        public void RemoveListener((GameObject, Component, MethodInfo, LBSEventType, bool) methodInfo, LBSEventHooker hooker)
        {
            (GameObject target, Component comp, MethodInfo method, LBSEventType eventType, bool triggerOnce) = methodInfo;
            _button.text = $"{method.Name}";
            _button.clicked += () =>
            {
                UnityActionStored entryKey = new UnityActionStored(methodInfo);
                foreach (UnityActionStored t in hooker.RegisteredActions.ToList())
                {
                    UnityActionStored entry = t;
                    if (entry.Equals(t))
                    {
                        hooker.RegisteredActions.Remove(entryKey);
                    }
                }
            };
        }

        #endregion
    }
}