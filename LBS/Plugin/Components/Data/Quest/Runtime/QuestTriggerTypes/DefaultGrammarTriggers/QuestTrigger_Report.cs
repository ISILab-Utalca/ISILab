using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
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
            SetData(dataReport);
        }

        protected override void SetData(QuestActionData data)
        {
            dataReport =  (DataReport)data;
            GenericObjectiveTrigger objectiveTrigger = objectToReport.AddComponent<GenericObjectiveTrigger>();
            objectiveTrigger.Setup(this);
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            // Use the "objectToReport" reference to start a dialogue/report
            // call TryComplete() when the reporting action is done.
        }

        protected override bool CanComplete() => true;
        
    }

}