using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class DynamicFoldout : VisualElement
    {
        ClassDropDown dropdown;
        VisualElement content;

        private object data;

        public Action OnRemoved;

        static readonly VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ClassFoldout");

        public object Data
        {
            get => data;
            set => SetInfo(value);
        }

        public string Label
        {
            get => dropdown.label;
            set 
            {
                // Changing label 
                dropdown.UnregisterValueChangedCallback(ApplyChoice);
                dropdown.label = value;
                dropdown.RegisterValueChangedCallback(ApplyChoice);
            }
        }

        public Action OnChoiceSelection;

        public DynamicFoldout()
        {
            visualTree.CloneTree(this);
        }
        
        public DynamicFoldout(Type type, params Func<object, bool>[] typeFilters)
        {
            visualTree.CloneTree(this);

            bool filtered = typeFilters.Length > 0;

            var foldout = this.Q<ClassFoldout>();
            if(filtered)
            {
                foldout.ReplaceDropdown(new FilteredDropdown(typeFilters));
                dropdown = foldout.Q<FilteredDropdown>();
            }
            else
            {
                dropdown = foldout.Q<ClassDropDown>();
            }
            dropdown.RegisterValueChangedCallback(ApplyChoice);
            dropdown.Type = type;
            content = foldout.Q<VisualElement>(name: "unity-content");
        }

        public DynamicFoldout(params Func<bool>[] predicates)
        {
            visualTree.CloneTree(this);


        }

        void UpdateView(Type type, object data)
        {

            if (data == null)
                return;

            content.Clear();

            var veType = LBS_Editor.GetEditor(type);

            if (veType == null)
                return;

            var ve = Activator.CreateInstance(veType, new object[] { data }) as LBSCustomEditor;

            if (!(ve is VisualElement))
            {
                throw new Exception("[ISI Lab] " + ve.GetType() + " is not a VisualElement ");
            }

            content.Add(ve);
        }

        public void SetInfo(object data)
        {
            if (data != null)
            {
                dropdown.SetValueWithoutNotify(data.GetType().Name);
                this.data = data;
                //OnChoiceSelection?.Invoke();
                UpdateView(this.data?.GetType(), this.data);
            }
        }

        public void ApplyChoice(ChangeEvent<string> e)
        {
            data = dropdown.GetChoiceInstance();
            OnChoiceSelection?.Invoke();
            UpdateView(data?.GetType(), data);
        }

        public void UpdateDropdown()
        {
            //Debug.Log($"UpdateDropdown ({(data is not null ? data.GetType() : "Empty")})");
            dropdown.UpdateOptions();
        }

        public new void RemoveFromHierarchy()
        {
            OnRemoved?.Invoke();
            base.RemoveFromHierarchy();
        }


    }
}