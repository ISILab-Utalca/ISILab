using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldInt : GrammarFieldEditor
    {
        public FieldInt(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldInt");
            visualTree.CloneTree(this);
            this.Q<LBSCustomIntField>().RegisterValueChangedCallback((evt) =>
            {
                SetTargetValue(evt);
            });
            
            this.Q<LBSCustomIntField>().value = GetTargetValue<int>();
            return this;
        }
    }
}