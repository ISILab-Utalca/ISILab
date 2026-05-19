using GeneticSharp.Domain.Mutations;
using ISILab.AI.Grammar;
using ISILab.Commons.Extensions;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.MapTools.CustomGizmo.QuestGizmo;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using Object = UnityEngine.Object;

namespace ISILab.LBS.Plugin.MapTools.Generators
{

    public class QuestRuleGenerator : LBSGeneratorRule
    {
        private const float frameDelay = 5f;
        private float _currentFrameDelay = frameDelay;
        
        // the probe radius detects objects at a given position in the scene, based on the existing graph
        private const float ProbeRadius = 10f;
        
        private Action<string> _onLayerRequired;
        public event Action<string> OnLayerRequired
        {
            add => _onLayerRequired = value;
            remove => _onLayerRequired -= value;
        }

        public QuestRuleGenerator() : base() { }
        // For template construction
        public QuestRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }

        public override bool CheckViability(LBSLayer layer)
        {
            return true;
        }

        public override object Clone()
        {
            return new QuestRuleGenerator();
        }

        /// <summary>
        /// Generates the quest observer (to set up the quest triggers in the scene)
        /// it also generates a UI Document that will display the default display for quest
        /// 
        /// </summary>
        /// <param name="layer"> the quest layer that contains the quest nodes and edges</param>
        /// <param name="settings"> the settings of the generator</param>
        /// <returns></returns>
        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            var pivot = new GameObject("Quest Tracker");
            var observer = pivot.AddComponent<QuestTracker>();

            CloneRefs.Start();
            var quest = layer.GetModule<QuestGraph>().Clone() as QuestGraph;
            if(quest == null)
            {
                Object.DestroyImmediate(pivot);
                return new GeneratedGO(null, 
                    new LBSLog("No quest graph found. Can't generate", LogType.Error));
            }
            CloneRefs.End();

            if (!quest.GraphEdges.Any())
            {
                Object.DestroyImmediate(pivot);
                return new GeneratedGO(null,
                    new LBSLog("The quest graph is empty!. Can't generate", LogType.Error));
            }
            
            if (quest.Root is null)
            {
                Object.DestroyImmediate(pivot);
                return new GeneratedGO(null, 
                    new LBSLog("There is no root in the graph. Assign a root to generate the quest", LogType.Error));
            }

            if (quest.GetQuestNodes().All(n => n.NodeType != QuestNode.ENodeType.Goal))
            {
                Object.DestroyImmediate(pivot);
                return new GeneratedGO(null, 
                    new LBSLog("There must be at least one goal node. Make sure to have actions with roots but no branches", LogType.Error));
            }
            
            var assistant = layer.GetAssistant<GrammarAssistant>();
            bool allValid = assistant.ValidateQuestGraph();
             if (!allValid)
            {
                Object.DestroyImmediate(pivot);
                return new GeneratedGO(null, 
                    new LBSLog("At least one quest node is not grammatically valid. Fix or remove", LogType.Error));
             }
          
            observer.Init(quest);
            GenerateTriggers(settings, quest, observer, pivot);
            
            /* For LBS User:
             * ----------------------------------------------------------------
             * Replace with your own function to incorporate the created quests
             * into your game. Check the "QuestVisualTree" class as an example.
             * ----------------------------------------------------------------
             */
            CreateUIDocument(pivot.transform, observer.gameObject);
            
            return new GeneratedGO(pivot, new LBSLog(0));
        }

        private void GenerateTriggers(LBSGenerator3DSettings settings, QuestGraph quest, QuestTracker tracker, GameObject pivot)
        {
            foreach (var node in quest.GetQuestNodes())
            {
                // Find if it has a reference to another layer
                GenerateRequiredLayers(node);
            }
            
            // Delay execution so the engine enables the colliders
            EditorApplication.update += DelayGeneration;
            return;

            void DelayGeneration()
            {
                if (_currentFrameDelay-- > 0) return;
                _currentFrameDelay = frameDelay;
                EditorApplication.update -= DelayGeneration;
                GenerateGraphTriggers(settings, quest, tracker, pivot);
            }
        }

        private static void GenerateGraphTriggers(LBSGenerator3DSettings settings, QuestGraph qGraph, QuestTracker tracker, GameObject pivot)
        {
            // 1. Generate the raw instances
            Dictionary<GraphNode, QuestTriggerNode> nodeToTrigger = MakeTriggerNodes(settings, qGraph, tracker, pivot);
            Dictionary<GraphNode, QuestTriggerBranch> branchToTrigger = MakeTriggerBranches(qGraph, tracker);

            // 2. Unify them immediately so we can map edges universally regardless of type
            Dictionary<GraphNode, QuestTrigger> allTriggers = new();
            foreach (var kvp in nodeToTrigger) allTriggers.Add(kvp.Key, kvp.Value);
            foreach (var kvp in branchToTrigger) allTriggers.Add(kvp.Key, kvp.Value);

            // 3. Connect the nodes via your new Next/Previous properties
            foreach (var edge in qGraph.GraphEdges)
            {
                // Find the source triggers matching the IDs in edge.From
                foreach (var sourceNode in nodeToTrigger)
                {
                    if (edge.From.Contains(sourceNode.Key))
                    {
                        // Safely look up the destination target from the unified map
                        if (allTriggers.TryGetValue(edge.To, out QuestTrigger targetTrigger))
                        {
                            // This triggers the smart property setter we built!
                            sourceNode.Value.Next = targetTrigger;
                        }
                    }
                }
            }

            // 4. Register branches with their dependencies 
            foreach (var kvp in branchToTrigger)
            {
                QuestTriggerBranch branchTrigger = kvp.Value;

                // Feed the branch its previous inputs so it can evaluate its AND/OR conditions
                foreach (var prevTrigger in branchTrigger.AllPrevious)
                {
                    branchTrigger.AddPrevious(prevTrigger);
                }
            }

            // 5. Add visual layout aids
            AddTrackerGizmos(tracker, allTriggers);
        }

        private static void AddTrackerGizmos(QuestTracker tracker, Dictionary<GraphNode, QuestTrigger> nodeToTrigger)
        {
            foreach (var entry in nodeToTrigger)
            {
                QuestTrigger qt = entry.Value;
                Custom3dQuestGizmo questGizmo = qt.gameObject.AddComponent<Custom3dQuestGizmo>();
               
                questGizmo.Tracker = tracker;
                questGizmo.Trigger = qt;
            }
        }

        private static Dictionary<GraphNode, QuestTriggerNode> MakeTriggerNodes(LBSGenerator3DSettings settings, QuestGraph quest, QuestTracker tracker, GameObject pivot)
        {
            Dictionary<GraphNode, QuestTriggerNode> dict = new();

            foreach (var node in quest.GetQuestNodes())
            {
                Type triggerType = node.Data.Terminal.Script.GetClass();
                if (triggerType == null)
                {
                    Debug.LogError($"The terminal {node.Data.Terminal} has no script field attached!");
                    continue;
                }

                var go = MakeTriggerNode(settings, pivot, tracker, node, triggerType);

                dict[node] = go;
            }

            return dict;
        }

        private static QuestTriggerNode MakeTriggerNode(LBSGenerator3DSettings settings, GameObject pivot, QuestTracker tracker, QuestNode node, Type triggerType)
        {
            if (!node.Data.IsValid())
            {
                Debug.LogError($"Node Data '{node.ID}' is invalid. Cannot generate trigger.");
                Object.DestroyImmediate(pivot);
                return null;
            }

            var Go = new GameObject(node.ID) { transform = { parent = tracker.transform } };
            var qtn = (QuestTriggerNode)Go.AddComponent(triggerType);

            float pivotY = pivot.transform.position.y;
            qtn.InitTrigger(node, settings, pivotY);

            Vector3 halfScaleOffset = new Vector3(settings.scale.x, 0, settings.scale.y) / 2f;
            FindBundleGos(node.Data, qtn, settings, settings.position, pivotY, halfScaleOffset);

            return qtn;
        }

        private static void FindBundleGos(QuestNodeData data, QuestTriggerNode trigger, LBSGenerator3DSettings settings, Vector3 position, float y, Vector3 vector3)
        {
            if (trigger == null)
                return;

            // find and store the positions of the generated game objects, based on their Bundle target graph position
            List<Vector3> scenePositions = new();
            var grammarBundleGraphs = data.GetFields<GrammarBundleGraph>();
            foreach (var gbg in grammarBundleGraphs)
            {
                Vector3 pos = GetScenePosition(gbg.value.Area, settings, position, y, vector3);
                scenePositions.Add(pos);
                
            }

            // find objects generated whose position correspond to the generated bundle graph target gos
            List<LBSGenerated> lbsgens = new();
            var allGenerated = Object.FindObjectsByType<LBSGenerated>(FindObjectsSortMode.None);
            foreach (var lbsgen in allGenerated)
            {
                foreach(var scenePosition in scenePositions)
                {
                    if (Vector3.Distance(lbsgen.transform.position, scenePosition) <= ProbeRadius)
                    {
                        lbsgens.Add(lbsgen);
                    }
                }
            }

            // try to find the lbsgens by bundle type
            foreach (var field in trigger.Fields)
            {
                var bundleStored = field as GrammarBundleGraph;
                if (bundleStored == null) continue;
                var bundle = bundleStored.GetBundle();

                if (field.IsList)
                    foreach(var entry in field.ItemsSource)                 
                        FindGoWithBundle(trigger, bundle, lbsgens);

                else
                    FindGoWithBundle(trigger, bundle, lbsgens);
            }

        }

        private static void FindGoWithBundle(
            QuestTriggerNode trigger,
            Bundle bundle,
            List<LBSGenerated> lbsgens)
        {
            if (bundle == null)
                return;

            // add a different lbsgen each time
            lbsgens.Shuffle();

            foreach (var lbsgen in lbsgens)
            {
                if (lbsgen.BundleRef == bundle)
                {
                    trigger.Gos.Add(lbsgen.gameObject);
                    return;
                }
            }
        }

        private static Dictionary<GraphNode, QuestTriggerBranch> MakeTriggerBranches(QuestGraph quest, QuestTracker tracker)
        {
            Dictionary<GraphNode, QuestTriggerBranch> dict = new();
            // Group edges by destination branch node
            var branchGroups = quest.GraphEdges
                .Where(e => e.To is AndNode || e.To is OrNode)
                .GroupBy(e => e.To);

            foreach (var group in branchGroups)
            {
                var branch = group.Key;
                QuestTriggerBranch qtb;

                GameObject branchGo = new GameObject($"{branch.ID}") { transform = { parent = tracker.transform } };
                qtb = branchGo.AddComponent<QuestTriggerBranch>();

                qtb.InitTrigger(branch);
            }

            return dict;
        }

        private void GenerateRequiredLayers(QuestNode node)
        {
            List<string> referencedLayers = node.Data.ReferencedLayerNames();
            if (referencedLayers is null || !referencedLayers.Any()) return;
            referencedLayers = referencedLayers.Distinct().ToList();

        // Find all GameObjects in the scene
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<GameObject> matchingObjects = allObjects.Where(gameObject => referencedLayers.Contains(gameObject.name)).ToList();
                    
            foreach (GameObject gameObject in matchingObjects)
            {
                referencedLayers.Remove(gameObject.name);
            }

            // the list keeps the non existing objects
            foreach (var pendingLayerID in referencedLayers.Distinct())
            {
                if (EditorUtility.DisplayDialog(
                        "Missing Layer Dependency",
                        $"The layer \"{pendingLayerID}\" does not exist and its data is being used by " +
                        $"the quest layer.\nWould you like to generate it now?",
                        "Yes (Generate Layer)",
                        "No (Set values manually in scene)"
                    ))
                {
                    _onLayerRequired?.Invoke(pendingLayerID);
                }
              
            }

        }



      private static void AssignObjectByBundleGraph(
            QuestNode node,
            BundleTargetGraph bundleGraph,
            LBSGenerator3DSettings settings,
            Vector3 basePos,
            float y,
            Vector3 delta,
            Action<GameObject> assignAction)
      {
            // Calculate the world position of the BundleGraph's position
            var scenePosition = GetScenePosition(bundleGraph.Area, settings, basePos, y, delta);

            // Find objects at the position with LBSGenerated component using physics query
            var colliders = Physics.OverlapSphere(scenePosition, ProbeRadius);
            if (colliders == null || colliders.Length == 0)
            {
                Debug.LogWarning($"OverlapSphere collider empty, no objects found.");
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null) continue;

                var lbsGenerated = collider.GetComponent<LBSGenerated>();
                if (lbsGenerated == null || lbsGenerated.BundleRef == null) continue;
                if (lbsGenerated.LayerName != bundleGraph.Layer?.Name) continue; 

                Bundle bundleRef = AssetMacro.LoadAssetByGuid<Bundle>(bundleGraph.GUID);
                if (lbsGenerated.BundleRef != bundleRef) continue;

                assignAction?.Invoke(collider.gameObject);
                return;
            }

            Debug.LogWarning($"No object with LBSGenerated component and matching BundleRef Guid '{bundleGraph.GUID}' found at position {scenePosition} for node {node.ID}");
        }

        private static Vector3 GetScenePosition(Rect graphArea, LBSGenerator3DSettings settings, Vector3 basePos, float y,
            Vector3 delta)
        {
            var bundlePosX = graphArea.x * settings.scale.x;
            var bundlePosZ = graphArea.y * settings.scale.y;
            var scenePosition = basePos + new Vector3(bundlePosX, y, bundlePosZ) - delta;
            return scenePosition;
        }


        /// <summary>
        /// Creates the ui document class (that's displayed during game mode) and
        /// adds it into the layer generated game object
        /// </summary>
        /// <param name="pivotTransform"> transform to assign the UI as child</param>
        private void CreateUIDocument(Transform pivotTransform, GameObject observerGameObject)
        {
            //eliminar previo
            var prev = GameObject.Find("UIDocument");
            if (prev)
                Object.DestroyImmediate(GameObject.Find("UIDocument"));

            GameObject uiGameObject = new GameObject("UIDocument");
            UIDocument uiDocument = uiGameObject.AddComponent<UIDocument>();
           
            if (!uiGameObject) return;
            var questVisualTree = uiGameObject.AddComponent<QuestVisualTree>();
            var uiAsset = DirectoryTools.GetAssetByName<VisualTreeAsset>("QuestVisualTree");
            var panelSettings = AssetMacro.LoadAssetByGuid<PanelSettings>("da6adae693698d3409943a20661e2031");

            if (!uiAsset || !panelSettings) return;

            questVisualTree.Go = observerGameObject;
            uiDocument.visualTreeAsset = uiAsset;
            uiDocument.panelSettings = panelSettings;
            uiGameObject.transform.SetParent(pivotTransform);
        }
    }
}