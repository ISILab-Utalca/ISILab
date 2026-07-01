using ISILab.AI.Grammar;
using ISILab.LBS.Modules;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    // Determines the state of a quest
    public enum QuestState
    {
        Blocked, Active, Completed, Failed
    }


    // Represents a quest node with specific action and state
    [Serializable]
    public class QuestNode : Node
    {
        // Defines the type of quest node (Start, Middle, Goal)


        #region FIELDS

        [SerializeField, JsonRequired]
        private string terminalID = string.Empty;

        [SerializeField, SerializeReference, JsonRequired]
        private QuestNodeData data;

        [SerializeField, JsonRequired]
        private GraphNodeType nodeType;

        [SerializeField, JsonRequired]
        private QuestState questState = QuestState.Blocked;

        [SerializeField]
        private bool validGrammar;

        #endregion

        #region PROPERTIES


        [JsonIgnore]
        public string TerminalID
        {
            get => terminalID;
            set => terminalID = value;
        }

        [JsonIgnore]
        public QuestNodeData Data
        {
            get => data;
            set => data = value;
        }

        [JsonIgnore]
        public GraphNodeType NodeType
        {
            get => nodeType;
            set => nodeType = value;
        }

        [JsonIgnore]
        public QuestState QuestState
        {
            get => questState;
            set => questState = value;
        }
        public bool ValidGrammar
        {
            get => validGrammar;
            set => validGrammar = value;
        }
        public bool ValidData => data.IsValid();

        #endregion

        #region CONSTRUCTORS
        private QuestNode()
        { }

        public QuestNode(string id, Vector2 position, GrammarTerminal terminal, Graph graph) 
            : base(position, graph)
        {
            this.id = id;
            terminalID = terminal.id;

            data = new QuestNodeData(this, terminal);

            nodeType = GraphNodeType.Middle;
            kind = NodeKind.Terminal;
        }
        #endregion

        #region METHODS


        public override string ToString() => terminalID;

        public override bool IsValid() =>
            base.IsValid() & ValidGrammar && Data.IsValid();

        #endregion
    }

}
