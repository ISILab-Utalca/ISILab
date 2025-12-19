using System.Collections.Generic;
using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("stealth")]
    public class QuestTriggerStealth : QuestTrigger
    {
        [HideInInspector]
        public DataStealth dataStealth;
        public List<GameObject> objectsObservers = new();
        /// <summary>
        /// The objective that must be reached to complete the quest
        /// </summary>
        public Vector3 objectivePosition;
        /// <summary>
        /// Tracks if the mission can be completed
        /// </summary>
        private bool _stealthDetected;

        public override void Init()
        {
            base.Init();
            SetData(dataStealth);
        }

        protected override void SetData(QuestActionData data)
        {
            dataStealth = (DataStealth)data;
            
            foreach (GameObject observer in objectsObservers)
            {
                if (observer is null)continue;
                
                StealthObserverTrigger observerTrigger = observer.AddComponent<StealthObserverTrigger>();
                observerTrigger.Setup(this);
            }

            // Create objective trigger
            GameObject objectiveGameObject = new GameObject("StealthObjectiveTrigger")
            {
                transform = { parent = transform, position = objectivePosition }
            };

            GenericObjectiveTrigger objectiveTrigger = objectiveGameObject.AddComponent<GenericObjectiveTrigger>();
            objectiveTrigger.Setup(this);
        }
        
        protected override void OnTriggerEnter(Collider other)
        {
            // When entering the trigger area, reset the stealthDetected state
            if (IsPlayer(other)) _stealthDetected = false;
        }

        private void OnTriggerExit(Collider other)
        {
            // When entering the trigger area, reset the stealthDetected state
            if (IsPlayer(other)) _stealthDetected = false;
        }

        public void OnPlayerDetected() => _stealthDetected = true;

        /// <summary>
        /// Complete if non detectable
        /// </summary>
        /// <returns></returns>
        protected override bool CanComplete() => _stealthDetected == false;
    }

    [RequireComponent(typeof(SphereCollider))]
    public class StealthObserverTrigger : MonoBehaviour
    {
        private QuestTriggerStealth _questTrigger;
        
        private const float DetectRadius = 5f; //TODO: Maybe add as value in LBSTool(node data)
        
        public void Setup(QuestTriggerStealth trigger)
        {
            _questTrigger = trigger;

            SphereCollider sphereCollider = GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();

            sphereCollider.isTrigger = true;
            sphereCollider.radius = DetectRadius; 
        }

        private void OnTriggerEnter(Collider other)
        {
            if (QuestTrigger.IsPlayer(other)) _questTrigger.OnPlayerDetected();
        }
    }

   
}