using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedInterior : LBSGenerated
    {

        #region FIELDS
        [SerializeField]
        ConnectionData dirConnection = new ConnectionData();

        [SerializeField]
        LBSTile connectinoPair;

        #endregion

        #region PROPERTIES

        public ConnectionData Connection
        {
            get => dirConnection;
            set { dirConnection = value; }
        }

        public LBSTile ConnectedTile 
        { 
            get => connectinoPair;
            set { connectinoPair = value; }
        }

        #endregion

        #region CONSTRUCTORS
        public LBSGeneratedInterior() { }

        #endregion

        #region METHODS

        private void Awake()
        {
        }

        #endregion
    }

}