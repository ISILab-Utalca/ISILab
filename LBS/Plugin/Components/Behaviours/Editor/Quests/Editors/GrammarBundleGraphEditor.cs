using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarBundleGraph))]
    public class GrammarBundleGraphEditor : GrammarFieldEditor
    {
        public GrammarBundleGraphEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarBundleGraphEditor");
            visualTree.CloneTree(content);

            var pbg = this.Q<PickerBundleGraph>();

            pbg.OnBundlePicked = (layer, tile) =>
            {
                var btg = new BundleTargetGraph(layer, tile);
                pbg.SetLayerTarget(btg);
                (target as GrammarBundleGraph).SetValue(btg);
            };

            (target as GrammarField).Refresh = () =>
            {
                BundleTarget btg = (target as GrammarBundleGraph).GetValue() as BundleTargetGraph;
                if (btg.IsValid())
                    pbg.SetLayerTarget(btg, true);
            };

            return this;
        }
    }
}