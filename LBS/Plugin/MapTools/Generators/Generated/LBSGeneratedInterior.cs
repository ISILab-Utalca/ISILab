using ISILab.Commons.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedInterior : LBSGenerated
    {
        #region FIELDS
        [SerializeField]
        DirConnection dirConnection = new DirConnection();

        #endregion

        #region PROPERTIES

        public DirConnection DirConnection
        {
            get => dirConnection;
            set { dirConnection = value; }
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