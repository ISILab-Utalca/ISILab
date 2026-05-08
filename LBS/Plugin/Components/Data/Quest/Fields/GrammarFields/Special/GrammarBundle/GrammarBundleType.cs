using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using System.Linq;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("refType")]
    public class GrammarObjectType : GrammarBundleField<string>
    {
        public override Type PrimitiveType => typeof(GrammarObjectType);

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
    }

    [Serializable]
    [GrammarField("List.refType")]
    public class GrammarTypeList : GrammarListField<GrammarObjectType>
    {
        public override Type PrimitiveType => typeof(GrammarObjectType);
    }
}
