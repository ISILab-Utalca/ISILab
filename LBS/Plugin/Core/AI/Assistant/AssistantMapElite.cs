using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Commons.Optimization.Evaluator;
using ISILab.AI.Categorization;
using ISILab.AI.Optimization;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Categorization;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [System.Serializable]
    [RequieredModule(typeof(BundleTileMap))]
    public class AssistantMapElite : LBSAssistant
    {
        #region FIELDS
        [JsonIgnore]
        private MapElites mapElites = new MapElites();
        [JsonIgnore, HideInInspector]
        public List<Vector2> toUpdate = new List<Vector2>();
        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public PopulationBehaviour LayerPopulation
        {
            get => OwnerLayer.Behaviours.Find(b => b.GetType().Equals(typeof(PopulationBehaviour))) as PopulationBehaviour;
        }
        [JsonIgnore]
        public LBSLevelData Data
        {
            get => LayerPopulation.OwnerLayer.Parent;
        }

        [JsonIgnore]
        public bool Testing { get; set; } = false;

        [JsonIgnore]
        public Rect RawToolRect { get; set; }

        [JsonIgnore]
        public Rect Rect
        {
            get
            {
                var corners = OwnerLayer.ToFixedPosition(RawToolRect.min, RawToolRect.max);

                var size = corners.Item2 - corners.Item1 + Vector2.one;
                return new Rect(corners.Item1, size);
            }
        }

        [JsonIgnore]
        public bool Finished => mapElites.Finished;

        public bool Running => mapElites.Running;

        [JsonIgnore]
        public int SampleWidth
        {
            get => mapElites.XSampleCount;
            set => mapElites.XSampleCount = value;
        }
        [JsonIgnore]
        public int SampleHeight
        {
            get => mapElites.YSampleCount;
            set => mapElites.YSampleCount = value;
        }

        [JsonIgnore]
        public IOptimizable[,] Samples => mapElites.BestSamples;

        [JsonIgnore]
        public IEvaluator XEvaluator => mapElites.XEvaluator;

        [JsonIgnore]
        public IEvaluator YEvaluator => mapElites.YEvaluator;

        private Type maskType;
        private List<LBSTag> blacklist;

        #endregion

        #region CONSTRUCTORS
        public AssistantMapElite(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }
        #endregion

        #region METHODS

        public override void OnGUI()
        {
        }

        public void Execute(bool synchronous = false, Action<float> onProgress = null, CancellationToken token = default)
        {
            mapElites.OnEnd = OnTerminateBind;
                
            toUpdate.Clear();
            if (!Testing)
            {
                mapElites.OnSampleUpdated += (v) => {
                    if (!toUpdate.Contains(v))
                    {
                        //Debug.Log("adding vector " + v);
                        toUpdate.Add(v);
                    }
                };
            }
            
            if (mapElites.Running)
            {
                mapElites.Stop();
            }
            
            mapElites.Run(synchronous, onProgress, token);
        }

        private void OnTerminateBind()
        {
            EditorApplication.delayCall += () => OnTermination?.Invoke("MapElites ended!",  LogType.Log);
        }

        public void RequestOptimizerStop() => mapElites?.Optimizer?.RequestStop();


        public void Continue()
        {
            throw new NotImplementedException(); // TODO: Implement Continue method for AssistantMapElite class
        }

        public void InitializeEvaluator(IEvaluator evaluator)
        {
            if (evaluator != null)
            {
                var contextualChoice = evaluator as IContextualEvaluator;
                if (contextualChoice != null)
                    contextualChoice.InitializeDefaultWithContext(Data.ContextLayers, RawToolRect);
                else evaluator.InitializeDefault();
            }
        }

        public void AutoSelectArea(out List<string> logs)
        {
            var rect = GetDefaultLayerArea();

            logs = new List<string>();

            //Is any() better than count > 0? Yes. Thank me later.
            if (Data.ContextLayers.Any())
            {
                var subRect = GetLayerContextArea(out logs);
                if (!rect.HasArea()) rect = subRect;
                else if (subRect.HasArea()) rect.GetCombinedArea(subRect);
            }

            RawToolRect = rect;
        }

        private Rect GetDefaultLayerArea()
        {
            //Grabs the owner layer area
            return OwnerLayer.GetModule<BundleTileMap>().GetBounds();
        }

        private Rect GetLayerContextArea(out List<string> logs)
        {
            //Grabs an area that encloses all context layers
            Rect combinedRect = new();
            List<LBSLayer> filteredLayers = new List<LBSLayer>();

            logs = new List<string>();

            foreach (LBSLayer layer in Data.ContextLayers)
            {
                if (layer.ID != "Interior" && layer.ID != "Exterior")
                {
                    logs.Add("Context layers must be of type 'Interior' or 'Exterior'. " +
                        "Layer '" + layer.Name + "' ignored.");
                    continue;
                }

                filteredLayers.Add(layer);
            }

            int firstValid = 0;
            for (int i = 0; i < filteredLayers.Count; i++)
            {
                LBSLayer layer = filteredLayers[i];
                string moduleID = (layer.ID.Equals("Exterior") && layer.Behaviours.Any(b => (bool)((b as ExteriorBehaviour)?.GridType.Equals(ConnectedTileMapModule.ConnectedTileType.VertexBased)))) ? "TempConnectedModule" : "";

                if (i == firstValid) combinedRect = layer.GetModule<ConnectedTileMapModule>(moduleID).GetBounds();

                if (!combinedRect.HasArea())
                {
                    firstValid++;
                    continue;
                }

                Rect rect = layer.GetModule<ConnectedTileMapModule>(moduleID).GetBounds();
                if (!rect.HasArea()) continue;
                combinedRect.GetCombinedArea(rect);
            }

            return combinedRect;
        }

        public void ApplySuggestion(object data)
        {
            var chrom = data as BundleTilemapChromosome;

            if (chrom == null)
            {
                throw new Exception("[ISI Lab] Data " + data.GetType().Name + " is not LBSChromosome!");
            }

            var population = OwnerLayer.Behaviours.Find(b => b.GetType().Equals(typeof(PopulationBehaviour))) as PopulationBehaviour;

            var rect = chrom.Rect;

            for (int i = 0; i < chrom.Length; i++)
            {
                var pos = chrom.ToMatrixPosition(i) + rect.position.ToInt();
                population.RemoveTileGroup(pos);
                var gene = chrom.GetGene(i);
                if (gene == null)
                    continue;
                population.AddTileGroup(pos, gene as BundleData);
            }
        }

        public void LoadPresset(MAPElitesPreset presset)
        {
            if (presset == null)
            {
                throw new Exception("[ISI Lab]: Map Elite Presset not selected or null");
            }

            mapElites?.Optimizer.RequestStop();

            mapElites = presset.MapElites;
            maskType = presset.MaskType;
            blacklist = presset.blackList;
        }

        public void SetAdam(Rect rect, List<LBSLayer> contextLayers = null)
        {
            var tm = OwnerLayer.GetModule<BundleTileMap>();
            var chrom = new BundleTilemapChromosome(tm, rect, CalcImmutables(rect), CalcInvalids(rect, contextLayers));
            mapElites.Adam = chrom;
        }


        private int[] CalcImmutables(Rect rect)
        {
            int[] immutables = null;
            var im = new List<int>();
            var x = (int)rect.min.x;
            var y = (int)rect.min.y;

            #region Deprecated?
            //if (maskType != null)
            //{
            //    var layers = OwnerLayer.Parent.Layers.Where(l => l.Behaviours.Any(b => b.GetType().Equals(maskType)));
            //    foreach (var l in layers)
            //    {
            //        var m = l.GetModule<TileMapModule>();

            //        if (m == null)
            //            continue;

            //        for (int j = y; j < y + rect.height; j++)
            //        {
            //            for (int i = x; i < x + rect.width; i++)
            //            {
            //                var t = m.GetTile(new Vector2Int(i, j));
            //                if (t != null)
            //                {
            //                    continue;
            //                }

            //                var pos = new Vector2(i, j) - rect.position;
            //                var index = (int)(pos.y * rect.width + pos.x);
            //                im.Add(index);
            //            }
            //        }
            //    }
            //}
            #endregion

            var tm = OwnerLayer.GetModule<BundleTileMap>();
            foreach (TileBundleGroup g in tm.Groups)
            {
                foreach (LBSTile t in g.TileGroup)
                {
                    if (rect.Contains(t.Position))
                    {
                        var tags = LBSAssetMacro.GetTagsFromBundle(g.BundleData.Bundle);

                        bool flag = false;
                        foreach (LBSTag tag in tags)
                        {
                            if (blacklist.Contains(tag))
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (flag)
                        {
                            Vector2 pos = t.Position - rect.position;
                            int i = (int)(pos.y * rect.width + pos.x);
                            im.Add(i);
                        }
                    }
                }
            }


            immutables = im.ToArray();
            return immutables;
        }

        private int[] CalcInvalids(Rect rect, List<LBSLayer> contextLayers)
        {
            if(contextLayers is null || contextLayers.Count == 0)
                return new int[0];

            var invalids = new HashSet<int>();
            var layerInvalids = new List<HashSet<int>>();

            int x = (int)rect.min.x;
            int y = (int)rect.min.y;
        
            for(int i = 0; i < contextLayers.Count; i++)
            {
                layerInvalids.Add(new HashSet<int>());
                switch (contextLayers[i].ID)
                {
                    case "Interior":
                        var TM = contextLayers[i].GetModule<TileMapModule>();
                        if (TM is null)
                            continue;
                        for (int j = x; j < x + rect.width; j++)
                        {
                            for (int k = y; k < y + rect.height; k++)
                            {
                                LBSTile tile = TM.GetTile(new Vector2Int(j, k));
                                if (tile is null)
                                {
                                    Vector2 pos = new Vector2(j, k) - rect.position;
                                    int index = (int)(pos.y * rect.width + pos.x);
                                    layerInvalids[i].Add(index);
                                }
                            }
                        }
                        break;
                    case "Exterior":
                        List<string> floorTags = contextLayers[i].GetBehaviour<ExteriorBehaviour>().NavigableTags;
                        var connectedTM = contextLayers[i].GetModule<ConnectedTileMapModule>();
                        if (connectedTM is null)
                            continue;
                        for (int j = x; j < x + rect.width; j++)
                        {
                            for (int k = y; k < y + rect.height; k++)
                            {
                                TileConnectionsPair tilePair = connectedTM.GetPair(new Vector2Int(j, k));
                                if (tilePair is null || !tilePair.IsFloor(floorTags))
                                {
                                    Vector2 pos = new Vector2(j, k) - rect.position;
                                    int index = (int)(pos.y * rect.width + pos.x);
                                    layerInvalids[i].Add(index);
                                }
                            }
                        }
                        break;
                    default:
                        Debug.LogError($"Invalid tiles calculation not implemented for layers of type: {contextLayers[i].ID}");
                        break;
                }
            }

            List<HashSet<int>> existingLayerInvalids = layerInvalids.Where(li => li.Count > 0).ToList();

            if (existingLayerInvalids.Count == 0)
                return new int[0];

            var intersection = new HashSet<int>(existingLayerInvalids[0]);
            for(int i = 1; i < existingLayerInvalids.Count; i++)
            {
                intersection.IntersectWith(existingLayerInvalids[i]);
            }

            invalids = intersection;

            return invalids.ToArray();
        }
        
        public Texture2D GetBackgroundTexture(Rect rect)
        {
            var text = new Texture2D((int)rect.width, (int)rect.height);


            return text;
        }

        public override object Clone()
        {
            return new AssistantMapElite(IconGuid, Name, ColorTint);
        }

        public override bool Equals(object obj)
        {
            var other = obj as AssistantMapElite;

            if (other == null) return false;

            if (!this.Name.Equals(other.Name)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public Vector3 EvaluateOriginalMap()
        {
            if (!RawToolRect.HasArea())
            {
                AutoSelectArea(out _);
            }

            var currentBundleMap = OwnerLayer.GetModule<BundleTileMap>();
            if (currentBundleMap == null) return Vector3.negativeInfinity;

            var sectorModule = OwnerLayer.GetModule<SectorizedTileMapModule>();

            if(sectorModule == null)
            {
                Debug.Log("sectorModule is null");
            }
            if (sectorModule != null)
            {
                Debug.Log("sectorModule is not null");
                sectorModule.RecalculateZonesProximity(RawToolRect);
            }

            if (mapElites.XEvaluator != null) InitializeEvaluator(mapElites.XEvaluator);
            if (mapElites.YEvaluator != null) InitializeEvaluator(mapElites.YEvaluator);
            if (mapElites.Optimizer?.Evaluator != null) InitializeEvaluator(mapElites.Optimizer.Evaluator);


            var tempChromosome = new BundleTilemapChromosome(
                currentBundleMap,
                RawToolRect,
                CalcImmutables(RawToolRect),
                CalcInvalids(RawToolRect, Data.ContextLayers)
            );

            float scoreX = (float)mapElites.XEvaluator.Evaluate(tempChromosome);
            float scoreY = (float)mapElites.YEvaluator.Evaluate(tempChromosome);
            float fitness = (float)mapElites.Optimizer.Evaluator.Evaluate(tempChromosome);

            return new Vector3(scoreX, scoreY, fitness);
        }

        #endregion
    }
}