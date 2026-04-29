using ISILab.AI.Grammar;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Components
{

    [Serializable]
    public class QuestNodeData : ScriptableObject
    {
        #region FIELDS

        [SerializeField, SerializeReference]
        protected QuestNode ownerNode;

        [SerializeField] 
        protected Rect area;

        [SerializeField] 
        private LBSEventHooker _eventHooker;

        // terminal from which we obtain color/icons
        [SerializeField] private string terminalGUID;
        private GrammarTerminal terminal;

        [SerializeReference] 
        private List<GrammarField> fields = new();

        #endregion

        #region ACTIONS

        public Action OnBeginChange;
        public Action OnEndChange;

        #endregion

        #region PROPERTIES
        public List<GrammarField> Fields => fields;
        public GrammarTerminal Terminal
        {
            get
            {
                terminal = terminal != null ? terminal : LBSAssetMacro.LoadAssetByGuid<GrammarTerminal>(terminalGUID);
                return terminal;
            }

            set
            {
                terminal = value;
                terminalGUID = LBSAssetMacro.GetGuidFromAsset(terminal);
            }
        }

        public LBSEventHooker EventHooker => _eventHooker;
        public QuestNode Node => ownerNode;
        public QuestGraph Graph => ownerNode.Graph;
        public LBSLayer OwnerLayer => Graph.OwnerLayer;

        public Rect Area
        {
            get => area;
            set
            {
                area = value;
                OnDataChanged?.Invoke(this);
            }
        }

        public string ID => Node.ID;

        #endregion

        #region CONSTRUCTOR

        public QuestNodeData(QuestNode ownerNode, GrammarTerminal terminal)
        {
            this.ownerNode = ownerNode;
            this.Terminal = terminal;

            foreach(var field in Terminal.fields)
            {
                fields.Add((GrammarField)field.Clone());
            }

            Vector2Int pos = ownerNode.Graph.OwnerLayer.ToFixedPosition(ownerNode.Position);
            area = new Rect(pos.x, pos.y, 1, 1);

            // all data actions must have the event hooker by default
            _eventHooker = new LBSEventHooker();

            // bind actions for Ctrl+Z
            var ndb = Graph.OwnerLayer.GetBehaviour<NodeDataBehaviour>();
            OnBeginChange += () => ndb.OnNodeDataChangedBegin?.Invoke(this); 
            OnEndChange += () => ndb.OnNodeDataChangedEnd?.Invoke(this);
        }

        #endregion

        public Action<QuestNodeData> OnDataChanged;

        #region METHODS

        public virtual void Clone(QuestNodeData data)
        {
            ownerNode = data.ownerNode;
            area = data.area;
            fields = data.fields;
            _eventHooker = data._eventHooker;
            Terminal = data.Terminal;
        }

        public override bool Equals(object obj)
        {
            var other = obj as QuestNodeData;
            return other != null && ID == other.ID;
        }
        public bool IsValid() { return true; }

        public override int GetHashCode()
        {
            return  ownerNode.GetHashCode() + ID.GetHashCode() + Graph.GetHashCode();
        }

        #region DATA
        public List<string> ReferencedLayerNames()
        {
            Debug.LogWarning("referneced layer names not implemented for " + GetType().Name);
            return new List<string>();
        }
        public void Resize()
        {
            Debug.LogWarning("Resize not implemented for " + GetType().Name);
        }
        public void SetDataByTiles(List<LBSLayer> layers, List<TileBundleGroup> tiles)
        {
            Debug.LogWarning("SetDataByTiles not implemented for " + GetType().Name);
        }

        protected void ResizeToFitBundles(IEnumerable<BundleTargetGraph> bundles)
        {
            List<Rect> validRects = bundles
                .Where(b => b != null && b.IsValid())
                .Select(b => b.Area)
                .ToList();

            if (validRects.Count == 0) return;

            float minX = validRects.Min(r => r.x);
            float maxX = validRects.Max(r => r.x + r.width);
            float minY = validRects.Min(r => r.y - r.height);
            float maxY = validRects.Max(r => r.y);

            float width = maxX - minX;
            float height = maxY - minY;

            area = new Rect(minX, maxY, Mathf.Abs(width), Mathf.Abs(height));
        }

        #endregion

        #region TILE + BUNDLE HELPERS

        protected bool TrySetBundleGraphList(
            List<LBSLayer> layers,
            List<TileBundleGroup> suggestions,
            ref List<BundleTargetGraph> listVar,
            Bundle.EElementFlag[] flags,
            bool bRequireAllTags = false)
        {
            foreach (TileBundleGroup suggestionTile in suggestions)
            {
                Tuple<LBSLayer, TileBundleGroup> graphData = LBSLayerHelper.GetBundleTileByPosition(suggestionTile.AreaRect.position.ToInt(), layers);
                if (graphData is null) continue;

                Bundle bundle = graphData.Item2.BundleData.Bundle;

                bool bTagRequirement =
                    bRequireAllTags ? bundle.HasAllFlags(flags) : bundle.HasAnyFlag(flags);

                if (!bTagRequirement) continue;

                listVar.Add(new BundleTargetGraph(graphData.Item1, graphData.Item2));
            }

            return listVar.Any();
        }


        protected bool TrySetBundleGraph(
            List<LBSLayer> layers,
            List<TileBundleGroup> suggestions,
            ref BundleTargetGraph graphVar,
            Bundle.EElementFlag[] flags,
            bool bRequireAllTags = false)
        {
            foreach (TileBundleGroup suggestionTile in suggestions)
            {
                Tuple<LBSLayer, TileBundleGroup> graphData = LBSLayerHelper.GetBundleTileByPosition(suggestionTile.AreaRect.position.ToInt(), layers);
                if (graphData is null) continue;

                Bundle bundle = graphData.Item2.BundleData.Bundle;
                bool bTagRequirement =
                    bRequireAllTags ? bundle.HasAllFlags(flags) : bundle.HasAnyFlag(flags);

                if (!bTagRequirement) continue;

                graphVar = new BundleTargetGraph(graphData.Item1, graphData.Item2);
                if (graphVar != null) return true;
            }

            return false;
        }


        protected bool TrySetBundleType(
            List<LBSLayer> layers,
            List<TileBundleGroup> suggestions,
            ref BundleTarget typeVar,
            Bundle.EElementFlag[] flags,
            bool bRequireAllTags = false)
        {
            foreach (TileBundleGroup suggestionTile in suggestions)
            {
                Tuple<LBSLayer, TileBundleGroup> graphData = LBSLayerHelper.GetBundleTileByPosition(suggestionTile.AreaRect.position.ToInt(), layers);
                if (graphData is null) continue;

                Bundle bundle = graphData.Item2.BundleData.Bundle;
                bool bTagRequirement =
                    bRequireAllTags ? bundle.HasAllFlags(flags) : bundle.HasAnyFlag(flags);

                if (!bTagRequirement) continue;

                typeVar = new BundleTarget(suggestionTile);
                if (typeVar != null) return true;
            }

            return false;
        }

        public void SetArea(Rect newValue)
        {
            OnBeginChange.Invoke();
            Area = newValue;
            OnEndChange.Invoke();
        }


        #endregion



        #endregion
    }



}