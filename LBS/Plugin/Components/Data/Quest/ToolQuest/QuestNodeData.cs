using ISILab.AI.Grammar;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ISILab.LBS.Components
{

    [Serializable]
    public class QuestNodeData
    {
        #region FIELDS

        [SerializeReference]
        protected QuestNode ownerNode;

        // terminal from which we obtain color/icons
        [SerializeField]
        private string terminalGUID;

        [SerializeField]
        private long terminalLocalID;
        
        [SerializeReference] 
        private List<GrammarField> fields = new();

        // for direct acccess but it exists within fields
        [SerializeReference]
        private GrammarEventHook _eventHookerField;

        // for direct acccess but it exists within fields
        [SerializeReference]
        private GrammarArea _areaField;

        #endregion

        #region ACTIONS

        public Action OnBeginChange;
        public Action OnEndChange;

        public Action<QuestNodeData> OnDataChanged;

        #endregion

        #region PROPERTIES
        public List<GrammarField> Fields => fields;


        public GrammarTerminal Terminal
        {
            get
            {
                if (string.IsNullOrEmpty(terminalGUID))
                    return null;

                string path = AssetDatabase.GUIDToAssetPath(terminalGUID);

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (var asset in assets)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        asset,
                        out _,
                        out long localID);

                    if (localID == terminalLocalID)
                        return asset as GrammarTerminal;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    terminalGUID = string.Empty;
                    terminalLocalID = 0;
                    return;
                }

                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value,
                    out terminalGUID,
                    out terminalLocalID);
            }
        }


    public string ID => Node.ID;
        public QuestNode Node => ownerNode;
        public Graph Graph => ownerNode.Graph;
        public LBSLayer OwnerLayer => Graph.OwnerLayer;
        public LBSEventHooker EventHooker => _eventHookerField.GetValue() as LBSEventHooker;
        public GrammarArea Area => _areaField;

        public QuestNodeData Data { get; internal set; }


        #endregion

        #region CONSTRUCTOR

        public QuestNodeData(QuestNode ownerNode, GrammarTerminal terminal)
        {

            this.ownerNode = ownerNode;
            Terminal = terminal;

            _areaField = new GrammarArea { Data = this, name = "Area" };
            Vector2Int pos = ownerNode.Graph.OwnerLayer.ToFixedPosition(ownerNode.Position);
            _areaField.SetValue(new Rect(pos.x, pos.y, 1, 1));

            _eventHookerField = new GrammarEventHook { Data = this, name = "Events" };

            fields = new();

            fields.Add(_areaField);
            fields.Add(_eventHookerField);

            foreach (var field in Terminal.fields)
                fields.Add((GrammarField)field.Clone());

            foreach (var field in fields)
                field.Data = this;

            // bind actions for Ctrl+Z
            var ndb = Graph.OwnerLayer.GetBehaviour<NodeDataBehaviour>();
            OnBeginChange += () => ndb.OnNodeDataChangedBegin?.Invoke(this);
            OnEndChange += () => ndb.OnNodeDataChangedEnd?.Invoke(this);
        }

        #endregion

        #region METHODS

        public virtual void Clone(QuestNodeData data)
        {
            ownerNode = data.ownerNode;

            fields = data.fields
                .Select(f => (GrammarField)f.Clone())
                .ToList();

            _areaField = fields.OfType<GrammarArea>().FirstOrDefault();
            _eventHookerField = fields.OfType<GrammarEventHook>().FirstOrDefault();

            Terminal = data.Terminal;
        }

        public override bool Equals(object obj)
        {
            var other = obj as QuestNodeData;
            return other != null && ID == other.ID;
        }
        public bool IsValid() 
        {
            foreach(var field in GetFields<GrammarField>())
            {
                if (!field.IsValid()) return false;
            }
            return true;
        }

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
        public void ApplyTilesToData(QuestCandidate data)
        {
            // assign tiles to any fields that implement IBundleFlags
            foreach (var field in fields) 
            {
                if (field is IBundleStored IBundleFlag)
                {
                    foreach (var tile in data.Tiles)
                    {
                        var bundle = tile.BundleData.Bundle;
                        if (IBundleFlag.HasAnyFlag(bundle))
                        {
                            var bt = new BundleTargetGraph(data.ContextLayer, tile);
                            field.SetValue(bt);

                        }
                    }
                }
            }

            // set the area size to contain all bundles in the layer
            var bundleTargetFields = GetFields<GrammarBundleGraph>();
            List<BundleTargetGraph> bundleTargets = new();
            foreach (var field in bundleTargetFields)
            {
                if(field.GetValue() is BundleTargetGraph btg) 
                {
                    bundleTargets.Add(btg);
                }
            }

            ResizeToFitBundles(bundleTargets);
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

            _areaField.SetValue(new Rect(minX, maxY, Mathf.Abs(width), Mathf.Abs(height)));
        }

        public List<T> GetFields<T>() where T : GrammarField
        {
            var result = fields.OfType<T>().ToList();
            foreach (var field in fields)
            {
                if (field is GrammarListFieldMarker listField && field.ItemsSource != null)
                {
                    result.AddRange(field.ItemsSource.Cast<GrammarField>().OfType<T>());
                }
            }

            return result;
        }

        public T GetField<T>() where T : GrammarField => GetFields<T>().FirstOrDefault();
        public GrammarField GetField(string fieldName) => fields.FirstOrDefault(f => f.name == fieldName);
        public T GetField<T>(string fieldName) where T : GrammarField => fields
                .OfType<T>()
                .FirstOrDefault(f => f.name == fieldName);

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

        public void SetArea(Rect newValue) => _areaField.SetValue(newValue);



        #endregion



        #endregion
    }



}