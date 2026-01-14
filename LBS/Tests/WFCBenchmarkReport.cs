using ISILab.Commons.JsonNet;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace ISILab.LBS.Tests

{
    [TestFixture]
    public class WFCBenchmarkReport
    {
        LBSLevelData levelData ;
        AssistantWFC WFCassistant;

        const string Edge_5x5_Map = "1e5437b06539cbc4bb2d78f0289eaa41";
        const string Edge_10x10_Map = "82609bf5ea1bcc7419de21d82445fea5";
        const string Edge_20x20_Map = "11800f84c2b82604f9a9e3f09d1a7fe3";
        const string Edge_40x40_Map = "301ab417ce8d3da48986747b725c267a";
        const string Vertex_5x5_Map = "6f1c8b94e4c5f2e4aba756e2d46dbb61";
        const string Vertex_10x10_Map = "41185b8b66e1de3499b080546b6eb729";
        const string Vertex_20x20_Map = "f44e1759c8bde004783ae5bbb9942e76";
        const string Vertex_40x40_Map = "a75be50c10d15e14f934906c53ba56e1";


        [Test, Performance]
        public void TestMap_5x5_Edge()
        {
            SampleGroup sg_01 = new SampleGroup("Run Time", SampleUnit.Millisecond); 
            SampleGroup sg_02 = new SampleGroup("GC Call", SampleUnit.Undefined); 
            
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Edge_5x5_Map))
                .GC()
                .CleanUp(CleanUpWFCTest)
                .Run();
        }
        
        [Test, Performance]
        public void TestMap_10x10_Edge()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
                })
                .WarmupCount(1)
                .MeasurementCount(50)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Edge_10x10_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }
        
        [Test, Performance]
        public void TestMap_20x20_Edge()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
                })
                .WarmupCount(0)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Edge_20x20_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }
        
        [Test, Performance]
        [Timeout(3600000)]
        public void TestMap_40x40_Edge()
        {
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Edge_40x40_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }
        
        
        [Test, Performance]
        public void TestMap_5x5_SameMap_Edge()
        {
            SetupWFCTest(Edge_5x5_Map);
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
            
            CleanUpWFCTest();
        }
        
        [Test, Performance]
        public void TestMap_10x10_SameMap_Edge()
        {
            SetupWFCTest(Edge_10x10_Map);
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
                })
                .WarmupCount(1)
                .MeasurementCount(50)
                .IterationsPerMeasurement(1)
                .Run();
            
            CleanUpWFCTest();
        }
        
        [Test, Performance]
        public void TestMap_20x20_SameMap_Edge()
        {
            SetupWFCTest(Edge_20x20_Map);
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
                })
                .WarmupCount(0)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1)
                .Run();
            
            CleanUpWFCTest();
        }
        
        [Test, Performance]
        [Timeout(600000)]
        public void TestMap_40x40_SameMap_Edge()
        {
            SetupWFCTest(Edge_40x40_Map);
            Measure.Method(() =>
                {
                    Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
                })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .Run();
            
            CleanUpWFCTest();
        }
        

        private void SetupWFCTest(string _guid)
        {
            levelData = JSONDataManager.LoadDataByGUID<LBSLevelData>(_guid);
            Assert.IsNotNull(levelData);
            LBSLayer fistLayer = levelData.GetLayer(0);
            Assert.IsNotNull(fistLayer);
            WFCassistant = fistLayer.GetAssistant<AssistantWFC>("");
            Assert.IsNotNull(WFCassistant);
            WFCassistant.SafeMode = true;
            fistLayer.Reload();
        }
        
        private void CleanUpWFCTest()
        {
            if (levelData != null)
            {
                LBSLayer fistLayer = levelData.GetLayer(0);
                fistLayer.RemoveAll();
                
                WFCassistant = null;
                levelData = null;
            }
        }

        [Test, Performance]
        public void TestMap_5x5_Vertex()
        {
            SampleGroup sg_01 = new SampleGroup("Run Time", SampleUnit.Millisecond);
            SampleGroup sg_02 = new SampleGroup("GC Call", SampleUnit.Undefined);

            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Vertex_5x5_Map))
                .GC()
                .CleanUp(CleanUpWFCTest)
                .Run();
        }

        [Test, Performance]
        public void TestMap_10x10_Vertex()
        {
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
            })
                .WarmupCount(1)
                .MeasurementCount(50)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Vertex_10x10_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }

        [Test, Performance]
        public void TestMap_20x20_Vertex()
        {
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
            })
                .WarmupCount(0)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Vertex_10x10_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }

        [Test, Performance]
        [Timeout(3600000)]
        public void TestMap_40x40_Vertex()
        {
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(false));
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupWFCTest(Vertex_40x40_Map))
                .CleanUp(CleanUpWFCTest)
                .Run();
        }


        [Test, Performance]
        public void TestMap_5x5_SameMap_Vertex()
        {
            SetupWFCTest(Vertex_5x5_Map);
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();

            CleanUpWFCTest();
        }

        [Test, Performance]
        public void TestMap_10x10_SameMap_Vertex()
        {
            SetupWFCTest(Vertex_10x10_Map);
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
            })
                .WarmupCount(1)
                .MeasurementCount(50)
                .IterationsPerMeasurement(1)
                .Run();

            CleanUpWFCTest();
        }

        [Test, Performance]
        public void TestMap_20x20_SameMap_Vertex()
        {
            SetupWFCTest(Vertex_20x20_Map);
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
            })
                .WarmupCount(0)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1)
                .Run();

            CleanUpWFCTest();
        }

        [Test, Performance]
        [Timeout(600000)]
        public void TestMap_40x40_SameMap_Vertex()
        {
            SetupWFCTest(Vertex_40x40_Map);
            Measure.Method(() =>
            {
                Assert.AreEqual(true, WFCassistant.ExecuteTest(true));
            })
                .WarmupCount(0)
                .MeasurementCount(10)
                .IterationsPerMeasurement(1)
                .Run();

            CleanUpWFCTest();
        }
    }
}
