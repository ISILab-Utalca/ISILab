using ISILab.AI.Optimization;
using ISILab.Commons;
using ISILab.Extensions;
using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    [System.Obsolete("Not implemented yet.")]
    public class DistanceRange : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
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

        /* Place here any LBSCharacteristic type field required for your evaluator to work. */
        #region CHARACTERISTIC FIELDS

        [SerializeField, SerializeReference]
        public LBSCharacteristic item1Characteristic;
        [SerializeField, SerializeReference]
        public LBSCharacteristic item2Characteristic;

        [SerializeField]
        private float min;
        [SerializeField]
        private float max;

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

            /* Search for your tagged elements. */
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

            /* You can use the necessary modules for your evaluator to work. */

            /**
            string connectedModuleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
            string sectorModuleID = "";
            ConnectedTileMapModule connectedTM = layer.GetModule<ConnectedTileMapModule>(connectedModuleID);
            SectorizedTileMapModule sectorTM = layer.GetModule<SectorizedTileMapModule>(sectorModuleID);
            **/

            if (layer is not null)
            {
                //fitness = EvaluateWithContext(defaultCharacteristicIndex, chrom, null);
            }
            else
            {
                //fitness = EvaluateWithoutContext(defaultCharacteristicIndex, chrom);
            }

            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }

        float EvaluateWithContext(int index, BundleTilemapChromosome chrom, params LBSModule[] modules)
        {
            throw new System.NotImplementedException("Default evaluation method not overwritten.");
        }

        float EvaluateWithoutContext(int index, BundleTilemapChromosome chrom)
        {
            throw new System.NotImplementedException("Default evaluation method not overwritten.");
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
            /* Initialize here all your LBSCharacteristic fields. */
            item1Characteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("TAG NAME"));
        
            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        #endregion

        #region CONFIGURATION

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            item1Characteristic = config.GetValue<LBSCharacteristic>("FIELD NAME");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField("FIELD NAME", item1Characteristic.FirstTag().Label, item1Characteristic)
            };

            return list;
        }

        #endregion

        public object Clone()
        {
            var clone = new DistanceRange();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;

            clone.item1Characteristic = item1Characteristic;
            return clone;
        }
    }
}
