using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("read")]
    public class QuestTriggerRead : QuestTrigger
    {
        [HideInInspector]
        public DataRead readData;
        public GameObject objectToRead;

        public override void Init()
        {
            base.Init();
            SetData(readData);
        }

        protected override void SetData(QuestActionData data)
        {
            readData = (DataRead)data;
            GenericObjectiveTrigger objectiveTrigger = objectToRead.AddComponent<GenericObjectiveTrigger>();
            objectiveTrigger.Setup(this);
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            // Use the "objectToRead" reference to start an interaction
            TryComplete();
        }

        protected override bool CanComplete() => true;
    }

}