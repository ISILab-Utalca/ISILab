using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
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
            SetData(dataListen);
        }

        protected override void SetData(QuestActionData data) => dataListen = (DataListen)data;

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            // Use the "objectToListen" reference to start a dialogue.
            // Call TryComplete() when the listening action is done.

        }

        protected override bool CanComplete() => true;
    }

}