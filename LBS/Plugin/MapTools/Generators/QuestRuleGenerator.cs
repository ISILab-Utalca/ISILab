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
                GenerateTriggersPerNode(settings, quest, tracker, pivot);
            }
        }

        private static void GenerateTriggersPerNode(LBSGenerator3DSettings settings, QuestGraph quest, QuestTracker tracker, GameObject pivot)
        {
            // Map QuestNode -> Trigger GameObject
            Dictionary<QuestNode, GameObject> questNodeGameObjects = CreateQuestNodeGameObjects(settings, quest, tracker, pivot);

            foreach (KeyValuePair<QuestNode, GameObject> entry in questNodeGameObjects)
            {
                GameObject go = entry.Value;
                QuestTrigger qt = go.GetComponent<QuestTrigger>();
                if(qt is null) continue;

                Custom3dQuestGizmo questGizmo = go.AddComponent<Custom3dQuestGizmo>();
                if(questGizmo is null) continue;

                questGizmo.Tracker = tracker;
                questGizmo.Trigger = qt;
            }
            
            // Create AND/OR branch node components
            CreateBranchNodeComponents(quest, tracker, questNodeGameObjects);
        }
        
        private static Dictionary<QuestNode, GameObject> CreateQuestNodeGameObjects(LBSGenerator3DSettings settings, QuestGraph quest, QuestTracker tracker, GameObject pivot)
        {
            var questNodeGameObjects = new Dictionary<QuestNode, GameObject>();

            foreach (var node in quest.GetQuestNodes())
            {
                Type triggerType = node.Data.Terminal.Script.GetClass();
                if (triggerType == null)
                {
                    Debug.LogError($"The terminal {node.Data.Terminal} has no script field attached!");
                    continue;
                }

                var go = CreateTriggerGameObject(settings, pivot, tracker, node, triggerType);

                questNodeGameObjects[node] = go;
            }

            return questNodeGameObjects;
        }
        
        private static GameObject CreateTriggerGameObject(LBSGenerator3DSettings settings, GameObject pivot, QuestTracker tracker, QuestNode node, Type triggerType)
        {
            var go = new GameObject(node.ID) { transform = { parent = tracker.transform } };
            var trigger = (QuestTrigger)go.AddComponent(triggerType);

            // Set visual size
            var size = node.Data.Area.value;
            trigger.SetSize(new Vector3(size.width * settings.scale.x,
                                        size.height * settings.scale.y,
                                        size.height * settings.scale.y));

            // Set position
            var x = (node.Data.Area.value.x + node.Data.Area.value.width / 2 - 1) * settings.scale.x;
            var z = (node.Data.Area.value.y - node.Data.Area.value.height / 2) * settings.scale.y;
            var y = pivot.transform.position.y;
            go.transform.position = settings.position + new Vector3(x, y, z);
           
            if (!node.Data.IsValid())
            {
                Debug.LogError($"Node Data '{node.ID}' doesn't have a valid data");
                Object.DestroyImmediate(pivot);
                return null;
            }

            trigger.SetNode(node);

            // Assign data
            AssignGameObjects(trigger, settings, settings.position, y, new Vector3(settings.scale.x, 0, settings.scale.y) / 2f);

            // all are active in the scene, on play they are activated in order
            go.SetActive(true);
            return go;
        }

        private static void AssignGameObjects(QuestTrigger trigger, LBSGenerator3DSettings settings, Vector3 position, float y, Vector3 vector3)
        {

            List<Vector3> scenePositions = new();
            var grammarBundleGraphs = trigger.Node.Data.GetFields<GrammarBundleGraph>();
            foreach (var gbg in grammarBundleGraphs)
            {
                // Calculate the world position of the BundleGraph's position
                scenePositions.Add(
                    GetScenePosition(
                        gbg.value.TileBundleGroup.AreaRect, 
                        settings, 
                        position, 
                        y, 
                        vector3));
            }

            List<LBSGenerated> lbsgens = new();
            // Instead of OverlapSphere
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

            foreach (var field in trigger.Node.Data.Fields)
            {
                var bundleStored = field as GrammarBundleGraph;
                if (bundleStored == null) continue;
                var bundle = bundleStored.GetBundle();

                if (field.IsList)
                    foreach(var entry in field.ItemsSource)                 
                        AssignGameObjectBundle(trigger, bundle, lbsgens);

                else
                    AssignGameObjectBundle(trigger, bundle, lbsgens);
            }

        }

        private static void AssignGameObjectBundle(
            QuestTrigger trigger,
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
                    trigger.AddGo(lbsgen.gameObject);
                    return;
                }
            }
        }

        private static void CreateBranchNodeComponents(QuestGraph quest, QuestTracker tracker, Dictionary<QuestNode, GameObject> questNodeGameObjects)
        {
            // Group edges by destination branch node
            var branchGroups = quest.GraphEdges
                .Where(e => e.To is AndNode || e.To is OrNode)
                .GroupBy(e => e.To);

            foreach (var group in branchGroups)
            {
                var branchNode = group.Key;
                GameObject branchGameObject;
                QuestTriggerBranch triggerBranchComponent;

                branchGameObject = new GameObject($"{branchNode.ID}") { transform = { parent = tracker.transform } };
                triggerBranchComponent = branchGameObject.AddComponent<QuestTriggerBranch>();
       
                // Assign child triggers
                var childGameObjects = group.SelectMany(e => e.From.Cast<QuestNode>().Select(n => questNodeGameObjects[n]))
                                            .Distinct()
                                            .ToList();
                triggerBranchComponent.SetChildTriggers(childGameObjects);

                // Assign destination trigger(s)
                var destinationEdges = quest.GraphEdges.Where(e => e.From.Contains(branchNode)).ToList();
                if (destinationEdges.Count > 0 && destinationEdges[0].To is QuestNode destNode && questNodeGameObjects.TryGetValue(destNode, out var destinationGameObject))
                {
                    triggerBranchComponent.SetDestinationTrigger(destinationGameObject);
                }

                triggerBranchComponent.SetNode(branchNode);
                branchGameObject.SetActive(true);
            }
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