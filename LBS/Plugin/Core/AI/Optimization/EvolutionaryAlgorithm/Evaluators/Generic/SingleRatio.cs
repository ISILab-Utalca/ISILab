using ISILab.AI.Optimization;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    public class SingleRatio : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!

        public float MaxValue => 1;
        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();
        public LBSLayer CombinedLayer { get; set; } = null;
        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;
        public LBSLayer CombinedPopulationLayer { get; set; } = null;

        public string Tooltip => "Single Ratio Evaluator\n\n" +
            "This evaluator aims to balance the frequency of items of a certain type in relation to the entire map space.\n\n" +
            "By default the evaluator balance Chest-tagged items with a ratio of 1 element for each 10 spaces.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.\n" +
            "- Any type of Population Layer.";

        private int permaCount = -1; // Needed for using extra population layers as context

        public static EvaluatorConfiguration config;

        #region CHARACTERISTIC FIELDS

        [SerializeField, SerializeReference]
        public LBSCharacteristic itemCharacteristic;

        [SerializeField]
        public float targetRatio;

        #endregion

        #region EVALUATION

        public float Evaluate(IOptimizable evaluable)
        {
            var chrom = evaluable as BundleTilemapChromosome;

            if (chrom is null)
            {
                throw new System.Exception("Wrong Chromosome Type");
            }
            if (chrom.IsEmpty())
            {
                return 0.0f;
            }

            LBSLayer layer = CombinedLayer;

            float fitness = 0;

            List<BundleData> genes = chrom.GetGenes().Cast<BundleData>().ToList();

            BundleTileMap bundleTM = CombinedPopulationLayer.GetModule<BundleTileMap>();
            List<TileBundleGroup> groups = new();

            bool checkPermaCount = permaCount == -1 && bundleTM is not null;
            if (permaCount == -1) permaCount = 0;

            int itemCount = 0;
            int validGenes = genes.Count;
            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                {
                    validGenes--;
                    continue;
                }
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(itemCharacteristic.FirstTag()))
                    {
                        itemCount++;
                        continue;
                    }
                }

                if (!checkPermaCount) continue;

                TileBundleGroup group = bundleTM.GetGroup(chrom.ToGlobalPosition(i));
                if (group is null || groups.Contains(group)) continue;
                if (group.BundleData.HasTag(itemCharacteristic.FirstTag()))
                {
                    permaCount++;
                    groups.Add(group);
                }
            }

            itemCount += permaCount;

            float currentRatio = (float)itemCount / (float)validGenes;

            int approxTarget10 = Mathf.RoundToInt(targetRatio * 10f);

            if (targetRatio <= 0.5f)
            {
                if(currentRatio < targetRatio)
                {
                    float fact = currentRatio / targetRatio;
                    fitness = fact * fact;
                }
                else
                {
                    int steps = 5 - approxTarget10;
                    int exp = 2;
                    List<int> expList = new() { 2, 3 };
                    if(steps == 1)
                    {
                        exp = expList[steps];
                    }
                    else
                    {
                        for (int i = 2; i <= steps; i++)
                        {
                            exp = expList.Sum();
                            expList.Add(exp);
                        }
                    }

                    float fraction = (1f - currentRatio) / (1f - targetRatio);
                    float OGFraction = fraction;
                    for (int i = 1; i < exp; i++)
                        fraction *= OGFraction;
                    fitness = Mathf.Abs(fraction);
                }
            }
            else
            {
                if(currentRatio > targetRatio)
                {
                    float fact = (1f - currentRatio) / (1f - targetRatio);
                    fitness = fact * fact;
                }
                else
                {
                    int steps = approxTarget10 - 5;
                    int exp = 2;
                    List<int> expList = new() { 2, 3 };
                    if (steps == 1)
                    {
                        exp = expList[steps];
                    }
                    else
                    {
                        for (int i = 2; i <= steps; i++)
                        {
                            exp = expList.Sum();
                            expList.Add(exp);
                        }
                    }

                    float fraction = currentRatio / targetRatio;
                    float OGFraction = fraction;
                    for (int i = 1; i < exp; i++)
                        fraction *= OGFraction;
                    fitness = Mathf.Abs(fraction);
                }
            }
                
            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }

        #endregion

        #region INITIALIZATION

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            IContextualEvaluator ctx = this;
            CombinedInteriorLayer = ctx.InteriorLayers(selection);
            CombinedExteriorLayer = ctx.ExteriorLayers(selection);
            CombinedPopulationLayer = ctx.PopulationLayers();
            permaCount = -1;
            CombinedLayer = ctx.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
        }

        public void InitializeDefault()
        {
            itemCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Chest"));

            targetRatio = 0.1f;
        
            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        #endregion

        #region CONFIGURATION

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            itemCharacteristic = config.GetValue<LBSCharacteristic>("Item");
            targetRatio = config.GetValue<float>("Ratio");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField("Item", itemCharacteristic.FirstTag().Label, itemCharacteristic, "Item whose frequency shall be changed."),
                new FloatConfigurationField("Ratio", targetRatio, 0.0f, 1.0f, "How frequently should the object appear in the level?")
            };

            return list;
        }

        #endregion

        public object Clone()
        {
            var clone = new SingleRatio();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;
            clone.CombinedPopulationLayer = CombinedPopulationLayer;

            clone.itemCharacteristic = itemCharacteristic;

            clone.targetRatio = targetRatio;

            clone.permaCount = permaCount;

            return clone;
        }
    }
}
