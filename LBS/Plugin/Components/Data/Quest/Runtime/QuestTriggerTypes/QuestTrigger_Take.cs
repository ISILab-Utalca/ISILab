using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS
{
    [QuestNodeActionTag("take")]
    public class QuestTriggerTake : QuestTrigger
    {
        [HideInInspector]
        public DataTake dataTake;
        public GameObject objectToTake;

        private LBSInventory _playerInventory;
        
        public override void Init()
        {
            base.Init();
            SetUniqueData(dataTake);
        }

        public override void SetUniqueData(QuestActionData data)
        {
            dataTake = (DataTake)data;
            if (objectToTake is not null)
            {
                GenericObjectiveTrigger objectiveTrigger = objectToTake.AddComponent<GenericObjectiveTrigger>();
                objectiveTrigger.Setup(this);
            }
            else
            {
                Debug.LogError("The object to take has no collision component. Add collision and regenerate Quest.");
            }

        }

        protected override void OnTriggerEnter(Collider other) 
        {
            if (!IsPlayer(other)) return;
            _playerInventory = other.gameObject.GetComponent<LBSInventory>();
            if (_playerInventory is not null)
            {
                _playerInventory.OnItemAdded += (itemGuid, quantity) =>
                {
                    if (dataTake.bundleToTake.GetGuid() == itemGuid)
                    {
                        // auto check to complete
                        CheckComplete();
                    }
                };
            }
        }
    }
}