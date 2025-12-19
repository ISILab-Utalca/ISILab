using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
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
            SetData(dataTake);
        }

        protected override void SetData(QuestActionData data)
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
                _playerInventory.OnItemAdded += OnItemAdded;
                
            }
        }

        private void OnItemAdded(string itemGuid, int quantity)
        {
            if (dataTake.bundleToTake.GetGuid() == itemGuid)
            {
                // auto check to complete
                TryComplete();
            }
        }

        protected override bool CanComplete() => true;

        private void OnDestroy()
        {
            if (_playerInventory is not null)
            {
                _playerInventory.OnItemAdded -= OnItemAdded;
            }
        }
    }
}