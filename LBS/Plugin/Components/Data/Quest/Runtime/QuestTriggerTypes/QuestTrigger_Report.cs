using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS
{
    [QuestNodeActionTag("report")]
    public class QuestTriggerReport : QuestTrigger
    {
        [HideInInspector]
        public DataReport dataReport;
        public GameObject objectToReport;

        public override void Init()
        {
            base.Init();
            SetUniqueData(dataReport);
        }

        public override void SetUniqueData(QuestActionData data)
        {
            dataReport =  (DataReport)data;
            GenericObjectiveTrigger objectiveTrigger = objectToReport.AddComponent<GenericObjectiveTrigger>();
            objectiveTrigger.Setup(this);
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            
            // Use the "objectToReport" reference to start a dialogue/report
            //CheckComplete();
        }
            
    }

}