using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
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
            visualTree.CloneTree(content);

            var pbg = this.Q<PickerBundleGraph>();

            pbg.OnBundlePicked = (layer, tile) =>
            {
                var btg = new BundleTargetGraph(layer, tile);
                pbg.SetLayerTarget(btg);
                (target as GrammarObject).SetValue(btg);
            };

            BundleTarget btg = (target as GrammarObject).GetValue() as BundleTargetGraph;
            if (btg.IsValid())
                pbg.SetLayerTarget(btg);

            return this;
        }
    }
}