using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("go to")]
    public class QuestTriggerGoTo : QuestTrigger
    {
        [HideInInspector]
        public DataGoto dataGoto;

        public override void Init()
        {
            base.Init();
            SetData(dataGoto);
        }

        protected override void SetData(QuestActionData data) => dataGoto = (DataGoto)data;

        protected override void OnTriggerEnter(Collider other) 
        {
            if (!IsPlayer(other))return;
            
            TryComplete();
        }

        protected override bool CanComplete() => true;
    }

}