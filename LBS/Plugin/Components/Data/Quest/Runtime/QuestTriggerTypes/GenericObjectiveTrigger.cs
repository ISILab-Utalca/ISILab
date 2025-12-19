using ISILab.LBS;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    /// <summary>
    /// Generic class to add box collider to a gameObject.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GenericObjectiveTrigger : MonoBehaviour
    {
        private QuestTrigger _questTrigger;
        private const float SizeFactor = 1f;

        public void Setup(QuestTrigger trigger)
        {
            _questTrigger = trigger;

            BoxCollider boxCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();

            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * SizeFactor;
            boxCollider.center = Vector3.zero;
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (QuestTrigger.IsPlayer(other))
                _questTrigger.TryComplete();
        }
    }
}
