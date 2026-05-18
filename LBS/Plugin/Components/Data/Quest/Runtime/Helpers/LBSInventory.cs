// LBSInventory.cs

using System;
using System.Collections.Generic;
using ISILab.AI.Optimization.Terminations;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.MapTools.Generators;
using UnityEngine;

namespace ISILab.LBS.Plugin.Components.Data.Quest.Runtime
{
    [Serializable]
    public class LBSInventory : MonoBehaviour
    {
        // Delegate for item added event
        public delegate void ItemAddedDelegate(string guid, int amount);
        public event ItemAddedDelegate OnItemAdded;
        

        public readonly Dictionary<string, int> Inventory = new();

        /// <summary>
        /// Returns the amount of a given GUID, use after HasType
        /// </summary>
        public int GetTypeAmount(string guid)
        {
            return Inventory.GetValueOrDefault(guid, 0);
        }

        /// <summary>
        /// Returns true if a GUID is in the inventory
        /// </summary>
        public bool HasType(string guid)
        {
            return Inventory.ContainsKey(guid);
        }

        /// <summary>
        /// Call to add a GUID by n amount
        /// </summary>
        public void AddItems(string guid, int amount)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[LBSInventory] Attempted to add item with empty GUID.");
                return;
            }

            if (!Inventory.TryAdd(guid, amount))
                Inventory[guid] += amount;

            Debug.Log($"[LBSInventory] Added GUID: {guid}, New count: {Inventory[guid]}");
            OnItemAdded?.Invoke(guid, amount);
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject obj = other.gameObject;
            LBSGeneratedPopulation lbsPopGen = obj.GetComponent<LBSGeneratedPopulation>();

            // weapons have a child game obejct for trigger detection
            if (!lbsPopGen)
            {
                obj = other.gameObject.transform.parent is null ? other.gameObject : other.gameObject.transform.parent.gameObject;
                lbsPopGen = other.gameObject.GetComponentInParent<LBSGeneratedPopulation>();
            }


            if (lbsPopGen == null || lbsPopGen.BundleRef == null) return;

            // Can only equip itemsif (lbsPopGen.BundleRef.HasAnyFlag(Bundle.EElementFlag.Item))
            if (lbsPopGen.BundleRef.HasFlag(Bundle.EElementFlag.Item))
            {
                Debug.LogWarning(lbsPopGen.BundleRef.BundleName);
                string guid = lbsPopGen.GetID();  

                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning("[LBSInventory] Skipped object with missing BundleRef GUID.");
                    return;
                }

                AddItems(guid, 1);
                Destroy(obj);
            }
        }

        internal void RemoveItem(string key, int amount = 1)
        {
            if (!Inventory.ContainsKey(key)) return;

            --Inventory[key];
            if (Inventory[key] <= 0) Inventory.Remove(key);
        }
    }
}