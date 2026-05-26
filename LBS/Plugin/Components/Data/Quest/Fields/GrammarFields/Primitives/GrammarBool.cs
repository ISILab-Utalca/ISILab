using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("bool")]
    public class GrammarBool : GrammarField<bool>
    {
        public override Type PrimitiveType => typeof(GrammarBool);

        public override bool IsValid() => true;
    }

    [Serializable]
    [GrammarField("List.bool")]
    public class GrammarBoolList : GrammarListField<GrammarBool>
    {
        public override Type PrimitiveType => typeof(GrammarBool);

    }
}
