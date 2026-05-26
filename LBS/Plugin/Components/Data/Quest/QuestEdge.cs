using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    [Serializable]
    public class QuestEdge : ICloneable
    {
        #region FIELDS
        
        [SerializeField, SerializeReference, JsonRequired]
        private GraphNode from;

        [SerializeField, SerializeReference, JsonRequired]
        private GraphNode to;
        #endregion
        
        #region PROPERTIES
        
        [JsonIgnore]
        public GraphNode From
        {
            get => from;
            set => from = value;
        }

        [JsonIgnore]
        public GraphNode To
        {
            get => to;
            set => to = value;
        }
        #endregion

        #region CONSTRUCTORS
        public QuestEdge()
        {
        }

        public QuestEdge(GraphNode from, GraphNode to)
        {
            this.from = from;
            this.to = to;
        }

        #endregion

        #region METHODS
        public object Clone()
        {
            var clonedFrom = CloneRefs.Get(from) as GraphNode;
            var clonedTo = CloneRefs.Get(to) as GraphNode;

            return new QuestEdge
            {
                From = clonedFrom,
                To = clonedTo
            };
        }

        #endregion
    }
}
