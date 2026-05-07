using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarObject))]
    public class GrammarObjectEditor : GrammarFieldEditor
    {
        public GrammarObjectEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarObjectEditor");
            visualTree.CloneTree(content);

            var pbg = this.Q<PickerBundleGraph>();

            pbg.OnBundlePicked = (layer, tile) =>
            {
                var btg = new BundleTargetGraph(layer, tile);
                pbg.SetLayerTarget(btg);
                (target as GrammarObject).SetValue(btg);
            };

            (target as GrammarField).Refresh = () =>
            {
                BundleTarget btg = (target as GrammarObject).GetValue() as BundleTargetGraph;
                if (btg.IsValid())
                    pbg.SetLayerTarget(btg, true);
            };

            return this;
        }
    }
}