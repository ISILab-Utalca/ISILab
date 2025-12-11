using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Manipulators
{
    public class AddOpenedObstacle : AddObstacle
    {
        #region PROPERTIES
        protected override string IconGuid => "aec905a47b785a34fa16e0f2f15d742f";
        #endregion

        public override void AddObstacleAction()
        {
            triggerTile.AddObstacle(obstacleTile, Components.PathOSObstacleConnections.Category.OPEN);
        }
    }

}