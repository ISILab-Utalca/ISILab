using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("bool")]
    public class GrammarBool : GrammarField<bool>
    {
        public override Type PrimitiveType => typeof(GrammarBool);
    }

    [Serializable]
    [GrammarField("List.bool")]
    public class GrammarBoolList : GrammarListField<GrammarBool>
    {
        public override Type PrimitiveType => typeof(GrammarBool);
    }
}
