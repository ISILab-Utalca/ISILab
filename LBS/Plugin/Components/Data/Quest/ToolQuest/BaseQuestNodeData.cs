using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ISILab.Extensions;
using ISILab.LBS.Modules;
using ISILab.LBS.Settings;
using ISILab.Macros;
using LBS.Bundles;
using LBS.Components;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ISILab.LBS.Components
{
 
    [Serializable]
    public struct UnityActionStored : IEquatable<UnityActionStored>
    {
        [SerializeField]
        public string objectName;
        [SerializeField]
        public string componentName;
        [SerializeField]
        public string methodName;
        [SerializeField]
        public UnityAction action;
        
        
        public UnityActionStored((GameObject, Component, MethodInfo) methodInfo, UnityAction action = null)
        {
            (GameObject target, Component comp, MethodInfo method) = methodInfo;
            
            objectName = target.name;
            componentName = comp.GetType().Name;
            methodName = method.Name;
            this.action = action;
        }
        
        public bool Equals(UnityActionStored other)
        {
            return objectName == other.objectName && componentName == other.componentName && methodName == other.methodName;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityActionStored other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(objectName, componentName, methodName);
        }
    }
    
    /// <summary>
    /// <para><b>FOR LBS USER</b></para>
    /// 
    /// <para>
    /// This class is meant to store data for terminal actions<c>string</c> declared in an
    /// Scriptable Object<c>LBSGrammar</c>.
    /// If the grammar is modified (actions) or a new grammar is assigned, 
    /// new children of this class must be created.
    /// </para>
    /// 
    /// <para>
    /// To assign data, create your own VisualElement child of <c>NodeEditor</c>
    /// </para>
    /// 
    /// <para>
    /// Remember to update the <see cref="QuestNodeDataFactory"/> by adding 
    /// an action <c>string</c> and its  
    /// <see cref="BaseQuestNodeData"/> child class.
    /// </para>
    /// </summary>
   [Serializable]
    public abstract class BaseQuestNodeData
    {
        #region FIELDS

        [SerializeField, SerializeReference, JsonRequired] protected QuestNode ownerNode;
        [SerializeField, JsonRequired] protected string tag;
        [SerializeField, JsonRequired] protected Rect area;
        [SerializeField, JsonRequired] protected Color color = LBSSettings.Instance.view.behavioursColor;
        [SerializeField, JsonRequired] protected string iconGuid = LocationIcon;

        [SerializeField] private List<UnityActionStored> _registeredListeners = new();
        [SerializeField] private UnityEvent onCompleteEvent = new();

        private GameObject _target;        // Not serialized
        [SerializeField] private string targetName;
        [SerializeField] private string sceneGuid;

        // Icons
        protected static string LocationIcon = "efd5e48bd83c08d469fcc341c886b38b";
        protected static string StarIcon = "99b7816ce61fd85449ad2379f39bb8c2";
        protected static string FoeIcon = "d0baea4f8bdb0c948887aed23edd4cad";
        protected static string ObjectIcon = "699cc90614aad8047875eb0fae8b175f";

        #endregion



        #region PROPERTIES

        public List<UnityActionStored> RegisteredListeners
        {
            get
            {
                _registeredListeners ??= new List<UnityActionStored>();
                return _registeredListeners;
            }
        }

        public UnityEvent OnCompleteEvent
        {
            get
            {
                onCompleteEvent ??= new UnityEvent();
                return onCompleteEvent;
            }
        }

        public GameObject Target
        {
            get
            {
                if (_target is null)
                {
                    var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                    // Optional: ensure the stored scene GUID matches the currently open scene
                    string currentSceneGuid = AssetDatabase.AssetPathToGUID(currentScene.path);
                    if (currentSceneGuid == sceneGuid)
                    {
                        Target = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                            .FirstOrDefault(o => o.name == targetName);
                    }
                }

                if (_target is not GameObject) Target = null;
                return _target;
            }
            set
            {
                if (value is not GameObject) return;
                _target = value;
                ResaveTargetValues()?.Invoke();
            }
        }

        public QuestNode OwnerNode => ownerNode;
        public QuestGraph Graph => ownerNode.Graph;
        public LBSLayer Layer => Graph.OwnerLayer;
        public string Tag => tag;

        public Rect Area
        {
            get => area;
            set => area = value;
        }

        public Color Color => color;
        public string ID => OwnerNode.ID;

        #endregion



        #region CONSTRUCTOR

        protected BaseQuestNodeData(QuestNode ownerNode, string tag)
        {
            EditorApplication.quitting += ResaveTargetValues();

            this.ownerNode = ownerNode;
            this.tag = tag;

            if (ownerNode?.Graph?.OwnerLayer == null) return;

            onCompleteEvent = new UnityEvent();
            _registeredListeners = new List<UnityActionStored>();

            Vector2Int pos = ownerNode.Graph.OwnerLayer.ToFixedPosition(ownerNode.Position);
            area = new Rect(pos.x, pos.y, 1, 1);
        }

        #endregion

        #region METHODS

        #region CLONE / VALIDATION

        public virtual void Clone(BaseQuestNodeData data)
        {
            ownerNode = data.ownerNode;
            tag = data.tag;
            area = data.area;
            _target = data.Target;
            onCompleteEvent = data.OnCompleteEvent;
            _registeredListeners = data._registeredListeners;
        }

        public virtual List<string> ReferencedLayerNames() => null;
        public virtual void Resize() { }

        public abstract bool Equals(BaseQuestNodeData other);
        public abstract bool IsValid();

        #endregion
        
        #region DATA

        public VectorImage GetIcon()
        {
            return LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
        }
        
        /// <summary>
        /// Used in the quest assistant, assign data to the node by passing tiles
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="tiles"></param>
        public abstract void SetDataByTiles(List<LBSLayer> layers, List<TileBundleGroup> tiles);
        
        protected void ResizeToFitBundles(IEnumerable<BundleGraph> bundles)
        {
            List<Rect> validRects = bundles
                .Where(b => b != null && b.Valid())
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
            ref List<BundleGraph> listVar,
            HashSet<Bundle.EElementFlag> flags,
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

                listVar.Add(new BundleGraph(this, graphData.Item1, graphData.Item2));
            }

            return listVar.Any();
        }


        protected bool TrySetBundleGraph(
            List<LBSLayer> layers,
            List<TileBundleGroup> suggestions,
            ref BundleGraph graphVar,
            HashSet<Bundle.EElementFlag> flags,
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

                graphVar = new BundleGraph(this, graphData.Item1, graphData.Item2);
                if (graphVar != null) return true;
            }

            return false;
        }


        protected bool TrySetBundleType(
            List<LBSLayer> layers,
            List<TileBundleGroup> suggestions,
            ref BundleType typeVar,
            HashSet<Bundle.EElementFlag> flags,
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

                typeVar = new BundleType(null, suggestionTile);
                if (typeVar != null) return true;
            }

            return false;
        }

        #endregion
        
        #region ON COMPLETE TARGET SAVE

        /// <summary>
        /// Used to keep GameObject name + Scene GUID in sync before editor shutdown.
        /// </summary>
        private Action ResaveTargetValues()
        {
            return () =>
            {
                if (Target != null)
                {
                    targetName = Target.name;
                    SetSceneGuid(Target);
                }
            };
        }

        private void SetSceneGuid(GameObject value)
        {
            string scenePath = value.scene.path;
            sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        }

#endregion

        #endregion
    }


  
} 