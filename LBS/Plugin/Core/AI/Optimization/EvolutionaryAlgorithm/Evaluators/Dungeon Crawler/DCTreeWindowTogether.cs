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
    [System.Serializable]
    public class DCTreeWindowTogether : IContextualEvaluator, IConfigurableEvaluator, IRangedEvaluator
    {
        // Weird or inconsistent behaviour? Maybe you just added a new Property and forgot to assign it in the Initialization or Clone Methods, you silly cat!
        public float MaxValue => 1;
        public float MinValue => 0;

        public List<LBSLayer> ContextLayers { get; set; } = new List<LBSLayer>();
        public LBSLayer CombinedLayer { get; set; } = null;
        public LBSLayer CombinedInteriorLayer { get; set; } = null;
        public LBSLayer CombinedExteriorLayer { get; set; } = null;

        public string Tooltip => "DC Tree Window Together Evaluator\n\n" +
            "This evaluator aims to ensure that each window has only one tree next to it and in its surroundings.\n\n" +
            "This evaluator currently supports as Context the combination of any of the following layer types:\n" +
            "- Any type of Interior Layer.\n";// +
            //"- Vertex-Based Exterior Layers.";

        public static EvaluatorConfiguration config;

        [SerializeField, SerializeReference]
        public LBSCharacteristic colliderCharacteristic;
        [SerializeField, SerializeReference]
        public LBSCharacteristic treeCharacteristic;

        [SerializeField]
        private int treeDistance = 3;

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

            if (layer is null) return 0.0f;

            ConnectedTileMapModule connectedTM = null;
            SectorizedTileMapModule sectorTM = null;

            switch (layer.ID)
            {
                case "Interior":
                case "Exterior":
                    string moduleID = layer.ID.Equals("Exterior") ? "TempConnectedModule" : "";
                    connectedTM = layer.GetModule<ConnectedTileMapModule>(moduleID);
                    sectorTM = layer.GetModule<SectorizedTileMapModule>();
                    break;


                default:
                    return 0.0f;
            }

            if (connectedTM == null || sectorTM == null) return 0.0f;

            Vector2Int mapOffset = Vector2Int.RoundToInt(chrom.Rect.position);

            var genes = chrom.GetGenes().Cast<BundleData>().ToList();
            HashSet<Vector2Int> treePositions = new HashSet<Vector2Int>();
            List<Vector2Int> treeList = new List<Vector2Int>();

            for (int i = 0; i < genes.Count; i++)
            {
                if (!chrom.IsInvalid(i) && genes[i] != null)
                {
                    if (genes[i].HasTag(treeCharacteristic.FirstTag()))
                    {
                        Vector2Int localPos = chrom.ToMatrixPosition(i);
                        Vector2Int worldPos = localPos + mapOffset;

                        treePositions.Add(worldPos);
                        treeList.Add(worldPos);
                    }
                }
            }

            HashSet<string> processedWindows = new HashSet<string>();
            List<Vector2Int> windowLocations = new List<Vector2Int>();

            float satisfiedWindows = 0;
            float totalWindows = 0;

            foreach (TileZonePair pair in sectorTM.PairTiles)
            {
                LBSTile tile = pair.Tile;
                Vector2Int currentPos = tile.Position;

                List<string> connections = connectedTM.GetConnections(tile);
                List<Vector2Int> dirs = Directions.Bidimencional.Edges;

                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i].Equals("Window"))
                    {
                        Vector2Int neighborPos = currentPos + dirs[i];
                        string windowID = GetUniqueEdgeID(currentPos, neighborPos);

                        if (!processedWindows.Contains(windowID))
                        {
                            processedWindows.Add(windowID);
                            totalWindows++;
                            windowLocations.Add(currentPos);
                            windowLocations.Add(neighborPos);

                            int treesNextToWindow = 0;
                            if (treePositions.Contains(currentPos)) treesNextToWindow++;
                            if (treePositions.Contains(neighborPos)) treesNextToWindow++;

                            if (treesNextToWindow == 1)
                            {
                                satisfiedWindows += 1.0f;
                            }
                        }
                    }
                }
            }

            if (totalWindows == 0) return 0.0f;

            float penalties = 0;
            foreach (Vector2Int tPos in treeList)
            {
                float dist = ManhattanDistance(tPos, windowLocations);
                if (dist > 0 && dist < treeDistance)
                {
                    penalties += 0.5f;
                }
            }

            float finalScore = satisfiedWindows - penalties;
            return Mathf.Clamp01(finalScore / totalWindows);
        }

        private string GetUniqueEdgeID(Vector2Int a, Vector2Int b)
        {
            if (a.x < b.x || (a.x == b.x && a.y < b.y)) return $"{a}-{b}";
            else return $"{b}-{a}";
        }

        private int ManhattanDistance(Vector2Int pos, List<Vector2Int> targets)
        {
            int min = int.MaxValue;
            foreach (var target in targets)
            {
                int dist = Mathf.Abs(pos.x - target.x) + Mathf.Abs(pos.y - target.y);
                if (dist < min) min = dist;
            }
            return min;
        }

        public void InitializeContext(List<LBSLayer> contextLayers, Rect selection)
        {
            ContextLayers = new List<LBSLayer>(contextLayers);
            var ctx = (IContextualEvaluator)this;
            CombinedInteriorLayer = ctx.InteriorLayers(selection);
            CombinedExteriorLayer = ctx.ExteriorLayers(selection);
            CombinedLayer = ctx.MergeExteriorWithInterior(CombinedExteriorLayer, CombinedInteriorLayer, selection);
        }

        public void InitializeDefault()
        {
            colliderCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Collider"));
            treeCharacteristic = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag("Tree"));

            treeDistance = 3;

            CreateOrUpdateConfiguration(ref config, GetType(), GetEvaluatorFields);
        }

        public void ReadConfiguration()
        {
            CreateOrUpdateConfiguration(ref config, GetType());

            colliderCharacteristic = config.GetValue<LBSCharacteristic>("Obstacle");
            treeCharacteristic = config.GetValue<LBSCharacteristic>("Target");

            treeDistance = config.GetValue<int>("Threshold");
        }

        public List<EvaluatorConfigurationField> GetEvaluatorFields()
        {
            var list = new List<EvaluatorConfigurationField>
            {
                new MainTagField("Obstacle", colliderCharacteristic.FirstTag().Label, colliderCharacteristic),
                new MainTagField("Target", treeCharacteristic.FirstTag().Label, treeCharacteristic),
                new IntegerConfigurationField("Threshold", treeDistance)
            };

            return list;
        }

        public object Clone()
        {
            var clone = new DCTreeWindowTogether();
            clone.ContextLayers = new List<LBSLayer>(ContextLayers);
            clone.CombinedLayer = CombinedLayer;
            clone.CombinedInteriorLayer = CombinedInteriorLayer;
            clone.CombinedExteriorLayer = CombinedExteriorLayer;
            clone.treeCharacteristic = treeCharacteristic;
            return clone;
        }
    }
}