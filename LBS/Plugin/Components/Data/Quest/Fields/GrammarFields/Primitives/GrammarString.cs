using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("string")]
    public class GrammarString : GrammarField<string>
    {
        public override Type PrimitiveType => typeof(GrammarString);
    }

    [Serializable]
    [GrammarField("List.string")]
    public class GrammarStringList : GrammarListField<GrammarString>
    {
        public override Type PrimitiveType => typeof(GrammarString);
    }
}
