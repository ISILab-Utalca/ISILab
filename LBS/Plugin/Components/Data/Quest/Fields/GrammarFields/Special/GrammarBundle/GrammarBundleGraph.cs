using ISILab.LBS.Components;
using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("ref")]
    public class GrammarObject : GrammarBundleField<BundleTargetGraph>
    {
        public override Type PrimitiveType => typeof(GrammarObject);

        public override void SetValue(object newValue)
        {
            if (newValue is BundleTargetGraph target)
            {
                value = target;
            }
        }

        public override void SetObjectBundle(object[] objs)
        {
            base.SetObjectBundle(objs);
        }

        public override object GetValue() => value;
    }

    [Serializable]
    [GrammarField("List.ref")]
    public class GrammarObjectList : GrammarListField<GrammarObject>
    {
        public override Type PrimitiveType => typeof(GrammarObject);
    }

}
