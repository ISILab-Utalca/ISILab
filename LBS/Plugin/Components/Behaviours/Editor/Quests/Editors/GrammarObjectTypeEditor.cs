using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarObjectType))]
    public class GrammarObjectTypeEditor : GrammarFieldEditor
    {
        public GrammarObjectTypeEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarObjectTypeEditor");
            visualTree.CloneTree(content);

            var pbg = this.Q<PickerBundleType>();

            pbg.OnBundlePicked = (layer, tile) =>
            {
                var bt = new BundleTarget(tile);
                pbg.SetLayerTarget(bt);
                (target as GrammarObjectType).SetValue(bt);
            };

            (target as GrammarField).Refresh = () =>
            {
                string guid = (string)(target as GrammarObjectType).GetValue();
                BundleTarget bundleTarget = new BundleTarget(LBSAssetMacro.LoadAssetByGuid<Bundle>(guid));
                if (bundleTarget.IsValid())
                    pbg.SetLayerTarget(bundleTarget, true);
            };

            return this;
        }
    }
}