using ISILab.Extensions;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    public enum GraphNodeType
    {
        Start, Middle, Goal
    }

    [Serializable]
    public class Graph : LBSModule, ICloneable
    {
  
        #region FIELDS
        [SerializeReference, JsonRequired]
        private List<object> _nodes = new();
       
        [SerializeReference, JsonRequired]
        private List<Edge> _edges = new();

        [SerializeReference]
        private object root;

        private object selected;

        #endregion

        #region PROPERTIES
        public object Root => root;
        public List<object> Nodes => _nodes;

        public List<Edge> Edges => _edges;

        /// <summary>
        /// The active selected graph node by the user.
        /// </summary>
        public object Selected
        {
            get => selected;
            set
            {
                if (value is not null && Selected is not null)
                {
                    if (value.Equals(Selected)) return;
                }

                // assign if its null or it is a graphnode contained in the existing nodes
                if (value == null || (value is not null && Nodes.Contains(value)))
                {
                    // deselect the previous node
                    if(selected != null) OnDeselect?.Invoke(selected);

                    selected = value;
                    Reselect();

                }
            }
        }

        #endregion

        #region ACTIONS

        public Action OnForceUpdate;
       
        public Action<object> OnSelect;
        public Action<object> OnDeselect;

        /// <summary>
        /// Old root, New Root
        /// </summary>
        public Action<object, object> OnNewRoot;

        public Action<object> OnAddNode;
        public Action<object> OnRemoveNode;

        public Action<Edge> PreAddEdge;
        public Action<Edge> PreRemoveEdge;

        public Action<Vector2Int> GraphPosition;
        internal Action PostEdgesChange;

        #endregion

        #region CONSTRUCTOR
        public Graph()
        {

        }

        #endregion

        #region METHODS


        #region Nodes
        
        public List<T> GetNodes<T>() where T : class
        {
            if (_nodes == null) return new List<T>();

            return _nodes.OfType<T>().ToList();
        }

        public void Reselect() =>
            // delegeates related to the graph node selection
            OnSelect?.Invoke(selected);
          


        
        public void AddNode(object node, bool fillEmptyRoot = true)
        {
            _nodes.Add(node);

            if (fillEmptyRoot && root == null)
            {
                SetRoot(node);
            }

            Selected = node;
            OnAddNode?.Invoke(node);
        }

        public void RemoveNode(object node)
        {
            _nodes.Remove(node);
            
            foreach (var e in GetEdgesWithNode(node))
            {
                RemoveEdge(e); 
            }

            if (Equals(node, root)) 
                SetRoot(null);

            OnRemoveNode?.Invoke(node);

        }
        #endregion

        #region Edges
        
        /// <summary>
        /// Checks to avoid loops in the graph by traversing it from the destination node to the source node, if it finds the source node again it means there is a loop and the connection should not be added.
        /// </summary>
        /// <param name="origin">starting node</param>
        /// <param name="current">iteration node</param>
        /// <param name="visited">set of marked visited nodes</param>
        /// <returns>true if a loop is found, false otherwise</returns>
        public bool IsLooped(object origin, object current, HashSet<object> visited)
        {
            if (Equals(origin, current))
                return true;

            if (!visited.Add(current))
                return false;

            // Traverse *forward only* (branches)
            foreach (Edge branch in GetBranches(current))
            {
                if (IsLooped(origin, branch.To, visited))
                    return true;
            }
  
            return false;
        }

        public Tuple<string, LogType> AddEdge(object from, object to)
        {
            Edge newEdge = new Edge(from, to);
            _edges.Add(newEdge);
            PreAddEdge?.Invoke(newEdge);

            PostEdgesChange?.Invoke();

            return Tuple.Create($"Connection: {from.ToString()} → {to.ToString()}", LogType.Log);
        }


        public bool RemoveEdge(Edge edge)
        {
            if (edge == null) return false;
            PreRemoveEdge?.Invoke(edge);
            _edges.Remove(edge);

            PostEdgesChange?.Invoke();

            return true;
        }

  

        /// <summary>
        /// Finds any <see cref="Edge"/> where either the source or destination is the param node
        /// </summary>
        /// <param name="node">node to find</param>
        /// <returns>list of edges</returns>
        private List<Edge> GetEdgesWithNode(object node) =>
            _edges.Where(e => e.From == node || e.To.Equals(node)).ToList();

        /// <summary>
        /// Finds all the <see cref="Edge"/> where the param node is the source, so it can be considered as the branches of that node.
        /// </summary>
        /// <param name="node">source node</param>
        /// <returns>list of edges</returns>
        public List<Edge> GetBranches(object node)
        {
            List<Edge> list = new List<Edge>();
            
            if (!_nodes.Contains(node)) 
                return list;
            
            foreach (Edge edge in _edges)
            {
                // root found -> can obtain branches from it
                if (!Equals(edge.From, node))
                    continue;

                if (!_nodes.Contains(edge.To)) 
                    continue;

                if (!_nodes.Contains(edge.From))
                    continue;

                list.Add(edge);
            }

            return list;
        }

        /// <summary>
        /// Finds all the <see cref="Edge"/> that have the param node as destination.
        /// </summary>
        /// <param name="node">destination node</param>
        /// <returns>list of edges</returns>
        public List<Edge> GetRoots(object node)
        {
            List<Edge> valid = new List<Edge>();

            foreach (Edge edge in _edges)
            {
                // to found -> can obtain roots from it
                if (!Equals(edge.To, node)) 
                    continue;
                
                if (!_nodes.Contains(edge.To)) 
                    continue;

                if (!_nodes.Contains(edge.From))
                    continue;

                valid.Add(edge);
            }

            return valid;
        }

           
        #endregion
        
        
        #region Root
        /// <summary>
        /// Assigns a new root to the graph. It must be a <see cref="QuestNode"/>
        /// </summary>
        /// <param name="node">The node to set as root</param>
        public void SetRoot(object node)
        {
            if (node == root) return;

            var oldRoot = root;

            root = node;
            OnNewRoot?.Invoke(oldRoot, root);
            OnForceUpdate?.Invoke();
        }


        #endregion

        #region Clone & Utils

        /// <summary>
        /// Check if there are any nodes in the graph
        /// </summary>
        /// <returns></returns>
        public override bool IsEmpty() => _nodes.Count == 0;

        public override object Clone()
        {
            var clone = new Graph();

            // node cloning
            var nodes = _nodes.Select(CloneRefs.Get);
            foreach (var n in nodes)
            {
                if (Root.Equals(n))
                {
                    clone.root = n;
                }
                clone._nodes.Add(n);
            }

            // edge cloning
            var edges = _edges.Select(CloneRefs.Get).Cast<Edge>();
            foreach (Edge e in edges) clone._edges.Add(e);

            return clone;
        }



        #endregion

        public void ChangeConnection(Edge edge, NodeKind kind) =>
            throw new NotImplementedException();

        public override void Print() => throw new NotImplementedException();
        public override void Clear() => throw new NotImplementedException();
        public override Rect GetBounds() => throw new NotImplementedException();
        public override void Rewrite(LBSModule other) => throw new NotImplementedException();

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is not Graph other) return false;

            if (OwnerLayer != null && other.OwnerLayer != null)
                return OwnerLayer.Equals(other.OwnerLayer);

            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode() => OwnerLayer != null ?
                OwnerLayer.GetHashCode() : base.GetHashCode();
    }
}
