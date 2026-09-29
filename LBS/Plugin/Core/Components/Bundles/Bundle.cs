#if UNITY_EDITOR
using ISILab.DevTools.Macros;
#endif
using ISILab.LBS.Characteristics;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles.Tools;
using ISILab.LBS.Plugin.Internal;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


namespace ISILab.LBS.Plugin.Components.Bundles
{
    /// <summary>
    /// Specifies the layers for which a bundle is intended to operate.
    /// </summary>
    [System.Flags]
    public enum BundleFlags
    {
        None = 0,
        Interior = 1 << 0,
        Exterior = 1 << 1,
        Population = 1 << 2,
        Quest = 1 << 3,
        Simulation = 1 << 4
    }

    /// <summary>
    /// Indicates spatial positioning a bundle should adopt when instantiated.
    /// </summary>
    [System.Serializable]
    public enum Positioning
    {
        Center,
        Edge,
        Corner,
        Other
    }

    /// <summary>
    /// Container to configure a prefab GameObject.
    /// </summary>
    [System.Serializable]
    public class Asset : ICloneable
    {
        /// <summary>
        /// Prefab to instantiate.
        /// </summary>
        public GameObject obj;
        /// <summary>
        /// Weighted probability of instantiating.
        /// </summary>
        [Range(0f,1f)]
        public float probability;
        [HideInInspector]
        public string id = "";

        public string ID => (id != null && id != "") ? id : SetID(); 
        public Asset(GameObject obj, float probability)
        {
            this.obj = obj;
            this.probability = probability;
        }
        /// <summary>
        /// Sets a unique ID.
        /// </summary>
        /// <returns>The ID created.</returns>
        public string SetID()
        {
            id = Guid.NewGuid().ToString();
            return id;
        }
        public object Clone()
        {
            return new Asset(this.obj, this.probability);
        }

        public override bool Equals(object comp)
        {
            var other = comp as Asset;
            if (other == null) return false;
            if (other.obj != obj) return false;
            if (other.probability != probability) return false;
            if (other.ID != ID) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(obj, probability);
        }
    }

    /// <summary>
    /// Object that wraps game assets, models, prefabs and its metadata used by LBS to generate interior, exterior and population layer content.<br/>
    /// Bundles also contain <see cref="LBSCharacteristic"/>s that provides necesary information to function.
    /// </summary>
    //[CreateAssetMenu(fileName = "New Bundle", menuName = "ISILab/LBS/Bundle")] <- Replaced with BundleMenuItem
    [System.Serializable]
    public class Bundle : ScriptableObject, ICloneable
    {
        public Bundle()
        {
            layerContentFlags = BundleFlags.None;
        }

        public enum TagType
        {
            Aesthetic, // (Style)Ej: Castle, Spaceship,
            Structural, // Ej: Door, Wall, Corner,Stair
            Element, // Ej: Furniture, Enemies, 
                     // Distinction, // (characteristics)Ej: Destroyed, Blooded, Dirty,
            Simulation
        }
        
        [System.Flags]
        public enum EElementFlag
        {
            None      = 0,
            Character = 1 << 0, // player, npc, enemies
            Enemy     = 1 << 1 | Character, 
            Player    = 1 << 2 | Character,
            Ally      = 1 << 3 | Character,
            Item      = 1 << 4, // collectable type
            Resource  = 1 << 5 | Item,
            Equipment = 1 << 6 | Item,
            Interactable = 1 << 7, // buttons, doors, levers
            Trigger   = 1 << 8, // triggers 
            Prop = 1 << 9, // static mesh
            Misc = 1 << 10 // non categorized
        }

        #region FIELDS

        [FormerlySerializedAs("populationName")]
        [SerializeField]
        private string bundleName;

        // Add a flags field
        [FormerlySerializedAs("flags")]
        [SerializeReference, HideInInspector]
        private BundleFlags layerContentFlags;

        [SerializeField, HideInInspector, Obsolete("Use layer content flags instead.")]
        private TagType type;

        [SerializeReference, HideInInspector]
        private Positioning anchorPosition = Positioning.Center;

        [SerializeReference, HideInInspector]
        private Color color;

        [SerializeField, HideInInspector] 
        private string iconGuid;
        
        [SerializeReference, HideInInspector]
        private VectorImage icon;
        
        [SerializeField]
        private List<Asset> assets = new List<Asset>();

        [SerializeReference, HideInInspector]
        private List<LBSCharacteristic> characteristics = new List<LBSCharacteristic>();

        // only used if it's an element (population)
        [SerializeField,HideInInspector] 
        private EElementFlag elementFlag = EElementFlag.None;

        // Used in generation 3d.
        [SerializeField,HideInInspector] 
        private Vector2Int tileSize = Vector2Int.one;
        
        // hides in inspector and uses the custom GUI to assign only children with containing flags
        [SerializeField, HideInInspector]
        private List<Bundle> childsBundles = new List<Bundle>();
        
        [SerializeField]
        private MicroGenTool microGenTool = new MicroGenTool();

        [SerializeField, HideInInspector]
        private string guid;

        // Simulation
        [SerializeField, HideInInspector]
        protected EntityType entityType = EntityType.ET_NONE;
        [SerializeField, HideInInspector]
        protected List<EntityType> admissibleTypes = new List<EntityType>();

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Custom bundle name. If empty, it uses its object name.
        /// </summary>
        public string BundleName
        {
            get => string.IsNullOrEmpty(bundleName) ? Name : bundleName;
            set => bundleName = value;
        }

        /// <summary>
        /// Flags specifying the layers for which the bundle is intended to operate.
        /// </summary>
        public BundleFlags LayerContentFlags
        {
            get => layerContentFlags;
            set => layerContentFlags = value;
        }

        /// <summary>
        /// A representative icon of the bundle.
        /// </summary>
        public VectorImage Icon
        {
            get
            {
#if UNITY_EDITOR
                if (icon is not null)
                {
                    iconGuid = AssetMacro.GetGuidFromAsset(icon);
                }
                else
                {
                    icon = AssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
                }
#endif
                return icon;
            }
            set
            {
                icon = value;
#if UNITY_EDITOR
                iconGuid = AssetMacro.GetGuidFromAsset(icon);
#endif
            }
        }

        /// <summary>
        /// A representative color of the bundle.
        /// </summary>
        public Color Color
        {
            get => color;
            set => color = value;
        }
        /// <summary>
        /// Object name.
        /// </summary>
        public string Name => name;
        /// <summary>
        /// The prefabs that can be instantiated for this bundle.
        /// </summary>
        public List<Asset> Assets
        {
            get => new List<Asset>(assets);
            set => assets = value;
        }

        public Vector2Int TileSize => tileSize;
        
        /// <summary>
        /// Identifies this bundle as a population element.
        /// </summary>
        public EElementFlag ElementFlag => elementFlag;

        /// <summary>
        /// Provides identity, information and diferent functionalities to this bundle.
        /// </summary>
        public List<LBSCharacteristic> Characteristics => characteristics;

        /// <summary>
        /// Bundle units, used if this bundle is a Main Bundle.
        /// </summary>
        public List<Bundle> ChildsBundles => new List<Bundle>(childsBundles);

        
        public bool IsLeaf => (childsBundles.Count <= 0);

        /// <summary>
        /// Indicates spatial positioning the bundle should adopt when instantiated.
        /// </summary>
        public Positioning Positioning => anchorPosition;

        [Obsolete("Use layer content flags instead.")]
        public TagType Type
        {
            get => type;
            set => type = value;
        }

        /// <summary>
        /// Asset GUID.
        /// </summary>
        public string GUID
        {
            get => guid;
            set => guid = value;
        }

        /// <summary>
        /// Label utilized by Simulation agent. Determines how the agent will react to this instantiated bundle.
        /// </summary>
        public EntityType EntityType
        {
            get => entityType;
            //internal set // Quiza no deberia haber un setter, pero se usa en PathOSTag.ToLBSTag. No se si sirva de mucho pero lo dejare como internal por ahora
            //{
            //    if (entityType == value) return;

            //    entityType = value;
            //}
        }

        public List<EntityType> AdmissibleEntityTypes { get => admissibleTypes; }

#endregion

        #region EVENTS
        public event Action<Bundle> OnAddChild;
        public event Action<Bundle> OnRemoveChild;

        public event Action<Asset> OnAddAsset;
        public event Action<Asset> OnRemoveAsset;

        public event Action<LBSCharacteristic> OnAddCharacteristic;
        public event Action<LBSCharacteristic> OnRemoveCharacteristic;
        #endregion

        /// <summary>
        /// Searches the bundle hierarchy for all children bundles with a specific positioning value.
        /// </summary>
        /// <param name="positioning">The type of positioning serched for in children bundles.</param>
        /// <returns>Every descendant with the specified positioning.</returns>
        #region METHODS
        public List<Bundle> GetChildrenByPositioning(Positioning positioning)
        {
            var r = new List<Bundle>();
            foreach (Bundle child in childsBundles)
            {
                if(child.anchorPosition == positioning)
                    r.Add(child);

                r.AddRange(child.GetChildrenByPositioning(positioning));
            }
            return r;
        }

        /// <summary>
        /// Searches the bundle hierarchy for all children bundles with a specific tag.
        /// </summary>
        /// <param name="tag">The tag searched for in children bundles.</param>
        /// <returns>Every descendant with the specified tag.</returns>
        internal List<Bundle> GetChildrensByTag(string tag)
        {
            var r = new List<Bundle>();
            foreach (Bundle child in childsBundles)
            {
                if (child.name == tag)
                    r.Add(child);

                r.AddRange(child.GetChildrensByTag(tag));
            }
            return r;
        }

        /// <summary>
        /// Initializes every characteristic on this bundle.
        /// </summary>
        public void Reload()
        {
            foreach (LBSCharacteristic characteristic in characteristics)
            {
                if (characteristic != null)
                {
                    characteristic.Init(this);
                }
            }
        }

        /// <summary>
        /// Refreshes every characteristic on this bundle.
        /// </summary>
        public void Refresh()
        {
            foreach(LBSCharacteristic characteristic in Characteristics)
            {
                characteristic?.OnRefresh();
            }
        }

        /* Checks that a child to be added:
            - not in a child already
            - not parent of the current bundle
            - has at least one of the current bundle's flags
    
        */
        /// <summary>
        /// Indicates whether a bundle is allowed to become a child of this bundle.
        /// </summary>
        /// <param name="potentialChild">Desired child Bundle to verify.</param>
        /// <returns>
        /// True if the potential child shares the same flags as this bundle, is not part of this bundle's ascendance, and is not already a child of this bundle.<br/>
        /// False if the potential child has not the same flags as this bundle, or is an ascendant or child of this bundle.
        /// </returns>
        public bool IsBundleValidChild(Bundle potentialChild)
        {
            // Get all parent bundles to avoid recursion
            List<Bundle> parents = new List<Bundle>();
            Bundle currentParent = this;
            while (currentParent != null)
            {
                parents.Add(currentParent);
                currentParent = currentParent.Parent();
            }
            if (!potentialChild.LayerContentFlags.HasFlag(LayerContentFlags)) return false;
            if (parents.Contains(potentialChild))  return false;
            if (ChildsBundles.Contains(potentialChild)) return false;
            return true;
        }
        
        /// <summary>
        /// Adds a valid bundle to the children list of this bundle.
        /// </summary>
        /// <param name="child">The new child bundle.</param>
        public void AddChild(Bundle child)
        {
            if (IsRecursive(this, child))
            {
                UnityEngine.Debug.Log("[ISI Lab]: Bundle '" +
                    this.name + "' is contained in bundle '" +
                    child.name + "' or one of its child bundles.");
                return;
            }

            if (!IsBundleValidChild(child)) return;

            childsBundles.Add(child);
            OnAddChild?.Invoke(child);
        }

        /// <summary>
        /// Inserts at a specified position a valid bundle to the children list of this bundle.
        /// </summary>
        /// <param name="index">The position to insert the child bundle.</param>
        /// <param name="child">The new child bundle.</param>
        public void InsertChild(int index, Bundle child)
        {
            Assert.IsTrue(IsRecursive(this, child), "[ISI Lab]: Bundle '" + this.name + "' is contained in bundle '" + child.name + "' or one of its child bundles.");

            childsBundles.Insert(index, child);
            OnAddChild?.Invoke(child);
        }

        /// <summary>
        /// Removes a bundle from the children list of this bundle.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        public void RemoveChild(Bundle child)
        {
            if (childsBundles.Remove(child))
            {
                OnRemoveChild?.Invoke(child);
            }
        }
        
        /// <summary>
        /// Clears from the children list all null children bundles.
        /// </summary>
        public void RemoveNullChildren()
        {
            for (int i = 0; i < childsBundles.Count; i++)
            {
                if (childsBundles[i] == null)
                {
                    childsBundles.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Clears the entire children list.
        /// </summary>
        public void ClearChilds()
        {
            while (childsBundles.Count > 0)
            {
                Bundle last = childsBundles[^1];
                OnRemoveChild?.Invoke(this);
                childsBundles.Remove(last);
            }
        }

        /// <summary>
        /// Creates an <see cref="Asset"/> from a prefab and adds it to the assets list.
        /// </summary>
        /// <param name="obj">The prefab gameobject from which the asset will be created.</param>
        /// <param name="probability">Weighted probability of the prefab of being chosen when instantiating this bundle.</param>
        public void AddAsset(GameObject obj, float probability = .5f)
        {
            var asset = new Asset(obj, probability);
            assets.Add(asset);
            OnAddAsset?.Invoke(asset);
        }

        /// <summary>
        /// Adds a given <see cref="Asset"/> to the assets list.
        /// </summary>
        /// <param name="asset"></param>
        public void AddAsset(Asset asset)
        {
            assets.Add(asset);
            OnAddAsset?.Invoke(asset);
        }

        /// <summary>
        /// Replaces an asset at a specified position with another asset.
        /// </summary>
        /// <param name="index">Position of the asset to replace.</param>
        /// <param name="asset">New asset.</param>
        public void ReplaceAsset(int index, Asset asset)
        {
            if (index == -1)
                return;

            OnRemoveAsset?.Invoke(assets[index]);
            assets[index] = asset;
            OnAddAsset?.Invoke(asset);
        }

        /// <summary>
        /// Inserts a new <see cref="Asset"/> at a specified position.
        /// </summary>
        /// <param name="index">The position to insert the asset.</param>
        /// <param name="asset">The new asset.</param>
        public void InsertAsset(int index, Asset asset)
        {
            assets.Insert(index, asset);
            OnAddAsset?.Invoke(asset);
        }

        /// <summary>
        /// Removes an <see cref="Asset"/> from the assets list.
        /// </summary>
        /// <param name="asset">The asset to remove.</param>
        public void RemoveAsset(Asset asset)
        {
            if (assets.Remove(asset))
                OnRemoveAsset?.Invoke(asset);
        }

        /// <summary>
        /// Adds a new <see cref="LBSCharacteristic"/> to this bundle.
        /// </summary>
        /// <param name="characteristic">The new characteristic to add.</param>
        public void AddCharacteristic(LBSCharacteristic characteristic)
        {
            characteristics.Add(characteristic);
            characteristic.Init(this);
            OnAddCharacteristic?.Invoke(characteristic);
        }

        /// <summary>
        /// Inserts at a specified position a new <see cref="LBSCharacteristic"/> to this bundle.
        /// </summary>
        /// <param name="index">The position to insert the new characteristic.</param>
        /// <param name="characteristic">The characteristic to insert.</param>
        public void InsertCharacteristic(int index, LBSCharacteristic characteristic)
        {
            characteristic.Init(this);
            characteristics.Insert(index, characteristic);
            OnAddCharacteristic?.Invoke(characteristic);
        }

        /// <summary>
        /// Removes an <see cref="Asset"/> at a specified index.
        /// </summary>
        /// <param name="index">Index of the asset to remove.</param>
        public void RemoveAssetAt(int index)
        {
            Asset asset = assets[index];
            assets.RemoveAt(index);
            OnRemoveAsset?.Invoke(asset);
        }

        /// <summary>
        /// Remove a specified <see cref="LBSCharacteristic"/>.
        /// </summary>
        /// <param name="characteristic">The characteristic to remove.</param>
        public void RemoveCharacteristic(LBSCharacteristic characteristic)
        {
            if (characteristics.Remove(characteristic))
            {
                OnRemoveCharacteristic?.Invoke(characteristic);
            }
        }

        /// <summary>
        /// Manually invoked callback for specific cases of removing a <see cref="LBSCharacteristic"/> from this bundle.
        /// </summary>
        /// <param name="characteristic"></param>
        public void RemoveCharacteristicCallback(LBSCharacteristic characteristic)
        {
            OnRemoveCharacteristic?.Invoke(characteristic);
        }

        /// <summary>
        /// Searches for every <see cref="LBSCharacteristic"/> of a specified subtype in this bundle and its descendance.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="LBSCharacteristic"/>.</typeparam>
        /// <returns>A list of the subtype specified with all found characteristics.</returns>
        public List<T> GetChildrenCharacteristics<T>() where T : LBSCharacteristic
        {
            var chars = new List<T>();

            chars.AddRange(GetCharacteristics<T>());

            foreach (Bundle child in childsBundles)
            {
                if(child == null) continue;
                List<T> subChars = child.GetChildrenCharacteristics<T>();
                chars.AddRange(subChars);
            }
            return chars;
        }

        /// <summary>
        /// Gets all <see cref="LBSCharacteristic"/>s of the specified subtype in this bundle.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="LBSCharacteristic"/>.</typeparam>
        /// <returns>A list of the subtype specified with all found characteristics.</returns>
        public List<T> GetCharacteristics<T>() where T : LBSCharacteristic
        {
            var list = new List<T>();
            foreach (object item in characteristics)
            {
                if (item is T t)
                {
                    list.Add(t);
                }
            }

            return list;
        }

        public object Clone()
        {
            Bundle other = ScriptableObject.CreateInstance<Bundle>();

            foreach (LBSCharacteristic charc in this.characteristics)
            {
                other.AddCharacteristic(charc.Clone() as LBSCharacteristic);
            }

            foreach (Bundle child in this.childsBundles)
            {
                var b = child.Clone() as Bundle;
                other.AddChild(b);
            }

            foreach (Asset asset in assets)
            {
                other.AddAsset(asset.Clone() as Asset);
            }

            other.color = this.color;
            other.icon = this.icon;

            return other;
        }
        
        /// <summary>
        /// true if the bundle has the label characteristic
        /// </summary>
        public bool GetHasTagCharacteristic(string label)
        {
            return LBSAssetMacro.BundleHasTag(this, label);
        }

        /// <summary>
        /// true if the bundle has one of the labels
        /// </summary>
        public bool GetHasTagCharacteristic(List<string> labels)
        {
           foreach (var label in labels)
           {
               if (GetHasTagCharacteristic(label)) return true;

           }
            return false;
        }

        /// <summary>
        /// true if the Bundle has all the labels
        /// </summary>
        public bool GetHasAllTagCharacteristics(List<string> labels)
        {
            foreach (var label in labels)
            {
                if (!GetHasTagCharacteristic(label)) return false;

            }
            return true;
        }
        
        /// <summary>
        /// Returns true if the bundle has only the given flag (and no others).
        /// Example: if bundle is Enemy, query Enemy -> true.
        ///          if bundle is Enemy | Item, query Enemy -> false.
        /// </summary>
        public bool HasOnlyFlag(EElementFlag queryFlag)
        {
            return ElementFlag == queryFlag;
        }

        
        /// <summary>
        /// Returns true if the bundle has all of the given flags.
        /// Example: Enemy | Item returns true if queried with { Character, Item }.
        /// </summary>
        public bool HasAllFlags(params EElementFlag[] queryFlags)
        {
            foreach (EElementFlag flag in queryFlags)
            {
                if ((ElementFlag & flag) != flag)
                    return false;
            }
            return true;
        }


        /// <summary>
        /// Returns true if the bundle matches at least one of the given flags
        /// (including via parent-category bits).
        /// Example: Enemy -> true for (Character, Item)
        ///          Equipment -> true for (Item)
        /// </summary>
        public bool HasAnyFlag(params EElementFlag[] queryFlags)
        {
            foreach (EElementFlag flag in queryFlags)
            {
                if (HasFlag(flag))
                    return true;
            }

            return false;
        }



        /// <summary>
        /// Returns true if this bundle has the given flag
        /// or is a subtype that includes it (via bit composition).
        /// Example: Enemy.HasFlag(Character) -> true
        ///          Equipment.HasFlag(Item)   -> true
        /// </summary>
        public bool HasFlag(EElementFlag queryFlag)
        {
            if (queryFlag == EElementFlag.None)
                return ElementFlag == EElementFlag.None;

            return (ElementFlag & queryFlag) == queryFlag;
        }


        public bool HasCharacteristic(Type t)
        {
            return Characteristics.Any(ch => ch?.GetType() == t);
        }

        public MicroGenTool GetMicroGenTool()
        {
            return microGenTool;
        }

        public void ClearEvents()
        {
            OnAddChild = null;
            OnRemoveChild = null;
        }

        private void OnValidate()
        {
            if (entityType != EntityType.ET_NONE && !admissibleTypes.Contains(entityType))
                admissibleTypes.Insert(0, entityType);
        }
        #endregion

        #region STATIC FUNCTIONS

        public static bool IsRecursive(Bundle parent, Bundle child) // mover a extensions (!)
        {
            if (parent == child) return true;
            if (child.ChildsBundles.Contains(parent)) return true;
            
            foreach (Bundle ch in child.ChildsBundles)
            {
                if (IsRecursive(parent, ch))
                {
                    return true;
                }
            }

            return false;
        }


        #endregion


    }

    public static class BundleExtensions
    {
        public static bool IsRoot(this Bundle bundle)
        {
            LBSAssetsStorage storage = LBSAssetsStorage.Instance;

            var x = storage.Get<Bundle>().ToList();
            var xx = x.Where(b => b.ChildsBundles.Contains(bundle)).ToList();
            var b = xx.Count() <= 0;
            return b;
        }

        public static Bundle Parent(this Bundle bundle)
        {
            Bundle parent = LBSAssetsStorage.Instance.Get<Bundle>()
                .Find(b => b.ChildsBundles.Contains(bundle));

            return parent;
        }
        
        public static List<Bundle> Parents(this Bundle bundle)
        {
            List<Bundle> parents = LBSAssetsStorage.Instance.Get<Bundle>()
                .FindAll(b => b.ChildsBundles.Contains(bundle));

            return parents;
        }
    }
    
}
