using ISILab.Commons.Extensions;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
using static ISILab.LBS.Plugin.MapTools.Generators.LBSGeneratorRule;
using Object = UnityEngine.Object;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class Generator3D
    {
        #region FIELDS

        [SerializeField]
        public LBSGenerator3DSettings settings = new LBSGenerator3DSettings();

        [JsonRequired, SerializeReference]
        private List<LBSGeneratorRule> rules = new();

        [JsonRequired, SerializeReference]
        private LightingSettings lightningSettings;

        private GameObject sharedRoot;

        private OptimizerGeometry optGeo;
        private OptimizerBatcher optBatch;
        private OptimizerGPUInstancing optGPUinst;

        #endregion

        #region PROPERTIES

        private LightingSettings LightningSettings =>
            lightningSettings ??= LBSAssetMacro.LoadAssetByGuid<LightingSettings>(
                "e64852b0a0c259543bc34a95930684dd");

        private OptimizerGeometry OptGeo 
        { 
            get
            {
                optGeo ??= new OptimizerGeometry();
                return optGeo;
            }
        }
        private OptimizerBatcher OptBatch
        {
            get
            {
                optBatch ??= new OptimizerBatcher();
                return optBatch;
            }
        }

        private OptimizerGPUInstancing OptGpuInst
        {
            get
            {
                optGPUinst ??= new OptimizerGPUInstancing();
                return optGPUinst;
            }
        }

        #endregion

        #region METHODS

        public Generator3D()
        {
            optGeo = new OptimizerGeometry();
            optBatch = new OptimizerBatcher();
        }

        #region RULES

        public void AddRule(LBSGeneratorRule rule)
        {
            if (rule == null) return;
            if (rules.Contains(rule)) return;

            rules.Add(rule);
            rule.generator3D = this;
        }

        public void RemoveRule(LBSGeneratorRule rule)
        {
            if (rules.Remove(rule))
                rule.generator3D = null;
        }

        public T GetRule<T>() where T : LBSGeneratorRule
        {
            return rules.OfType<T>().FirstOrDefault();
        }

        #endregion

        public bool CheckIfIsPossible(LBSLayer layer)
        {
            foreach (LBSGeneratorRule rule in layer.GeneratorRules)
            {
                if (!rule.CheckViability(layer))
                    return false;
            }
            return true;
        }

        public GeneratedEntry Generate(LBSLayer layer, List<LBSGeneratorRule> layerRules)
        {
            List<GeneratedGO> logs = new List<GeneratedGO>();

            if (layerRules == null || layerRules.Count == 0)
            {
                logs.Add(new GeneratedGO(null,
                    new LBSLog("[Generator3D] No rules to generate this layer", LogType.Error)));
                return new GeneratedEntry(null, logs);
            }

            GameObject parent = new GameObject(layer.Name);
            parent.transform.position = settings.position;

            foreach (LBSGeneratorRule rule in layerRules)
            {
                GeneratedGO result = rule.Generate(layer, settings);

                if (result.go == null || !string.IsNullOrEmpty(result.log.message))
                {
                    logs.Add(result);
                    continue;
                }

                result.go.SetParent(parent);
            }

            return new GeneratedEntry(parent, logs);
        }

        #region MULTI LAYER GENERATION

        public LBSLog GenerateAllLayers(List<LBSLayer> layers)
        {
            if (layers == null || layers.Count == 0)
                return new LBSLog("There are no layers to generate", LogType.Warning);

            SortLayers(ref layers);

            ResetRoot();

            foreach (LBSLayer layer in layers)
            {
                Tuple<bool, LBSLog> result = GenerateSingleLayer(layer, layers);
                if (!result.Item1)
                {
                    Object.DestroyImmediate(sharedRoot);
                    return result.Item2;
                }
            }

            StandardTopDownCamera.SetStandardTopDown(sharedRoot);
            OnFinishGenerate();

            return new LBSLog("All layers generated correctly");
        }

        public LBSLog GenerateCurrentLayer(LBSLayer layer, List<LBSLayer> allLayers)
        {
            Tuple<bool, LBSLog> result = GenerateSingleLayer(layer, allLayers);
            if (result.Item1)
            {
                StandardTopDownCamera.SetStandardTopDown(sharedRoot);
                OnFinishGenerate();
            }

            return result.Item2;
        }

        private Tuple<bool, LBSLog> GenerateSingleLayer(LBSLayer layer, List<LBSLayer> allLayers)
        {
            if (layer == null)
                return Tuple.Create(false,
                    new LBSLog("There is no reference for any layer to generate.", LogType.Error));

            if (!CheckIfIsPossible(layer))
                return Tuple.Create(false,
                    new LBSLog($"Layer {layer.Name} is not viable to generate.", LogType.Error));

            if (settings.replacePrevious)
                DestroyPreviousLayer(layer.Name);

            QuestRuleGenerator questGen = layer.GetRule<QuestRuleGenerator>();
            if (questGen != null)
            {
                questGen.OnLayerRequired -= OnQuestLayerRequested;
                questGen.OnLayerRequired += OnQuestLayerRequested;

                void OnQuestLayerRequested(string requiredLayer)
                {
                    GenerateLayerByName(requiredLayer, allLayers);
                }
            }

            GeneratedEntry generated = Generate(layer, layer.GeneratorRules);

            if (generated.ParentGO == null || HasErrors(generated))
            {
                CleanupFailedGeneration(generated);
                return Tuple.Create(false,
                    new LBSLog($"Layer {layer.Name} could not be created correctly.", LogType.Error));
            }

            GameObject root = GetOrCreateRoot();
            generated.ParentGO.transform.SetParent(root.transform);

            Undo.RegisterCreatedObjectUndo(generated.ParentGO, "Create Layer");

            StaticObjs(root);
            Optimize(root);
            PostOptimization(generated);

            return Tuple.Create(true,
                new LBSLog($"Layer {generated.ParentGO.name} created."));
        }

        private void GenerateLayerByName(string layerName, List<LBSLayer> allLayers)
        {
            LBSLayer found = allLayers.Find(l => l.Name == layerName);
            if (found == null) return;

            Tuple<bool, LBSLog> result = GenerateSingleLayer(found, allLayers);
            if (result.Item1) OnFinishGenerate();

        }

        #endregion

        private void Optimize(GameObject root)
        {
            switch (settings.optimization3d)
            {
                case OptimizationGenMode.None:
                    return;
                case OptimizationGenMode.Batch:
                    OptBatch.Optimize(root);
                    return;
                case OptimizationGenMode.JoinGeometry:
                    OptGeo.Optimize(root);
                    return;
                case OptimizationGenMode.GpuInstancing:
                    OptGpuInst.Optimize(root);
                    return;
            }
        }

        private void PostOptimization(GeneratedEntry generated)
        {
            if (settings.bakeLights)  Bake(generated.ParentGO);
            if (settings.buildLightProbes) BuildLightProbes();
        }

        private void Bake(GameObject genGo)
        {
            StaticObjs(genGo);
            ReflectionProbe[] probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            foreach (ReflectionProbe probe in probes) probe.RenderProbe();
        }

        private void BuildLightProbes()
        {
            LightProbeCubeGenerator[] probes = Object.FindObjectsByType<LightProbeCubeGenerator>(FindObjectsSortMode.None);
            foreach (LightProbeCubeGenerator lpcg in probes) lpcg.Execute();
        }


        private void OnFinishGenerate()
        {
            if (settings.bakeLights && LightningSettings)
            {
                Lightmapping.lightingSettings = LightningSettings;
                Lightmapping.Bake();
            }

            EditorWindow.FocusWindowIfItsOpen<SceneView>();
        }

        #region HELPERS

        private static void StaticObjs(GameObject obj)
        {
            if (obj.TryGetComponent<LBSGenerated>(out LBSGenerated lbsGen))
            {
                if (lbsGen.BundleRef.HasAnyFlag(Bundle.EElementFlag.Character)) return;
                if (LBSAssetMacro.BundleHasTag(lbsGen.BundleRef, "NoBake")) return;
            }

            if (!obj.isStatic) obj.isStatic = true;
            foreach (Transform child in obj.transform)
            {
                StaticObjs(child.gameObject);
            }
        }

        private GameObject GetOrCreateRoot()
        {
            if (sharedRoot != null)
                return sharedRoot;

            GameObject existing = GameObject.Find(settings.rootParentName);
            if (existing != null)
            {
                sharedRoot = existing;
                return sharedRoot;
            }

            sharedRoot = new GameObject(settings.rootParentName);
            sharedRoot.transform.position = settings.position;
            return sharedRoot;
        }

        private void ResetRoot()
        {
            GameObject existing = GameObject.Find(settings.rootParentName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            sharedRoot = new GameObject(settings.rootParentName);
            sharedRoot.transform.position = settings.position;
        }

        private void DestroyPreviousLayer(string layerName)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            List<GameObject> toDestroy = allObjects
                .Where(obj => obj != null && obj.name == layerName)
                .ToList();   // materialize first

            foreach (GameObject obj in toDestroy)
            {
                Object.DestroyImmediate(obj);
            }

        }

        private bool HasErrors(GeneratedEntry entry)
        {
            return entry.Gens.Any(g =>
                g.go == null || g.log.type == LogType.Error);
        }

        private void CleanupFailedGeneration(GeneratedEntry entry)
        {
            if (entry.ParentGO != null)
                Object.DestroyImmediate(entry.ParentGO);

            foreach (GeneratedGO gen in entry.Gens)
            {
                if (gen.go != null)
                    Object.DestroyImmediate(gen.go);
            }
        }

        private void SortLayers(ref List<LBSLayer> layers)
        {
            string[] order = { "Interior", "Exterior", "Population", "Quest", "Simulation" };
            Dictionary<string, int> lookup = order.Select((id, i) => (id, i))
                                .ToDictionary(x => x.id, x => x.i);

            layers.Sort((a, b) =>
            {
                int ai = lookup.TryGetValue(a.ID, out var va) ? va : int.MaxValue;
                int bi = lookup.TryGetValue(b.ID, out var vb) ? vb : int.MaxValue;
                return ai.CompareTo(bi);
            });
        }

        #endregion

        #endregion

    }


    public class GeneratedEntry
    {
        // main generated go(parent container)
        GameObject parentGo;
        // Generated game objects and logs
        List<GeneratedGO> gens;

        public GameObject ParentGO { get => parentGo; set => parentGo = value; }
        public List<GeneratedGO> Gens { get => gens; set => gens = value; }

        public GeneratedEntry() { }
        public GeneratedEntry(GameObject parentGo, List<GeneratedGO> gens)
        {
            this.parentGo = parentGo;
            this.gens = gens;
        }
    }
   
}