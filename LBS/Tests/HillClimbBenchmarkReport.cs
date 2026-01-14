using ISILab.Commons.JsonNet;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;


namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class HillClimbBenchmarkReport
    {
        LBSLevelData levelData ;
        HillClimbingAssistant HCassistant;
        const string Map25Rooms = "4da6ebb0aca35d64b88743e3dadf267d";
        const string Map16Rooms = "427ab32f305e74e41bad23bf41f74b05";
        const string Map16Rooms_Simpler = "69981e3f2a94f1849a8008ba46c915cf";
        const string Map9Rooms = "5751d55fd72bb1945b789e0ff542c4f5";
        const string Map5Rooms = "d68863ab6554de747be765e86aa7ee9d";


        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureHillClimbing_25_Rooms()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(HCassistant.TryExecute(out string log, out LogType type), true);
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupHillClimbTest(Map25Rooms))
                .CleanUp(CleanUpHillClimbTest)
                .Run();
        }
        
        
        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureHillClimbing_16_Rooms()
        {
            Measure.Method(() =>
            {
                Assert.AreEqual(HCassistant.TryExecute(out string log, out LogType type), true);
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupHillClimbTest(Map16Rooms))
                .CleanUp(CleanUpHillClimbTest)
                .Run();
        }
        
        
        [Test, Performance]
        [Timeout(3600000)]
        public void MeasureHillClimbing_16_Rooms_Simpler()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(HCassistant.TryExecute(out string log, out LogType type), true);
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupHillClimbTest(Map16Rooms_Simpler))
                .CleanUp(CleanUpHillClimbTest)
                .Run();
        }
        
        [Test, Performance]
        public void MeasureHillClimbing_9_Rooms()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(HCassistant.TryExecute(out string log, out LogType type), true);
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupHillClimbTest(Map9Rooms))
                .CleanUp(CleanUpHillClimbTest)
                .Run();
        }
        
        [Test, Performance]
        public void MeasureHillClimbing_5_Rooms()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(HCassistant.TryExecute(out string log, out LogType type), true);
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupHillClimbTest(Map5Rooms))
                .CleanUp(CleanUpHillClimbTest)
                .Run();
        }

        // [OneTimeSetUp]
        private void SetupHillClimbTest(string _guid)
        {
            levelData = JSONDataManager.LoadDataByGUID<LBSLevelData>(_guid);
            Assert.IsNotNull(levelData);
            LBSLayer fistLayer = levelData.GetLayer(0);
            Assert.IsNotNull(fistLayer);
            HCassistant = fistLayer.GetAssistant<HillClimbingAssistant>("");
            Assert.IsNotNull(HCassistant);
            fistLayer.Reload();
        }


        private void CleanUpHillClimbTest()
        {
            if (levelData != null)
            {
                LBSLayer fistLayer = levelData.GetLayer(0);
                fistLayer.RemoveAll();
                
                HCassistant = null;
                levelData = null;
            }
        }
    }

}

