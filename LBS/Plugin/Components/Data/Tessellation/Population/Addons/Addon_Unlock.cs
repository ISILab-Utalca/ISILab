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
        DirConnection connection;

        public Action<DirConnection> OnConnectionChange;

        public DirConnection Connection 
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
