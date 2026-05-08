using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using ISILab.AI.Grammar;
using ISILab.Commons.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using LBS.Components;
using Newtonsoft.Json;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{

    /// <summary>
    /// Stores a <see cref="GrammarTerminal"/> and <see cref="TileBundleGroup"/>s 
    /// if they have alllowed flags via <see cref="IBundleFlags"/> interface.
    /// </summary>
    public class TerminalToBundleTiles : IEquatable<TerminalToBundleTiles>
    {
        public LBSLayer Layer { get; }
        public List<TileBundleGroup> Tiles { get; }
        public GrammarTerminal Terminal { get; }

        public TerminalToBundleTiles(LBSLayer layer, GrammarTerminal terminal, List<TileBundleGroup> tiles = null)
        {
            Layer = layer;
            Tiles = tiles ?? new List<TileBundleGroup>();
            Terminal = terminal;
        }

        public bool Equals(TerminalToBundleTiles other)
        {
            if (other == null) return false;
            return Terminal == other.Terminal && Layer == other.Layer && Tiles == other.Tiles;
        }

        public override int GetHashCode() => HashCode.Combine(Terminal, Layer);
    }
    

    [Serializable]
    [RequieredModule(typeof(QuestGraph))]
    public class QuestAssistant : LBSAssistant
    {
        #region FIELDS
        
        [SerializeField]
        private uint suggestionAmount = 3;
        private Vector2Int _positionOverlapOffset = new(25, 50);

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

        Action<QuestNode> OnSuggestionAdded;
        Action<QuestNode> OnSuggestionRemoved;


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
                RequestTilePaint(suggestion);
            };

            OnSuggestionRemoved += (suggestion) =>
            {
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
        /// Generates a list of suggestions from population layers.
        /// </summary>
        public List<TerminalToBundleTiles> GenSuggestions(int suggestionsCount, Action<float> onProgress, CancellationToken token = default)
        {
            HashSet<TerminalToBundleTiles> suggestions = new HashSet<TerminalToBundleTiles>();
            foreach (var contextLayer in Data.ContextLayers)
            {
                if (token.IsCancellationRequested) 
                    return suggestions.ToList();
                
                var pb = contextLayer.GetBehaviour<PopulationBehaviour>();
                if (pb == null) continue;
                for (int index = 0; index < suggestionsCount; index++)
                {
                    if (token.IsCancellationRequested) 
                        return suggestions.ToList();
                    
                    var newSuggestion = GetSuggestion(pb);
                    if(newSuggestion == null) 
                        continue;

                    suggestions.Add(newSuggestion);
                    onProgress?.Invoke((float)index / suggestions.Count);
                }

            }

            return suggestions.ToList();
        }

        #endregion



        /// <summary>
        /// Creates suggestion nodes in the quest graph using the suggestion list.
        /// get and preassign tlies to the nodes based on the suggestion list and the context layers in the level data.
        /// </summary>
        public List<QuestNode> GetSuggestions(List<TerminalToBundleTiles> suggestions, Action<float> onProgress = null, CancellationToken token = default)
        {
            List<QuestNode> suggestedNodes = new List<QuestNode>();
            if(!suggestions.Any()) 
                return suggestedNodes;
            
            List<Vector2> existingPositions = new();
            suggestions = suggestions.Distinct().ToList();
            for (var index = 0; index < suggestions.Count; index++)
            {
                if(token.IsCancellationRequested) 
                    return suggestedNodes;

                var suggestion = suggestions[index];
                var newNode = QuestGraph.GetNodeSuggestion(suggestion.Terminal.id, suggestedNodes);
                var nodeData = newNode.Data;

               // suggestion.Tiles.Shuffle();

                nodeData.ApplyTilesToData(suggestion);


                // trigger position is used to draw the suggestion element area
                var triggerPos = nodeData.Area.value.position;
                // to move the capsule within the suggestion element area
                Vector2Int offsetPosition = Vector2Int.zero;

                while (existingPositions.Contains(triggerPos))
                {
                    triggerPos += _positionOverlapOffset;
                    offsetPosition.x += (int)QuestGraph.SuggestionDistance;
                    offsetPosition.y += (int)QuestGraph.ViewNodeWidthOffset;
                }

                existingPositions.Add(triggerPos);
                newNode.Position = offsetPosition;
                suggestedNodes.Add(newNode);
                
                onProgress?.Invoke((float)index/suggestions.Count);
            }
            
            return suggestedNodes;
        }

        #region PRIVATE METHODS

        private TerminalToBundleTiles GetSuggestion(PopulationBehaviour pb)
        {
            var groups = GroupTerminalsToTiles(pb);
            if (groups == null || groups.Count == 0) 
                return null;

            // Pick a random index safely in background thread
            var rng = new System.Random();
            var groupsArray = groups.ToArray();
            return groupsArray[rng.Next(groupsArray.Length)];
        }


        /// <summary>
        /// Groups <see cref="GrammarTerminal"/>s to <see cref="TileBundleGroup"/>s 
        /// based on <see cref="IBundleFlags"/> compatibility.
        /// </summary>
        private HashSet<TerminalToBundleTiles> GroupTerminalsToTiles(PopulationBehaviour pb)
        {
            var grammar = QuestGraph.Grammar;
            if (grammar == null || pb == null)
                return null;

            var groups = new HashSet<TerminalToBundleTiles>();

            foreach (var terminal in grammar.LBSTerminals)
            {
                foreach(var field in terminal.fields)
                {
                    if (field is not IBundleFlags bundleFlag)
                        continue;

                    var validTiles = pb.TileBundleGroup
                        .Where(tbg => bundleFlag.HasAnyFlag(tbg.BundleData.Bundle))
                        .ToList();

                    if (validTiles.Count <= 0)
                        continue;

                    var newGroup = new TerminalToBundleTiles(pb.OwnerLayer, terminal);
                    foreach (var tile in validTiles)
                        newGroup.Tiles.Add(tile);

                    groups.Add(newGroup);
                }
            }

            return groups;
        }


        #endregion
    }
}