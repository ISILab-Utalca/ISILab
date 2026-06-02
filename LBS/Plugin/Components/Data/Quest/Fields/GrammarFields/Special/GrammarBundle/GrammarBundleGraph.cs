using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("tile")]
    public class GrammarBundleGraph : GrammarBundleField<BundleTargetGraph>
    {
        // only assigned in the 3d generation when looking for the generated object in the BundleTargetGraph
        public GameObject objectRef;
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
            if (objs.Length == 0) return;
            objectRef = objs[0] as GameObject;
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
