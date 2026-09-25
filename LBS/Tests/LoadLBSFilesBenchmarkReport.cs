using ISILab.Commons.JsonNet;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class LoadLBSFilesBenchmarkReport
    {
        const string LargeMapGUID = "694c2e9d3afd40e41b585c191eeaf4ab";
        const string MediumMapGUID = "1599cf2b434e6724fb173af02f7fb1b1";
        const string SmallMapGUID = "346bf6d52b5ef004e9453887fb4e0e91";

        [Test, Performance]
        public void LoadLargeMap()
        {
            Measure.Method(() =>
                {
                    LBSLevelData loaded = JSONDataManager.LoadDataByGUID<LBSLevelData>(LargeMapGUID);
                    Assert.IsNotNull(loaded);
                
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .Run();
        }

        [Test, Performance]
        public void LoadMediumMap()
        {
            Measure.Method(() =>
            {
                LBSLevelData loaded = JSONDataManager.LoadDataByGUID<LBSLevelData>(MediumMapGUID);
                Assert.IsNotNull(loaded);

            })
                .WarmupCount(3)
                .MeasurementCount(20)
                .Run();
        }

        [Test, Performance]
        public void LoadSmallMap()
        {
            Measure.Method(() =>
                {
                    LBSLevelData loaded = JSONDataManager.LoadDataByGUID<LBSLevelData>(SmallMapGUID);
                    Assert.IsNotNull(loaded);
                    
                })
                .WarmupCount(3)
                .MeasurementCount(20)
                .Run();
        }
    }
}

