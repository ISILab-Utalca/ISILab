using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Editor;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldReferenceType : GrammarFieldEditor
    {
        public FieldReferenceType(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldReferenceType");
            visualTree.CloneTree(this);
            this.Q<PickerBundleType>().OnClicked += () => {
                UnityEngine.Debug.Log("Clicked a type ref");
            };

            return this;
        }
    }
}