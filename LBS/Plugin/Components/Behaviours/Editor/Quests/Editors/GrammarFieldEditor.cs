using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class GrammarFieldEditor : LBSCustomEditor
    {
        private LBSCustomFoldout foldout;

        // field umxl are added here
        protected VisualElement content;

        public GrammarFieldEditor(object target) : base(target)
        {
            SetNewInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            target = paramTarget as GrammarField;
        }

        public void SetNewInfo(object paramTarget)
        {
            SetInfo(paramTarget);
            CreateVisualElement();
        }

        protected override VisualElement CreateVisualElement()
        {
            Clear();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarFieldEditor");
            visualTree.CloneTree(this);

            foldout = this.Q<LBSCustomFoldout>();
            var name = (target as GrammarField)?.name;
            foldout.text = name;

            content = this.Q<VisualElement>("Content");
            return this;
        }

        protected void SetTargetValue<T>(ChangeEvent<T> evt)
        {
            if (target == null) 
                return;
            (target as GrammarField).SetValue(evt.newValue);
        }

        protected T GetTargetValue<T>()
        {
            if (target is GrammarField field)
            {
                object value = field.GetValue();

                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    UnityEngine.Debug.LogError(
                        $"[Grammar] Cannot convert {value?.GetType()} to {typeof(T)}");
                }
            }

            return default;
        }
    }
}