using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarInt))]
    public class GrammarIntEditor : GrammarFieldEditor
    {
        public GrammarIntEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarIntEditor");
            visualTree.CloneTree(content);
            this.Q<LBSCustomIntField>().RegisterValueChangedCallback((evt) =>
            {
                SetTargetValue(evt);
            });
            
            this.Q<LBSCustomIntField>().value = GetTargetValue<int>();
            return this;
        }
    }
}