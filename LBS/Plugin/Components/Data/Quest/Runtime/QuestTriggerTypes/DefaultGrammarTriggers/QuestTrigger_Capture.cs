using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("capture")]
    public class QuestTriggerCapture : QuestTrigger
    {
        [HideInInspector]
        public DataCapture dataCapture;
        private float ActiveCaptureTime { get; set; }

        public override void Init()
        {
            base.Init();
            SetTypedData(dataCapture);
        }

        protected override void SetData(QuestActionData data) => dataCapture = (DataCapture)data;

        protected void OnTriggerStay(Collider other)
        {
            if (!IsPlayer(other)) return;

            TryComplete();
            ActiveCaptureTime += Time.deltaTime;
 
        }

        protected void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            
            if (dataCapture.resetTimeOnExit) ActiveCaptureTime = 0f;
        }

        public void SetTypedData(DataCapture data) => dataCapture = data;
        protected override bool CanComplete() => ActiveCaptureTime > dataCapture.captureTime;
    }

}