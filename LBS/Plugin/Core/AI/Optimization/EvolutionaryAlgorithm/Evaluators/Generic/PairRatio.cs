using ISILab.AI.Optimization;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    public class PairRatio : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!

        public float MaxValue => 1;
        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();
        public LBSLayer CombinedLayer { get; set; } = null;
        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;

        public string Tooltip => "DC Custom Evaluator\n\n" +
            "Explain the purpose of your Custom Evaluator and how it works.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.";

        public static EvaluatorConfiguration config;

        #region CHARACTERISTIC FIELDS

        [SerializeField, SerializeReference]
        public LBSCharacteristic item1Characteristic;
        [SerializeField, SerializeReference]
        public LBSCharacteristic item2Characteristic;

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

            int item1Count = 0, item2Count = 0;
            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(item1Characteristic.FirstTag()))
                        item1Count++;
                    else if (genes[i].HasTag(item2Characteristic.FirstTag()))
                        item2Count++;
                }
            }

            if(item1Count == 0 || item2Count == 0) 
                return targetRatio == 1f && item1Count == item2Count ? 1f : 0f;

            float currentRatio = (float)item1Count / (float)item2Count;

            if(currentRatio <= targetRatio)
            {
                float fact = currentRatio / targetRatio;
                fitness = fact * fact;
            }
            else
            {
                // Creating this formula was a pain in the neck (Thank you, Geogebra).
                // Modify it by your own responsibility.
                float offset = targetRatio * 0.2f;
                float displacer = 1f - targetRatio;
                float multiplier = 1f + 12f / targetRatio;

                float logParam = currentRatio + offset + displacer;
                float den = Mathf.Log10(logParam) * multiplier;

                fitness = 1f / den;
            }

            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }

        #endregion

        #region INITIALIZATION

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            var contextualEvaluator = this as IContextualEvaluator;
            CombinedInteriorLayer = contextualEvaluator.InteriorLayers(selection);
            CombinedExteriorLayer = contextualEvaluator.ExteriorLayers(selection);
            CombinedLayer = contextualEvaluator.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
        }

        public void InitializeDefault()
        {
            item1Characteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Chest"));
            item2Characteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Enemies"));

            targetRatio = 0.5f;

            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        #endregion

        #region CONFIGURATION

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            item1Characteristic = config.GetValue<LBSCharacteristic>("Item 1");
            item2Characteristic = config.GetValue<LBSCharacteristic>("Item 2");

            targetRatio = config.GetValue<float>("Ratio");
                //(float)config.GetValue<int>("Value 1") /
                //(float)config.GetValue<int>("Value 2");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            //decimal mult = 1m;
            //decimal remain = 1m;
            //decimal decimalRatio = (decimal)targetRatio;
            //while (remain > 0m)
            //{
            //    remain = decimalRatio % mult;
            //    mult /= 10m;
            //    if (mult <= 1.0E-26m) break;
            //}

            //mult = 0.1m / mult;

            //int num = (int)(decimalRatio * mult);
            //int den = (int)mult;
            //int gcd = GCD(num, den);
            //num /= gcd;
            //den /= gcd;

            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField("Item 1", item1Characteristic.FirstTag().Label, item1Characteristic),
                new MainTagField("Item 2", item2Characteristic.FirstTag().Label, item2Characteristic),
                new FloatConfigurationField("Ratio", targetRatio, 0.05f, 20.00f)
                //new IntegerConfigurationField("Value 1", num, 1, 20),
                //new IntegerConfigurationField("Value 2", den, 1, 20),
            };

            return list;

            //static int GCD(int a, int b)
            //{
                //while(a != 0 && b != 0)
                //{
                    //if (a > b)
                        //a %= b;
                    //else
                        //b %= a;
                //}

                //return a | b;
            //}
        }

        #endregion

        public object Clone()
        {
            var clone = new PairRatio();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;

            clone.item1Characteristic = item1Characteristic;
            clone.item2Characteristic = item2Characteristic;

            clone.targetRatio = targetRatio;

            return clone;
        }
    }
}
