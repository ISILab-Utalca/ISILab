using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarFloat))]
    public class GrammarFloatEditor : GrammarFieldEditor
    {
        public GrammarFloatEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarFloatEditor");
            visualTree.CloneTree(content);
            this.Q<LBSCustomFloatField>().RegisterValueChangedCallback((evt) =>
            {
                SetTargetValue(evt);
            });

            (target as GrammarField).Refresh = () =>
            {
                this.Q<LBSCustomFloatField>().SetValueWithoutNotify(GetTargetValue<float>());
            };
            
            return this;
        }
    }
}