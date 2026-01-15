using ISILab.LBS.Plugin.Components.Behaviours;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Unlock : BundleTileMapAddons
    {
        // keys may only unlcock a single connection at a time
        [SerializeField]
        ConnectionData connection;

        public Action<ConnectionData> OnConnectionChange;

        public ConnectionData Connection 
        {
            get => connection;
            set
            {
                connection = value;
                OnConnectionChange?.Invoke(connection);
            }
        }

        public Addon_Unlock() { }


    }
}
