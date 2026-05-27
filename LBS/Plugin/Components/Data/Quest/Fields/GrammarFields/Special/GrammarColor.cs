using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("color")]
    public class GrammarColor : GrammarField<Color>
    {
        public override Type PrimitiveType => typeof(GrammarColor);
        public override bool IsValid() => true;
    }

    [Serializable]
    [GrammarField("List.color")]
    public class GrammarColorList : GrammarListField<GrammarColor>
    {
        public override Type PrimitiveType => typeof(GrammarColor);
    }
}
