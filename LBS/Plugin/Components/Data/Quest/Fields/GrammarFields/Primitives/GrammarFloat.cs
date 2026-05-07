using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("float")]
    public class GrammarFloat : GrammarField<float>
    {
        public override Type PrimitiveType => typeof(GrammarFloat);
    }


    [Serializable]
    [GrammarField("List.float")]
    public class GrammarFloatList : GrammarListField<GrammarFloat>
    {
        public override Type PrimitiveType => typeof(GrammarFloat);
    }
}
