using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("area")]
    public class GrammarArea : GrammarField<Rect>
    {
        public override Type PrimitiveType => typeof(Rect);
    }

    [Serializable]
    [GrammarField("List.area")]
    public class GrammarAreaList : GrammarListField<GrammarArea>
    {
        public override Type PrimitiveType => typeof(GrammarArea);
    }

}
