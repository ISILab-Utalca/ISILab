using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ISILab.AI.Grammar;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using LBS.Components;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{

    /// <summary>
    /// Represents a potential quest configuration mapping a <see cref="GrammarTerminal"/> 
    /// to compatible world entities (<see cref="TileBundleGroup"/>).
    /// </summary>
    public class QuestCandidate : IEquatable<QuestCandidate>
    {
        public LBSLayer ContextLayer { get; }
        public List<TileBundleGroup> Tiles { get; }
        public GrammarTerminal Terminal { get; }

        public QuestCandidate(LBSLayer layer, GrammarTerminal terminal, List<TileBundleGroup> targets = null)
        {
            ContextLayer = layer;
            Terminal = terminal;
            Tiles = targets ?? new List<TileBundleGroup>();
        }

        public bool Equals(QuestCandidate other)
        {
            if (other == null) return false;
            // Equality is based on the Action and the Context; targets are the variable data.
            return Terminal == other.Terminal && ContextLayer == other.ContextLayer;
        }

        public override int GetHashCode() => HashCode.Combine(Terminal, ContextLayer);
    }

    [Serializable]
    [RequieredModule(typeof(QuestGraph))]
    public class QuestAssistant : LBSAssistant
    {
        #region CONSTS
        private const float listAssignPercent = 0.5f;
        private readonly Vector2Int _positionOverlapOffset = new(25, 50);
        #endregion

        #region FIELDS

        [SerializeField]
        private uint suggestionAmount = 3;


        [SerializeField]
        private List<QuestNode> suggestions = new();

        [SerializeField]
        public bool displaySuggestions;

        #endregion

        #region PROPERTIES
        [JsonIgnore]
        private QuestGraph QuestGraph => OwnerLayer.GetModule<QuestGraph>();

        [JsonIgnore] public uint SuggestionAmount
        {
            get => suggestionAmount;
            set => suggestionAmount = value;
        }

        public LBSLevelData Data => QuestGraph.OwnerLayer.Parent;

        public List<QuestNode> Suggestions
        {
            get => suggestions;
            set => suggestions = value;
        }
        public QuestGraph Graph => OwnerLayer?.GetModule<QuestGraph>();

        #endregion

        #region ACTIONS

        public Action<QuestNode> OnSuggestionAdded;
        public Action<QuestNode> OnSuggestionRemoved;


        #endregion


        #region CONSTRUCTORS
        public QuestAssistant() : base(null, null, Color.black) { }

        public QuestAssistant(string IconGuid, string name, Color colorTint)
            : base(IconGuid, name, colorTint) { }
        #endregion

        #region PUBLIC METHODS
        public override object Clone() => new QuestAssistant(IconGuid, Name, ColorTint);

        public override void OnAttachLayer(LBSLayer layer)
        {
            base.OnAttachLayer(layer);

            OnSuggestionAdded += (suggestion) =>
            {
                if (suggestion == null) 
                    return;
                Suggestions.Add(suggestion);
                RequestTilePaint(suggestion);
            };

            OnSuggestionRemoved += (suggestion) =>
            {
                if (suggestion == null)
                    return;

                Suggestions.Remove(suggestion);
                RequestTileRemove(suggestion);
            };

        }

        public override void OnGUI() { }

        /// <summary>
        /// Generates a specified number of random quest nodes starting from a random root action.
        /// </summary>
        public void GenerateRandomNodes(int count)
        {
            var grammarAssistant = QuestGraph.OwnerLayer.GetAssistant<GrammarAssistant>();
            Assert.IsNotNull(grammarAssistant, "GrammarAssistant should not be null.");

            if (QuestGraph.Grammar.TerminalActions.Count == 0) return;

            // Set random root node
            var randomIndex = Random.Range(0, QuestGraph.Grammar.TerminalActions.Count);
            var currentNode = QuestGraph.AddNewQuestNode(QuestGraph.Grammar.TerminalActions[randomIndex], Vector2.zero);
            QuestGraph.SetRoot(currentNode);

            // Add subsequent nodes
            for (int i = 1; i < count - 1; i++)
            {
                var nextActions = grammarAssistant.GetAllValidNextActionsInsert(currentNode.TerminalID);
                if (!nextActions.Any()) 
                    break;

                var newAction = nextActions[Random.Range(0, nextActions.Count)];
                currentNode = QuestGraph.AddNewQuestNode(newAction, Vector2.zero);
            }

            
        }

        /// <summary>
        /// Connects all quest nodes in sequence.
        /// </summary>
        public void ConnectAllNodes()
        {
            for (int i = 0; i < QuestGraph.GraphNodes.Count - 1; i++)
            {
                QuestGraph.AddEdge(QuestGraph.GraphNodes[i], QuestGraph.GraphNodes[i + 1]);
            }
        }

        /// <summary>
        /// Step 1: Scans the world population to find all possible quests that could exist.
        /// This runs in a background thread.
        /// </summary>
        public List<QuestCandidate> GenerateCandidates(int count, Action<float> onProgress, CancellationToken token = default)
        {
            var candidates = new HashSet<QuestCandidate>();
            var rng = new System.Random();

            foreach (var layer in Data.ContextLayers)
            {
                if (token.IsCancellationRequested) break;

                var population = layer.GetBehaviour<PopulationBehaviour>();
                if (population == null) continue;

                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var candidate = FindQuestSuggestion(population, rng);
                    if (candidate != null) candidates.Add(candidate);

                    onProgress?.Invoke((float)i / count);
                }
            }
            return candidates.ToList();
        }

        #endregion



        /// <summary>
        /// Step 2: Converts the raw Candidates into actual Graph Nodes on the Main Thread.
        /// </summary>
        public List<QuestNode> GetSuggestions(List<QuestCandidate> candidates, Action<float> onProgress = null, CancellationToken token = default)
        {
            var realizedNodes = new List<QuestNode>();
            var occupiedPositions = new List<Vector2>();

            for (int i = 0; i < candidates.Count; i++)
            {
                if (token.IsCancellationRequested) break;

                var candidate = candidates[i];
                var newNode = Graph.GetNodeSuggestion(candidate.Terminal.id, realizedNodes);

                // Map the world data to the quest fields
                newNode.Data.ApplyTilesToData(candidate);

                // Layout Logic
                PositionNodeWithoutOverlap(newNode, occupiedPositions);

                realizedNodes.Add(newNode);
                onProgress?.Invoke((float)i / candidates.Count);
            }

            return realizedNodes;
        }

        #region PRIVATE SOLVER

        private QuestCandidate FindQuestSuggestion(PopulationBehaviour population, System.Random rng)
        {
            var validOptions = FindValidTerminalMappings(population, rng);
            if (validOptions == null || validOptions.Count == 0) return null;

            return validOptions.ElementAt(rng.Next(validOptions.Count));
        }

        private HashSet<QuestCandidate> FindValidTerminalMappings(PopulationBehaviour population, System.Random rng)
        {
            var possibleMappings = new HashSet<QuestCandidate>();
            var grammar = Graph.Grammar;

            foreach (var terminal in grammar.LBSTerminals)
            {
                foreach (var field in terminal.fields)
                {
                    if (field is not IBundleFlags filter) continue;

                    var matches = population.TileBundleGroup
                        .Where(tbg => filter.HasAnyFlag(tbg.BundleData.Bundle))
                        .ToList();

                    if (matches.Count == 0) continue;

                    var candidate = new QuestCandidate(population.OwnerLayer, terminal);

                    if (field.IsList)
                    {
                        PopulateListField(field, candidate, matches, population.OwnerLayer, rng);
                    }
                    else
                    {
                        PopulateSingleField(field, candidate, matches, population.OwnerLayer, rng);
                    }

                    possibleMappings.Add(candidate);
                }
            }
            return possibleMappings;
        }

        private void PopulateSingleField(GrammarField field, QuestCandidate candidate, List<TileBundleGroup> options, LBSLayer layer, System.Random rng)
        {
            var choice = options[rng.Next(options.Count)];
            candidate.Tiles.Add(choice);
            field.SetValue(new BundleTargetGraph(layer, choice));
        }

        private void PopulateListField(GrammarField field, QuestCandidate candidate, List<TileBundleGroup> options, LBSLayer layer, System.Random rng)
        {
            int requiredCount = Mathf.CeilToInt(options.Count * listAssignPercent);

            for (int i = 0; i < requiredCount; i++)
            {
                var choice = options[rng.Next(options.Count)];
                candidate.Tiles.Add(choice);

                var wrapper = (GrammarField)Activator.CreateInstance(field.PrimitiveType);
                wrapper.SetValue(new BundleTargetGraph(layer, choice));
                field.ItemsSource.Add(wrapper);
            }
        }

        private void PositionNodeWithoutOverlap(QuestNode node, List<Vector2> existingPositions)
        {
            var pos = node.Data.Area.value.position;
            var visualOffset = Vector2Int.zero;

            while (existingPositions.Contains(pos))
            {
                pos += _positionOverlapOffset;
                visualOffset.x += (int)QuestGraph.SuggestionDistance;
                visualOffset.y += (int)QuestGraph.ViewNodeWidthOffset;
            }

            existingPositions.Add(pos);
            node.Position = visualOffset;
        }

        #endregion

    }
}