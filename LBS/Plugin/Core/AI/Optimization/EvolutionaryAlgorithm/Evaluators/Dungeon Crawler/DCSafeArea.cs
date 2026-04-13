using ISILab.AI.Optimization;
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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ISILab.LBS.AI.Categorization.EvaluatorConfiguration;

namespace ISILab.AI.Categorization
{
    public class DCSafeArea : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        public float MaxValue => 1;

        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();

        public LBSLayer CombinedLayer { get; set; } = null;

        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;
        public LBSLayer CombinedPopulationLayer { get; set; } = null;

        public string Tooltip => "DC Safe Area Evaluator\n\n" +
            "This evaluator aims to distribute enemies and other dangers in a way that most of them are in areas far from the players.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n" +
            "- Vertex-Based Exterior Layers.";

        private List<int> permaIndices1 = null;
        private List<int> permaIndices2 = null;

        public static EvaluatorConfiguration config;

        [SerializeField, SerializeReference]
        public LBSCharacteristic playerCharacteristic;

        [SerializeField, SerializeReference]
        public LBSCharacteristic enemiesCharacteristic;

        public float Evaluate(IOptimizable evaluable)
        {
            var chrom = evaluable as BundleTilemapChromosome;

            if (chrom == null)
            {
                throw new System.Exception("Wrong Chromosome Type");
            }
            if (chrom.IsEmpty())
            {
                return 0.0f;
            }

            LBSLayer layer = CombinedLayer;

            float fitness = 0;

            var genes = chrom.GetGenes().Cast<BundleData>().ToList();

            BundleTileMap bundleTM = CombinedPopulationLayer.GetModule<BundleTileMap>();
            List<TileBundleGroup> groups = new();

            bool checkPermaIndices = (permaIndices1 is null || permaIndices2 is null) && bundleTM is not null;
            permaIndices1 ??= new List<int>();
            permaIndices2 ??= new List<int>();

            List<int> playersInd = new List<int>();
            List<int> enemiesInd = new List<int>();

            for (int i = 0; i < genes.Count; i++)
            {
                if (chrom.IsInvalid(i))
                    continue;
                if (genes[i] is not null)
                {
                    if (genes[i].HasTag(playerCharacteristic.FirstTag()))
                    {
                        playersInd.Add(i);
                    }
                    else if (genes[i].HasTag(enemiesCharacteristic.FirstTag()))
                    {
                        enemiesInd.Add(i);
                    }
                }

                if (!checkPermaIndices) continue;

                TileBundleGroup group = bundleTM.GetGroup(chrom.ToGlobalPosition(i));
                if (group is null || groups.Contains(group)) continue;
                if (group.BundleData.HasTag(playerCharacteristic.FirstTag()))
                {
                    permaIndices1.Add(i);
                    groups.Add(group);
                }
                else if (group.BundleData.HasTag(enemiesCharacteristic.FirstTag()))
                {
                    permaIndices2.Add(i);
                    groups.Add(group);
                }
            }

            playersInd.AddRange(permaIndices1);
            enemiesInd.AddRange(permaIndices2);

            int bestPossibleScore = (int)(2.00f * enemiesInd.Count);
            int worstPossibleScore = (int)(1.00f * enemiesInd.Count);
            int score = worstPossibleScore;
            if(layer is not null)
            {
                switch (layer.ID)
                {
                    case "Interior":
                    case "Exterior":
                        score = ScoreEnemyDistance(playersInd, enemiesInd, chrom, layer.GetModule<SectorizedTileMapModule>());
                        break;
                    default:
                        score = ScoreManhattan(playersInd, enemiesInd, chrom);
                        break;
                }
            }
            else
            {
                score = ScoreManhattan(playersInd, enemiesInd, chrom);
            }

            fitness = Mathf.InverseLerp(worstPossibleScore, bestPossibleScore, score);

            UnityEngine.Assertions.Assert.IsFalse(fitness == float.NaN);
            return fitness;
        }
        // Como es practicamente a igual a DCResourceSafety.ScoreResourceDistance, podria ser una extension a futuro?
        private int ScoreEnemyDistance(List<int> players, List<int> enemies, BundleTilemapChromosome chrom, SectorizedTileMapModule sectorTM)
        {
            var zones = sectorTM.SelectedZones;
            var zonesIndex = zones.Select((z, i) => KeyValuePair.Create(z, i)).ToDictionary(x => x.Key, x => x.Value);
            var zonesDist = sectorTM.ZonesProximity;

            var playerZones = new List<int>();
            for (int i = 0; i < players.Count; i++)
                playerZones.Add(-1);

            for (int i = 0; i < players.Count; i++)
            {
                int p = players[i];
                for (int j = 0; j < zones.Count; j++)
                {
                    if (sectorTM.GetZone(chrom.ToMatrixPosition(p) + Vector2Int.RoundToInt(chrom.Rect.position)).Equals(zones[j]))
                    {
                        playerZones[i] = j;
                        break;
                    }
                }
            }

            int totalScore = 0;

            foreach(int enemy in enemies)
            {
                Zone zone = sectorTM.GetZone(chrom.ToMatrixPosition(enemy) + Vector2Int.RoundToInt(chrom.Rect.position));
                if(zone is null) continue;
                int eZone = zonesIndex[zone];
                int score = 2;
                foreach (int pZone in playerZones)
                {
                    score = Mathf.Min(score, zonesDist[eZone, pZone]);
                }
                totalScore += score;
            }

            return totalScore;
        }

        private int ScoreManhattan(List<int> players, List<int> enemies, BundleTilemapChromosome chrom)
        {
            int totalScore = 0;

            int maxSignificantDist = (int)(Mathf.Max(chrom.Rect.width, chrom.Rect.height) * 0.25f);
            int halfDist = maxSignificantDist / 2;
            foreach(int e in enemies)
            {
                int score = 2;
                Vector2Int ePos = chrom.ToMatrixPosition(e);
                foreach(int p in players)
                {
                    Vector2Int pPos = chrom.ToMatrixPosition(p);
                    int dist = Mathf.Abs(ePos.x - pPos.x) + Mathf.Abs(ePos.y - pPos.y);
                    if (halfDist == 0) halfDist = 1;
                    score = Mathf.Min(score, dist / halfDist);
                }
                totalScore += score;
            }

            return totalScore;

            //Debug.LogWarning("El Evaluador Safe Area no ha implementado un método de evaluación para layers distintas a \"Interior\".");
            //return (int)(1.00f * enemies.Count);
        }

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            IContextualEvaluator ctx = this;
            CombinedInteriorLayer = ctx.InteriorLayers(selection);
            CombinedExteriorLayer = ctx.ExteriorLayers(selection);
            CombinedPopulationLayer = ctx.PopulationLayers();
            permaIndices1 = null;
            permaIndices2 = null;
            CombinedLayer = ctx.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
        }

        public void InitializeDefault()
        {
            playerCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Player"));
            enemiesCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Enemies"));

            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            playerCharacteristic = config.GetValue<LBSCharacteristic>("Player");
            enemiesCharacteristic = config.GetValue<LBSCharacteristic>("Danger");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField(playerCharacteristic.FirstTag().Label, playerCharacteristic, "Main item to be compared with every danger."),
                new MainTagField("Danger", enemiesCharacteristic.FirstTag().Label, enemiesCharacteristic, "Dangerous item to move away.")
            };

            return list;
        }

        public object Clone()
        {
            var clone = new DCSafeArea();

            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;
            clone.CombinedPopulationLayer = CombinedPopulationLayer;

            clone.playerCharacteristic = playerCharacteristic;
            clone.enemiesCharacteristic = enemiesCharacteristic;

            clone.permaIndices1 = permaIndices1;
            clone.permaIndices2 = permaIndices2;

            return clone;
        }
    }
}