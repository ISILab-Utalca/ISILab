using ISILab.LBS.Plugin.Components.Data.Quest.Runtime;
using UnityEngine;


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

        #endregion

        #region PROPERTIES
        public LBSGeneratedPopulation KeyComponent { set => keyComp = value; }
        public string Key { get => key; }
        #endregion

        #region METHODS

        public void Start()
        {
            if (keyComp is null) return;
            // store ID as the object is destroyed when the item is added to inventory
            key = keyComp.GetID();
        }

        /* Implement unlock logic by default just destroy the locked object
        for example, animation. */
        public virtual void OnUnlock() => Destroy(gameObject);

        private void OnTriggerStay(Collider other) => TryUnlock(other);

        private void OnTriggerEnter(Collider other) => TryUnlock(other);

        private bool TryUnlock(Collider other)
        {
            var inventory = other.GetComponent<LBSInventory>();
            if (inventory is null) return false;
            if (!inventory.HasType(key)) return false;

            inventory.RemoveItem(key);

            LBSUnlockHandler[] locks = FindObjectsByType<LBSUnlockHandler>(FindObjectsSortMode.None);
            foreach (var locked in locks)
            {
                if (locked.key == key) locked.OnUnlock();
            }

            return true;
        }

        #endregion

    }
}
