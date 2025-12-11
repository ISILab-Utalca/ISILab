using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomEventHooker : VisualElement
    {
        #region VIEW FIELDS
        private LBSCustomObjectField _gameObjectSelector;
        private ListView _selectedMethodsList;
        private ListView _availableMethodsList;
        #endregion

        #region FIELDS
        QuestActionData actionData;
        private readonly List<(GameObject, Component, MethodInfo)> _availableMethods = new();
        #endregion

        #region PROPERTIES
        public LBSCustomObjectField Selector
        {
            get => _gameObjectSelector;
            set => _gameObjectSelector = value;
        }

        public QuestActionData ActionData
        {
            get => actionData;
            set
            {
                actionData = value;
               //?? RefreshMethodList();
            }
        }

        #endregion



        public LBSCustomEventHooker()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EventHooker");
            visualTree.CloneTree(this);

            _gameObjectSelector = this.Q<LBSCustomObjectField>("GameObjectSelector");
            _availableMethodsList = this.Q<ListView>("AvailableMethodsList");
            _selectedMethodsList = this.Q<ListView>("SelectedMethodsList");
        }

        internal void RefreshMethodList()
        {
            List<(GameObject, Component, MethodInfo)> selectedMethods = new();

            GameObject gameObject = actionData?.Target;

            // reset on refresh
            _availableMethodsList.itemsSource = null;
            _selectedMethodsList.itemsSource = null;
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();

            if (!gameObject) return;

            #region Available Methods

            _availableMethods.Clear();

            foreach (MonoBehaviour comp in gameObject.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;

                foreach (MethodInfo method in comp.GetType().GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (method.ReturnType == typeof(void) && !method.GetParameters().Any())
                        _availableMethods.Add((gameObject, comp, method));
                }
            }

            _availableMethodsList.itemsSource = _availableMethods;
            _availableMethodsList.makeItem = () => new QuestMethodVisualElement();
            _availableMethodsList.bindItem = (element, i) =>
            {
                GetRegisteredMethods(actionData, selectedMethods);

                if (i < 0 || i >= _availableMethods.Count)
                    return;

                var availableMethod = _availableMethods[i];
                QuestMethodVisualElement vm = (QuestMethodVisualElement)element;

                vm.SetEnabled(!selectedMethods.Contains(availableMethod));
                vm.AddListener(availableMethod, actionData);
                vm.Q<Button>().clicked += RefreshMethodList;
            };

            #endregion

            #region Selected Methods

            selectedMethods.Clear();
            GetRegisteredMethods(actionData, selectedMethods);

            _selectedMethodsList.itemsSource = selectedMethods;
            _selectedMethodsList.makeItem = () => new QuestMethodVisualElement();
            _selectedMethodsList.bindItem = (element, i) =>
            {
                if (i < 0 || i >= selectedMethods.Count)
                    return;

                QuestMethodVisualElement vm = (QuestMethodVisualElement)element;
                vm.RemoveListener(selectedMethods[i], actionData);
                vm.Q<Button>().clicked += RefreshMethodList;
            };

            #endregion

            // rebuild both at the end
            _availableMethodsList.Rebuild();
            _selectedMethodsList.Rebuild();
        }

        internal void SetSelectorTarget(GameObject target)
        {
            Selector.value = target;
        }

        private static void GetRegisteredMethods(QuestActionData actionData, List<(GameObject, Component, MethodInfo)> selectedMethods)
        {
            selectedMethods.Clear();
            foreach (UnityActionStored entry in actionData.RegisteredActions)
            {
                GameObject go = actionData.Target;
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
