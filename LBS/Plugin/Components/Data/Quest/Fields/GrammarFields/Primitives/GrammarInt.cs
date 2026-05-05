using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("int")]
    public class GrammarInt : GrammarField<int>
    {
        public override Type PrimitiveType => typeof(GrammarInt);
    }

    [Serializable]
    [GrammarField("List.int")]
    public class GrammarIntList : GrammarListField<GrammarInt>
    {
        public override Type PrimitiveType => typeof(GrammarInt);
    }
}
