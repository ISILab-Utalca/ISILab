using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.AI.Grammar;
using ISILab.Commons.JsonNet;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace ISILab.LBS.Tests
{
    [TestFixture]
    public class QuestBenchmarkReport
    {
        private const int MeasureCount = 100;
        private const string Guid = "b7937caae958ded45a71b6292dba0b0e";

        private LBSLevelData _levelData;
        private GrammarAssistant _grammarAssistant;
        private QuestAssistant _questAssistant;
        private QuestGraph _questGraph;

        #region NODE COUNT 10

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_10() => AddNextNodeBenchmark(10);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_10() => AddPreviousNodeBenchmark(10);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_10() => ExpandNodeBenchmark(10);

        #endregion

        #region NODE COUNT 20

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_20() => AddNextNodeBenchmark(20);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_20() => AddPreviousNodeBenchmark(20);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_20() => ExpandNodeBenchmark(20);

        #endregion

        #region NODE COUNT 30

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_30() => AddNextNodeBenchmark(30);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_30() => AddPreviousNodeBenchmark(30);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_30() => ExpandNodeBenchmark(30);

        #endregion

        #region NODE COUNT 40

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_40() => AddNextNodeBenchmark(40);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_40() => AddPreviousNodeBenchmark(40);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_40() => ExpandNodeBenchmark(40);

        #endregion

        #region NODE COUNT 50

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_50() => AddNextNodeBenchmark(50);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_50() => AddPreviousNodeBenchmark(50);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_50() => ExpandNodeBenchmark(50);

        #endregion

        #region NODE COUNT 60

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_60() => AddNextNodeBenchmark(60);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_60() => AddPreviousNodeBenchmark(60);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_60() => ExpandNodeBenchmark(60);

        #endregion

        #region NODE COUNT 70

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_70() => AddNextNodeBenchmark(70);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_70() => AddPreviousNodeBenchmark(70);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_70() => ExpandNodeBenchmark(70);

        #endregion

        #region NODE COUNT 80

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_80() => AddNextNodeBenchmark(80);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_80() => AddPreviousNodeBenchmark(80);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_80() => ExpandNodeBenchmark(80);

        #endregion

        #region NODE COUNT 90

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_90() => AddNextNodeBenchmark(90);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_90() => AddPreviousNodeBenchmark(90);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_90() => ExpandNodeBenchmark(90);

        #endregion

        #region NODE COUNT 100

        [Test, Performance]
        [Timeout(3600000)]
        public void AddNextNode_100() => AddNextNodeBenchmark(100);

        [Test, Performance]
        [Timeout(3600000)]
        public void AddPreviousNode_100() => AddPreviousNodeBenchmark(100);

        [Test, Performance]
        [Timeout(3600000)]
        public void ExpandNode_100() => ExpandNodeBenchmark(100);

        #endregion

        #region METHODS

        private void AddNextNodeBenchmark(int nodeCount)
        {
            Measure.Method(() =>
                {
                    var nodes = _questGraph.QuestNodes;
                    QuestNode chosenNode = null;
                    string nextAction = null;
                    int attempts = 0;

                    while (string.IsNullOrEmpty(nextAction) && attempts++ < 100)
                    {
                        chosenNode = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                        var nextActions =
                            _grammarAssistant.GetAllValidNextActionsInsert(chosenNode.TerminalID);
                        if (nextActions.Count > 0)
                            nextAction = nextActions[UnityEngine.Random.Range(0, nextActions.Count)];
                    }

                    Assert.IsFalse(string.IsNullOrEmpty(nextAction),
                        $"No valid next action after {attempts} attempts (nodes={nodeCount})");
                    _grammarAssistant.InsertNextAction(nextAction, chosenNode);
                })
                .WarmupCount(1)
                .MeasurementCount((int)Math.Ceiling( (float)MeasureCount * 10.0 / (float)nodeCount))
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupTestEnvironment(nodeCount))
                .CleanUp(CleanupTest)
                .Run();
        }

        private void AddPreviousNodeBenchmark(int nodeCount)
        {
            Measure.Method(() =>
                {
                    var nodes = _questGraph.QuestNodes;
                    QuestNode chosenNode = null;
                    string prevAction = null;
                    int attempts = 0;

                    while (string.IsNullOrEmpty(prevAction) && attempts++ < 100)
                    {
                        chosenNode = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                        var prevActions =
                            _grammarAssistant.GetAllValidPrevActionsInsert(chosenNode.TerminalID);
                        if (prevActions.Count > 0)
                            prevAction = prevActions[UnityEngine.Random.Range(0, prevActions.Count)];
                    }

                    Assert.IsFalse(string.IsNullOrEmpty(prevAction),
                        $"No valid previous action after {attempts} attempts (nodes={nodeCount})");
                    _grammarAssistant.InsertPreviousAction(prevAction, chosenNode);
                })
                .WarmupCount(1)
                .MeasurementCount((int)Math.Ceiling( (float)MeasureCount * 10.0 / (float)nodeCount))
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupTestEnvironment(nodeCount))
                .CleanUp(CleanupTest)
                .Run();
        }

        private void ExpandNodeBenchmark(int nodeCount)
        {
            Measure.Method(() =>
                {
                    var nodes = _questGraph.QuestNodes;
                    QuestNode chosenNode = null;
                    List<string> expansion = null;
                    int attempts = 0;

                    while ((expansion == null || !expansion.Any()) && attempts++ < 100)
                    {
                        chosenNode = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                        var expansions = _grammarAssistant.GetAllExpansions(chosenNode.TerminalID);
                        if (expansions.Count > 0)
                            expansion = expansions[UnityEngine.Random.Range(0, expansions.Count)];
                    }

                    Assert.IsTrue(expansion != null && expansion.Any(),
                        $"No valid expansion after {attempts} attempts (nodes={nodeCount})");
                    _grammarAssistant.ExpandAction(expansion, chosenNode);
                })
                .WarmupCount(1)
                .MeasurementCount((int)Math.Ceiling( (float)MeasureCount * 10.0 / (float)nodeCount))
                .IterationsPerMeasurement(1)
                .SetUp(() => SetupTestEnvironment(nodeCount))
                .CleanUp(CleanupTest)
                .Run();
        }

        #endregion

        #region SETUP - CLEANUP

        private void SetupTestEnvironment(int nodeCount)
        {
            _levelData = JSONDataManager.LoadDataByGUID<LBSLevelData>(Guid);
            Assert.IsNotNull(_levelData, $"Level data could not be loaded for GUID={Guid}");

            LBSLayer firstLayer = _levelData.GetLayer(0);
            Assert.IsNotNull(firstLayer, "First layer not found in level data");

            _questGraph = firstLayer.GetModule<QuestGraph>();
            _questAssistant = firstLayer.GetAssistant<QuestAssistant>();
            _grammarAssistant = firstLayer.GetAssistant<GrammarAssistant>();

            Assert.IsNotNull(_questGraph, "QuestGraph not found");
            Assert.IsNotNull(_questAssistant, "QuestAssistant not found");
            Assert.IsNotNull(_grammarAssistant, "GrammarAssistant not found");

            _questGraph.OwnerLayer = firstLayer;

            if (_questGraph.Grammar == null)
            {
                _questGraph.Grammar = AssetMacro.LoadAssetByGuid<LBSGrammar>("63ab688b53411154db5edd0ec7171c42");
            }

            _questGraph.GraphNodes.Clear();
            _questAssistant.GenerateRandomNodes(nodeCount);
            _questAssistant.ConnectAllNodes();

            firstLayer.Reload();
        }

        private void CleanupTest()
        {
            if (_levelData != null)
            {
                var firstLayer = _levelData.GetLayer(0);
                firstLayer.RemoveAll();
                _questGraph = null;
                _questAssistant = null;
                _grammarAssistant = null;
                _levelData = null;
            }
        }

        #endregion
    }
}