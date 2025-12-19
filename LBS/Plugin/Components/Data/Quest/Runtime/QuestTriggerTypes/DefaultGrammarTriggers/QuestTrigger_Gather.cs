using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;
namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("gather")]
    public class QuestTriggerGather : QuestTrigger
    {
        [HideInInspector]
        public DataGather dataGather;

        private string _bundleGuid = string.Empty;
        private LBSInventory _playerInventory;

        public override void Init()
        {
            base.Init();
            SetData(dataGather);
        }

        protected override void SetData(QuestActionData data)
        {
            dataGather = (DataGather) data;
            _bundleGuid = dataGather.bundleGatherType.GetGuid();
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            _playerInventory = other.gameObject.GetComponent<LBSInventory>();
            if (_playerInventory is not null)
            {
                _playerInventory.OnItemAdded += (itemGuid, quantity) =>
                {
                    if (itemGuid == _bundleGuid)
                    {
                        TryComplete();
                    }
                };
            }
        }
        
        protected override bool CanComplete()
        {
            return _playerInventory.GetTypeAmount(_bundleGuid) >= dataGather.gatherAmount;
        }
    }

}