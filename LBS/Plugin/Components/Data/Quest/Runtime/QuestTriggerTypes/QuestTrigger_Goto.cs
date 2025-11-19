using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS
{
    [QuestNodeActionTag("go to")]
    public class QuestTriggerGoTo : QuestTrigger
    {
        [HideInInspector]
        public DataGoto dataGoto;

        public override void Init()
        {
            base.Init();
            SetUniqueData(dataGoto);
        }

        public override void SetUniqueData(QuestActionData data)
        {
            dataGoto =  (DataGoto)data;
        }

        protected override void OnTriggerEnter(Collider other) 
        {
            if (!IsPlayer(other))return;
            
            CheckComplete();
        }
            
    }

}