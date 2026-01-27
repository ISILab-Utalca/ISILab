using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;

namespace ISILab.LBS.Plugin.Modules.Simulation.Editor.Manipulators
{
    public class AddClosedObstacle : AddObstacle
    {
        #region PROPERTIES
        protected override string IconGuid => "de6c99b07d278fb47a26992fab1c56f0";
        #endregion

        public override void AddObstacleAction()
        {
            triggerTile.AddObstacle(obstacleTile, LBSSimulationObstacleConnections.Category.CLOSE);
        }
    }

}