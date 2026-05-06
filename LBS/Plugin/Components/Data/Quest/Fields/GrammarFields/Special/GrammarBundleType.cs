using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("refType")]
    public class GrammarObjectType : GrammarField<string>
    {
        public override Type PrimitiveType => typeof(GrammarObjectType);
    }

    [Serializable]
    [GrammarField("List.refType")]
    public class GrammarTypeList : GrammarListField<GrammarObjectType>
    {
        public override Type PrimitiveType => typeof(GrammarObjectType);
    }
}
