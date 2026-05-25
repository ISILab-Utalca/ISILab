using System;
using UnityEngine;

namespace ISILab.LBS.Plugin.Components.Data.Quest.Runtime
{
    /// <summary>
    /// TODO Change into a health on death/Kill indicates better what's used for (link to tag on population bundle)?
    /// </summary>
    public class DestroyNotifier : MonoBehaviour
    {
        // Event to notify when this GameObject is destroyed used by multiple trigger quest check on complete
        public event Action<GameObject> OnDestroyed;

        private void OnDestroy()
        {
            // Notify subscribers that this GameObject is destroyed
            OnDestroyed?.Invoke(gameObject);
        }
    }
}