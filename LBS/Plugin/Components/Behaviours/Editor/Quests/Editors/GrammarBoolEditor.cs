using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarBool))]
    public class GrammarBoolEditor : GrammarFieldEditor
    {
        public GrammarBoolEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarBoolEditor");
            visualTree.CloneTree(content);

            this.Q<LBSCustomToggle>().RegisterValueChangedCallback(evt =>
            {
                SetTargetValue(evt);
            });

            this.Q<LBSCustomToggle>().value = GetTargetValue<bool>();
            return this;
        }
    }
}