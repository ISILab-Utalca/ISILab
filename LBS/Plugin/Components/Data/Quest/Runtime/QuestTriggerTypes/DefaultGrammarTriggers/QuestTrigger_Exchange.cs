using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    
    /*
        RECOMMENDED: implement your own give and receive logic, but this example works under the implication of having an inventory
        of class LbsInventory.
    */
    
    [QuestNodeActionTag("exchange")]
    public class QuestTriggerExchange : QuestTrigger
    {
        [HideInInspector]
        public DataExchange dataExchange;
        [SerializeField]
        private string _giveType;
        [SerializeField]
        private string _receiveType;
        
        private LBSInventory _playerInventory;
        
        public int givenAmount;

        public override void Init()
        {
            base.Init();
            SetData(dataExchange);
        }

        protected override void SetData(QuestActionData data)
        {
            dataExchange =  (DataExchange)data;
            _giveType = dataExchange.bundleGiveType.GetGuid();
            _receiveType =dataExchange.bundleReceiveType.GetGuid();
        }
        
        protected override bool CanComplete()
        {
            if (_playerInventory.HasType(_giveType))  givenAmount += _playerInventory.GetTypeAmount(_giveType);
            if (givenAmount < dataExchange.requiredAmount) return false;
            
            ReceiveObjects();
            return true;
        }

        private void ReceiveObjects() => _playerInventory.AddItems(_receiveType, dataExchange.receiveAmount);

        protected override void OnTriggerEnter(Collider other) 
        {
            if (!IsPlayer(other)) return;
            _playerInventory = other.gameObject.GetComponent<LBSInventory>();

            TryComplete();
        }
            
    }

}