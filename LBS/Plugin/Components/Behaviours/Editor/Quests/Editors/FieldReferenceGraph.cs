using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldReferenceGraph : GrammarFieldEditor
    {
        public FieldReferenceGraph(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldReferenceGraph");
            visualTree.CloneTree(this);
            this.Q<PickerBundleGraph>().OnClicked += () => {
                UnityEngine.Debug.Log("Clicked a graph ref");
            };

            return this;
        }
    }
}