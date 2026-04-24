using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldString : GrammarFieldEditor
    {
        public FieldString(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldString");
            visualTree.CloneTree(this);

            var stringField = this.Q<LBSCustomTextField>("Text");
            stringField.RegisterValueChangedCallback<string>(evt =>
            {
                if(target is GrammarString gs)
                {
                    gs.value = evt.newValue;
                }
            });
            return this;
        }
    }
}