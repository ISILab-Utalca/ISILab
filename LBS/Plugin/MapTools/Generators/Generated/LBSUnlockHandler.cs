using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;


namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class LBSUnlockHandler : MonoBehaviour
    {

        #region STATICS

        public static string methodName = "OnUnlock";
        #endregion

        #region FIELDS
        private string key = string.Empty;

        // Component whose's runtime ID will be the key of this lock
        [SerializeReference]
        LBSGeneratedPopulation keyComp;

        NavMeshObstacle obstacle;

        #endregion

        #region PROPERTIES
        public LBSGeneratedPopulation KeyComponent { set => keyComp = value; }
        public string Key { get => key; }
        #endregion

        #region METHODS

        public void Start()
        {
            obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) return;
            obstacle.carving = true;

            if (keyComp == null) return;
            // store ID as the object is destroyed when the item is added to inventory
            key = keyComp.GetID();

            ActivatePOICallback();
        }

        private void ActivatePOICallback()
        {
            if (keyComp == null || !keyComp.TryGetComponent<DestroyNotifier>(out var keyDestroyNotifier))
                return;
            
            keyDestroyNotifier.OnDestroyed += obj =>
            {
                Debug.Log("ON DESTROY!!!");
                SimulationUnlock();
            };
        }

        /* Implement unlock logic by default just destroy the locked object
        for example, animation. */
        public virtual void OnUnlock()
        {
            if(keyComp == null)
                SimulationUnlock();

            obstacle = GetComponent<NavMeshObstacle>();
            obstacle.carving = false;
            Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other) => TryUnlock(other);

        private void OnTriggerEnter(Collider other) => TryUnlock(other);

        private bool TryUnlock(Collider other)
        {
            var inventory = other.GetComponent<LBSInventory>();
            if (inventory == null) return false;
            if (!inventory.HasType(key)) return false;

            inventory.RemoveItem(key);

            LBSUnlockHandler[] locks = FindObjectsByType<LBSUnlockHandler>(FindObjectsSortMode.None);
            foreach (var locked in locks)
            {
                if (locked.key == key) locked.OnUnlock();
            }

            return true;
        }

        private void SimulationUnlock()
        {
            Collider[] doorPOIElements = Physics.OverlapBox(transform.position, new Vector3(0.5f, 0.5f, 1f), transform.rotation);
            for (int i = 0; i < doorPOIElements.Length; i++)
            {
                var simComp = doorPOIElements[i].GetComponentInParent<LBSGeneratedSimulation>();
                if (simComp == null || simComp.Visible) continue;

                simComp.ReactivateEntity();
            }
        }

        #endregion

    }
}
