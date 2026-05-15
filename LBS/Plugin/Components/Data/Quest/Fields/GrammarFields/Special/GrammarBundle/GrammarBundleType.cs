using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using System.Linq;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("type")]
    public class GrammarBundleType : GrammarBundleField<string>
    {
        public override Type PrimitiveType => typeof(GrammarBundleType);

        public override void SetValue(object newValue)
        {
            if (newValue is BundleTargetGraph target)
            {
                value = target.GUID;
            }
        }

        // expect to pass just the bundle and extract type
        public override void SetObjectBundle(object[] objs)
        {
            if(objs == null || objs.Length == 0) return;
            Bundle bundle = objs.FirstOrDefault() as Bundle;

            SetValue(LBSAssetMacro.GetGuidFromAsset(bundle));
        }

        public override Bundle GetBundle() => LBSAssetMacro.LoadAssetByGuid<Bundle>(value);
    }

    [Serializable]
    [GrammarField("List.type")]
    public class GrammarBundleTypeList : GrammarListField<GrammarBundleType>
    {
        public override Type PrimitiveType => typeof(GrammarBundleType);
    }
}
