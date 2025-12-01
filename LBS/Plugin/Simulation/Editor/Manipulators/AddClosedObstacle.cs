using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Manipulators
{
    public class AddClosedObstacle : AddObstacle
    {
        #region PROPERTIES
        protected override string IconGuid => "de6c99b07d278fb47a26992fab1c56f0";
        #endregion

        public override void AddObstacleAction()
        {
            triggerTile.AddObstacle(obstacleTile, Components.PathOSObstacleConnections.Category.CLOSE);
        }
    }

}