
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathOS
{
    // Connexions entre un SimulationTile de tipo DynamicObstacleTrigger y los
    // respectivos DynamicObstacleObject que afecta.
    
    [System.Serializable]
    public class SimulationObstacleConnections
    {
        #region ENUMS
        [System.Serializable]
        public enum Category
        {
            None,
            OPEN,
            CLOSE
        }
        #endregion
        
        [SerializeField]
        public bool IsNull = false;

        #region CONSTRUCTORS
        public SimulationObstacleConnections()
        {

        }
        // "NULL" Constructor: Represents a "null" connections object. Prevents serialization problems
        // with Unity by replacing traditional "null" value.
        public SimulationObstacleConnections(bool isNull):  this()
        {
            if (!isNull) { Debug.LogError("Null constructor should always set 'isNull' as true!"); }
            this.IsNull = true;
        }
        #endregion
    }
}
