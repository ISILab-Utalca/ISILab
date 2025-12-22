using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("give")]
    public class QuestTriggerGive : QuestTrigger
    {
        [HideInInspector]
        public DataGive dataGive;
        [SerializeField]
        private string _giveObjectType;
        /// <summary>
        /// Reference to the npc in the map, in case a dialogue/interaction wants to be triggered
        /// </summary>
        public GameObject objectToGiveTo;
        private LBSInventory _playerInventory;

        public override void Init()
        {
            base.Init();
            SetData(dataGive);
        }

        protected override void SetData(QuestActionData data)
        {
            dataGive = (DataGive)data;
            _giveObjectType = dataGive.bundleGive.GetGuid();
        }

        protected override void OnTriggerEnter(Collider other) 
        {
            if (!IsPlayer(other)) return;
            _playerInventory = other.gameObject.GetComponent<LBSInventory>();
          
        }

        private void OnTriggerStay(Collider other)
        {
            if (_playerInventory is null) return;
            TryComplete();
        }

        protected override bool CanComplete() => !_playerInventory.HasType(_giveObjectType);
    }

}