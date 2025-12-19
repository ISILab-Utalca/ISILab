using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    /** should rework this to bind an event to the enemies (Destroy) function call
     * instead of using OnTriggerStay.
     */
    [QuestNodeActionTag("kill")]
    public class QuestTriggerKill : QuestTrigger
    {
        [HideInInspector]
        public DataKill dataKill;
        public List<GameObject> objectsToKill = new();

        public override void Init()
        {
            base.Init();
            SetData(dataKill);
        }

        protected override void SetData(QuestActionData data) => dataKill = (DataKill)data;

        public void Start()
        {
            foreach (GameObject obj in objectsToKill)
            {
                DestroyNotifier destroyer = obj.GetComponent<DestroyNotifier>();
                destroyer.OnDestroyed += item=>
                {
                    objectsToKill.Remove(item);
                    TryComplete();
                };
            }
        }

        protected override bool CanComplete() =>
            // if the list is empty all enemies were killed
            !objectsToKill.Any();
    }

}