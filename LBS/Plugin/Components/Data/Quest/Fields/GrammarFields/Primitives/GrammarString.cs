using ISILab.LBS.Plugin.Core.Settings;
using System;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("string")]
    public class GrammarString : GrammarField<string>
    {
        public override Type PrimitiveType => typeof(GrammarString);
        public override bool IsValid() => value != string.Empty;
        public override LBSLog GetValidStateLog() =>
            IsValid() ? base.GetValidStateLog() : new LBSLog($"{name}: String is empty!", UnityEngine.LogType.Error);

    }

    [Serializable]
    [GrammarField("List.string")]
    public class GrammarStringList : GrammarListField<GrammarString>
    {
        public override Type PrimitiveType => typeof(GrammarString);
    }
}
