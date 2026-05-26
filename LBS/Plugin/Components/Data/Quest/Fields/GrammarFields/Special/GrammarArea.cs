using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
    [Serializable]
    [GrammarField("area")]
    public class GrammarArea : GrammarField<Rect>
    {
        public override Type PrimitiveType => typeof(Rect);
        public override QuestNodeData Data
        {
            get => base.Data;
            set
            {
                if (Data != null) return;

                base.Data = value;
                if(Data.Area != null && Data.Area != this)
                {
                    var pos = Data.Area.value.position;
                    var newRect = new Rect(pos.x+1,pos.y+1, 1, 1);
                    SetValue(newRect);
                }
            }
        }
        public override bool IsValid() => value.width > 0 || value.height > 0;

        public override LBSLog GetValidStateLog() =>
            IsValid() ? base.GetValidStateLog() : new LBSLog($"{name}: Width and Height can't be 0!", UnityEngine.LogType.Error);
    }

    [Serializable]
    [GrammarField("List.area")]
    public class GrammarAreaList : GrammarListField<GrammarArea>
    {
        public override Type PrimitiveType => typeof(GrammarArea);
    }

}
