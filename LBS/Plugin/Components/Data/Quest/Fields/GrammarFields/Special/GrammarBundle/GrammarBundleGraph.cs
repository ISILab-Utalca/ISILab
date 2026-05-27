using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("tile")]
    public class GrammarBundleGraph : GrammarBundleField<BundleTargetGraph>
    {
        public override Type PrimitiveType => typeof(GrammarBundleGraph);

        public override void SetValue(object newValue)
        {
            if (newValue is BundleTargetGraph target)
            {
                value = target;
            }
        }

        public override bool IsValid() => value != null && value.IsValid();
        public override LBSLog GetValidStateLog() =>
            IsValid() ? base.GetValidStateLog() : new LBSLog($"{name}: Bundle target not assigned!", UnityEngine.LogType.Error);
        public override void SetObjectBundle(object[] objs)
        {
            base.SetObjectBundle(objs);
        }

        public override object GetValue() => value;

        public override Bundle GetBundle() => value?.TileBundleGroup?.BundleData?.Bundle;
    }

    [Serializable]
    [GrammarField("List.tile")]
    public class GrammarBundleGraphList : GrammarListField<GrammarBundleGraph>
    {
        public override Type PrimitiveType => typeof(GrammarBundleGraph);
    }

}
