using ISILab.LBS.Plugin.Components.Data;
using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("hook")]
    public class GrammarEventHook : GrammarField<LBSEventHooker>
    {
        public override Type PrimitiveType => typeof(LBSEventHooker);
    }
    [Serializable]
    [GrammarField("List.hook")]
    public class GrammarEventHookList : GrammarListField<GrammarEventHook>
    {
        public override Type PrimitiveType => typeof(GrammarEventHook);
    }
}
