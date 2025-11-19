using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS
{
    [QuestNodeActionTag("listen")]
    public class QuestTriggerListen : QuestTrigger
    {
        [HideInInspector]
        public DataListen dataListen;
        public GameObject objectToListen;

        public override void Init()
        {
            base.Init();
            SetUniqueData(dataListen);
        }

        public override void SetUniqueData(QuestActionData data)
        {
            dataListen = (DataListen)data;
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            
            // Use the "objectToListen" reference to start a dialogue
            CheckComplete();
        }
        
    }

}