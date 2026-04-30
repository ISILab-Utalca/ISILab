using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarString))]
    public class GrammarStringEditor : GrammarFieldEditor
    {
        public GrammarStringEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarStringEditor");
            visualTree.CloneTree(content);

            this.Q<LBSCustomTextField>().RegisterValueChangedCallback(evt =>
            {
                SetTargetValue(evt);
            });

            this.Q<LBSCustomTextField>().value = GetTargetValue<string>();
            return this;
        }
    }
}