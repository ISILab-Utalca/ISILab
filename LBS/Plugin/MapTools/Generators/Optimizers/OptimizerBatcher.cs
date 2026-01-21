using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class OptimizerBatcher : Generator3DOptimizer
    {
        public override void Optimize(GameObject rootObject)
        {
            Generator3D.StaticObjs(rootObject);
            // rebuild static batches in editor
            StaticBatchingUtility.Combine(rootObject);

        }
    }
}
