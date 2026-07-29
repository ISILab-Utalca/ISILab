using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedInterior : LBSGenerated
    {

        #region FIELDS
        [SerializeField]
        ConnectionData dirConnection = new ConnectionData();

        [SerializeField]
        LBSTile connectionPair = new LBSTile(new(0,0));

        #endregion

        #region PROPERTIES

        public ConnectionData Connection
        {
            get => dirConnection;
            set { dirConnection = value; }
        }

        public LBSTile ConnectedTile 
        { 
            get => connectionPair;
            set { connectionPair = value; }
        }

        #endregion

        #region CONSTRUCTORS
        public LBSGeneratedInterior() { }

        #endregion

        #region METHODS

        private void Awake() { }

        #endregion
    }

}