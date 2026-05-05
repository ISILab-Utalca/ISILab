using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class FieldFloat : GrammarFieldEditor
    {
        public FieldFloat(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldFloat");
            visualTree.CloneTree(content);
            this.Q<LBSCustomFloatField>().RegisterValueChangedCallback((evt) =>
            {
                SetTargetValue(evt);
            });

            this.Q<LBSCustomFloatField>().value = GetTargetValue<float>();
            return this;
        }
    }
}