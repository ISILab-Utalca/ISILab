using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;

namespace ISILab.LBS.Plugin.Modules.Simulation.Editor.Manipulators
{
    public class AddOpenedObstacle : AddObstacle
    {
        #region PROPERTIES
        protected override string IconGuid => "aec905a47b785a34fa16e0f2f15d742f";
        #endregion

        public override void AddObstacleAction()
        {
            triggerTile.AddObstacle(obstacleTile, LBSSimulationObstacleConnections.Category.OPEN);
        }
    }

}