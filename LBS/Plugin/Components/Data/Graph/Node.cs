using ISILab.LBS.Modules;
using System;
using UnityEngine;

namespace ISILab.LBS.Components
{
    // Determines the state of a quest
    public enum NodeKind
    {
        Terminal, Or, And
    }

    /// <summary>
    /// Base node with position and graph connection
    /// </summary>
    [Serializable]
    public class Node : ICloneable
    {

        #region FIELDS


        [SerializeField]
        protected string id = string.Empty;

        [SerializeField, HideInInspector]
        protected int x;

        [SerializeField, HideInInspector]
        protected int y;

        [SerializeField, SerializeReference]
        protected Graph graph;

        [SerializeField]
        protected Rect area;

        [SerializeField, HideInInspector]
        private bool validConnections;

        [SerializeField]
        protected NodeKind kind;

        #endregion

        #region PROPERTIES
        public NodeKind Kind => kind;

        public string ID => id;

        public Graph Graph => graph;

        /// <summary>
        /// The cell position in the grid
        /// </summary>
        public Vector2Int Position
        {
            get => new(x, y);
            set
            {
                x = value.x;
                y = value.y;
            }
        }

        /// <summary>
        /// The position of the node visual element.
        /// </summary>
        public Rect Area
        {
            get => area;
            set
            {
                // to avoid assigning the view Rect that's undefined (the visual element is being laid out)
                if (!float.IsFinite(value.size.x) || !float.IsFinite(value.size.y)) return;
                if (value.size == Vector2.zero || value.size == Vector2.one) return;

                area = value;
            }

        }

        public bool ValidConnections
        {
            get => validConnections;
            set => validConnections = value;
        }

        #endregion

        public Action OnSelect;
        public Action OnDeselect;

        #region CONSTRUCTORS
        protected Node() { }

        protected Node(Vector2 position, Graph graph)
        {
            this.graph = graph;

            x = (int)position.x;
            y = (int)position.y;
            area = new Rect(position, Vector2.zero);
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Selects the node as the active node in the graph
        /// </summary>
        /// <param name="forceReselect">will call all the delegates when a new node is selected, even if its already selected</param>
        public void Select(bool forceReselect = false)
        {
            // node already selected, force delegate calls
            if (forceReselect && this == Graph.Selected)
                Graph.Reselect();

            // normal selection
            else
                Graph.Selected = this;

        }

        public virtual object Clone()
        {
            var clone = new Node();
            clone.Area = Area;
            clone.Position = Position;
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Node other) 
                return false;

            return other.ID == ID;
        }

        public override int GetHashCode() => ID.GetHashCode();

        public virtual bool IsValid() => validConnections;

        public bool IsSelected() => Graph.Selected == this;

        public override string ToString() => $"node {x},{y}";
        #endregion
    }


}
