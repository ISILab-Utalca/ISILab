using ISILab.LBS.Modules;
using Newtonsoft.Json;
using System;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

namespace ISILab.LBS.Components
{
    // Determines the state of a quest
    public enum QuestState
    {
        Blocked, Active, Completed, Failed
    }

    // Base class for nodes in a quest graph, handling position and validation
    [Serializable]
    public abstract class GraphNode : ICloneable
    {
        #region CONST

        public const string Or = "Or";      
        public const string And = "And";

        #endregion

        #region FIELDS

        [SerializeField]
        private string id = "";

        [SerializeField]
        protected bool validGrammar;

        [SerializeField]
        protected bool validConnections;

        [SerializeField, HideInInspector]
        protected int x;

        [SerializeField, HideInInspector]
        protected int y;

        [SerializeField, SerializeReference]
        protected QuestGraph graph;

        [SerializeField]
        protected Rect nodeViewRect;

        #endregion

        #region PROPERTIES
        public QuestGraph Graph
        {
            get => graph;
            set => graph = value;
        }
        
        public string ID
        {
            get => id;
            set => id = value;
        }
        
        public Vector2Int Position
        {
            get => new(x, y);
            set
            {
                x = value.x;
                y = value.y;
            }
        }
        
        public bool ValidGrammar
        {
            get => validGrammar;
            set => validGrammar = value;
        }
        
        public bool ValidConnections
        {
            get => validConnections;
            set => validConnections = value;
        }
        
        public Rect NodeViewPosition
        {
            get => nodeViewRect;
            set
            {
                // to avoid assigning the view Rect that's undefined (the visual element is being laid out)
                if (!float.IsFinite(value.size.x) || !float.IsFinite(value.size.y)) return;
                if (value.size == Vector2.zero || value.size == Vector2.one) return;

                nodeViewRect = value;
            }

        }

        #endregion

        #region CONSTRUCTORS
        protected GraphNode() { }

        protected GraphNode(string id, Vector2 position, QuestGraph graph)
        {
            this.id = id;
            x = (int)position.x;
            y = (int)position.y;
            this.graph = graph;
            validGrammar = false;
            nodeViewRect = new Rect(position, Vector2.zero);
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Selects the node as the active node in the graph
        /// </summary>
        /// <param name="reselect">will call all the delegates when a new node is selected, even if its already selected</param>
        public void Select(bool reselect = false)
        {
            Graph.SelectedGraphNode = this;
            if (reselect) Graph.Reselect();
        }

        public object Clone()
        {
            var clone = CreateCloneInstance();

            // Ensure ID is copied for all nodes
            clone.ID = ID;

            clone.validGrammar = validGrammar;
            clone.validConnections = validConnections;
            clone.nodeViewRect = nodeViewRect;
            clone.x = x;
            clone.y = y;
            clone.graph = graph;

            return clone;
        }

        public override bool Equals(object obj)
        {
            if(obj is not GraphNode ode) return false;
            return ID == ode.ID;
        }
        
        public override int GetHashCode()
        {
            return ID?.GetHashCode() ?? 0;
        }

        protected abstract GraphNode CreateCloneInstance();

        public abstract bool IsValid();

        public bool Equal(GraphNode other)
        {
            return nodeViewRect == other.nodeViewRect;
        }
        #endregion
    }

    // Represents an OR logic node in a quest graph
    [Serializable]
    public class OrNode : GraphNode
    {
        public OrNode(string id, Vector2 position, QuestGraph graph) : base(id, position, graph) { }

        protected override GraphNode CreateCloneInstance()
        {
            return new OrNode(ID, Position, graph);
        }

        public override bool IsValid()
        {
            return ValidConnections;
        }
    }

    // Represents an AND logic node in a quest graph
    [Serializable]
    public class AndNode : GraphNode
    {
        public AndNode(string id, Vector2 position, QuestGraph graph) : base(id, position, graph) { }

        protected override GraphNode CreateCloneInstance()
        {
            return new AndNode(ID, Position, graph);
        }

        public override bool IsValid()
        {
            return ValidConnections;
        }
    }

    // Represents a quest node with specific action and state
    [Serializable]
    public class QuestNode : GraphNode
    {
        // Defines the type of quest node (Start, Middle, Goal)
        public enum ENodeType
        {
            Start, Middle, Goal
        }

        #region FIELDS
        [SerializeField, SerializeReference, JsonRequired]
        private QuestNodeData data;

        [SerializeField, JsonRequired]
        private string terminalID = "";

        [SerializeField, JsonRequired]
        private ENodeType nodeType;

        [SerializeField, JsonRequired]
        private QuestState questState = QuestState.Blocked;
        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public QuestNodeData Data
        {
            get => data;
            set => data = value;
        }

        [JsonIgnore]
        public string TerminalID
        {
            get => terminalID;
            set => terminalID = value;
        }

        [JsonIgnore]
        public ENodeType NodeType
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
        #endregion

        #region CONSTRUCTORS
        private QuestNode()
        { }

        public QuestNode(string id, Vector2 position, string action, QuestGraph graph) : base(id, position, graph)
        {
            terminalID = action;
            nodeType = ENodeType.Middle;

            data = new QuestNodeData(this, graph.Grammar.GetTerminal(action));
        }
        #endregion

        #region METHODS

        protected override GraphNode CreateCloneInstance()
        {
            var clone = new QuestNode(ID, Position, terminalID, graph)
            {
                nodeType = nodeType,
                questState = questState,
                data = data
            };
            return clone;
        }

        public override string ToString()
        {
            return terminalID;
        }

        public override bool IsValid()
        {
            return ValidConnections & ValidGrammar;
        }

        #endregion
    }

}
