using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomEventHooker : VisualElement
    {
        #region VIEW FIELDS
        private static VisualTreeAsset visualTree;
        private LBSCustomObjectField _targetField;
        private ListView _selectedMethodsList = new();
        private ListView _availableMethodsList = new();
    
        #endregion

        #region FIELDS
        LBSEventHooker hooker;
        // This field is set via code only (not exposed to visual elements)
        private LBSEventType eventType = LBSEventType.Unassigned;
        private readonly List<(GameObject, Component, MethodInfo)> _availableMethods = new();
        #endregion

        #region PROPERTIES
        public LBSCustomObjectField Selector
        {
            get => _targetField;
            set => _targetField = value;
        }

        public LBSEventHooker Hooker
        {
            get => hooker;
            set 
            {
                hooker = value;
                ChangeTargetField(hooker?.Target);
            }
        }

        public LBSEventType EventType
        {
            get => (LBSEventType)eventType;
            set
            {
                eventType = value;
                // assuming unity actions already exists we ought to update them as well
                hooker?.EventTypeChanged(eventType);
           
            }
        }

        #endregion



        public LBSCustomEventHooker()
        {
            if (visualTree is null) visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EventHooker");

            visualTree.CloneTree(this);

            _targetField = this.Q<LBSCustomObjectField>("GameObjectSelector");
            _targetField.RegisterValueChangedCallback(evt =>
            {
                GameObject obj = null;
                if (evt.newValue is not null) obj = evt.newValue as GameObject;
                ChangeTargetField(obj);
            });
 

            _availableMethodsList = this.Q<ListView>("AvailableMethodsList");
            _selectedMethodsList = this.Q<ListView>("SelectedMethodsList");
            RefreshMethodList();
        }

        private void ChangeTargetField(GameObject newTarget)
        {
            if (_targetField is null || hooker is null) return;

            hooker.Target = newTarget;
            _targetField.SetValueWithoutNotify(newTarget);

            RefreshMethodList();
        }


        internal void RefreshMethodList()
        {
            if (visualTree is null) return;
            List<(GameObject, Component, MethodInfo)> selectedMethods = new();

            // reset on refresh
            _availableMethodsList.itemsSource = null;
            _selectedMethodsList.itemsSource = null;
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();
            
            _availableMethods.Clear();
            _selectedMethodsList.Clear();

            var Target = hooker?.Target;

            if (Target is null) return;

            #region Available Methods

      
            foreach (MonoBehaviour comp in Target.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;

                foreach (MethodInfo method in comp.GetType().GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (method.ReturnType == typeof(void) && !method.GetParameters().Any())
                        _availableMethods.Add((Target, comp, method));
                }
            }

            _availableMethodsList.itemsSource = _availableMethods;
            _availableMethodsList.makeItem = () => new EventHookEntryView();
            _availableMethodsList.bindItem = (element, i) =>
            {
                GetRegisteredMethods(Hooker, selectedMethods);

                if (i < 0 || i >= _availableMethods.Count)
                    return;

                (GameObject, Component, MethodInfo) availableMethod = _availableMethods[i];
                EventHookEntryView vm = (EventHookEntryView)element;

                vm.SetEnabled(!selectedMethods.Contains(availableMethod));

                (GameObject, Component, MethodInfo, LBSEventType) listenerMethod = (availableMethod.Item1, availableMethod.Item2, availableMethod.Item3, eventType);
                vm.AddListener(listenerMethod, Hooker);
                vm.Q<Button>().clicked += RefreshMethodList;
            };

            #endregion

            #region Selected Methods

            selectedMethods.Clear();
            GetRegisteredMethods(hooker, selectedMethods);

            _selectedMethodsList.itemsSource = selectedMethods;
            _selectedMethodsList.makeItem = () => new EventHookEntryView();
            _selectedMethodsList.bindItem = (element, i) =>
            {
                if (i < 0 || i >= selectedMethods.Count)
                    return;

                EventHookEntryView vm = (EventHookEntryView)element;
                var selectedMethod = selectedMethods[i];
                (GameObject, Component, MethodInfo, LBSEventType) listenerMethod = (selectedMethod.Item1, selectedMethod.Item2, selectedMethod.Item3, eventType);
                vm.RemoveListener(listenerMethod, hooker);
                vm.Q<Button>().clicked += RefreshMethodList;
            };

            #endregion

            // rebuild both at the end
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();
        }

        private static void GetRegisteredMethods(LBSEventHooker hooker, List<(GameObject, Component, MethodInfo)> selectedMethods)
        {
            selectedMethods.Clear();
            foreach (UnityActionStored entry in hooker.RegisteredActions)
            {
                GameObject go = hooker.Target;
                if (go is null || go.name != entry.objectName) continue;

                foreach (MonoBehaviour comp in go.GetComponents<MonoBehaviour>())
                {
                    if (comp == null || comp.GetType().Name != entry.componentName) continue;
                    MethodInfo method = comp.GetType().GetMethod(entry.methodName);
                    selectedMethods.Add((go, comp, method));
                }
            }
        }
    }
}
