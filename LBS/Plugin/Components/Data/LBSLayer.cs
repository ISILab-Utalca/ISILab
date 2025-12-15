using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tesellation.Tilemap;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.Core.Settings;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace LBS.Components    
{
    [Serializable]
    public class LBSLayer : ICloneable
    {
        #region meta-fields
        [SerializeField, JsonRequired, HideInInspector] private bool visible = true;
        [SerializeField, JsonRequired, HideInInspector] private bool blocked;
        [SerializeField, JsonRequired] public string iconGuid = "915dd173939598c43ab48bbec50425e8";

        [SerializeField, JsonRequired] private string id = "Default ID";
        [SerializeField, JsonRequired] private string name = "Layer name";

        [JsonIgnore] private LBSLevelData _parent;

        [SerializeField, SerializeReference] private List<LBSModule> modules = new();
        [SerializeField, SerializeReference] private List<LBSBehaviour> behaviours = new();
        [SerializeField, SerializeReference] private List<LBSAssistant> assistants = new();
        [SerializeField, SerializeReference] private List<LBSGeneratorRule> generatorRules = new();

        [SerializeField] private LBSGenerator3DSettings settings = new();

        [SerializeField, JsonRequired] public int index;

        #endregion
        
        #region Properties
        [JsonIgnore] public bool IsVisible { get => visible; set => visible = value; }
        [JsonIgnore] public bool IsBlocked { get => blocked; set => blocked = value; }
        [JsonIgnore] public bool IsLocked { get => blocked; set => blocked = value; }

        [JsonIgnore] public LBSLevelData Parent { get => _parent; set => _parent = value; }
        [JsonIgnore] public string ID { get => id; set => id = value; }
        [JsonIgnore] public string Name { get => name; set => name = value; }

        // Return copies to protect internal lists
        [JsonIgnore] public List<LBSModule> Modules => new(modules);
        [JsonIgnore] public List<LBSBehaviour> Behaviours => new(behaviours);
        [JsonIgnore] public List<LBSAssistant> Assistants => new(assistants);
        [JsonIgnore] public List<LBSGeneratorRule> GeneratorRules => new(generatorRules);

        [JsonIgnore] public LBSGenerator3DSettings Settings { get => settings; set => settings = value; }

        [JsonIgnore]
        public Vector2Int TileSize
        {
            get => new Vector2Int((int)settings.scale.x, (int)settings.scale.y);
            set
            {
                settings.scale.x = value.x;
                settings.scale.y = value.y;
                OnTileSizeChange?.Invoke(value);
            }
        }
        #endregion

        #region Events
        public event Action OnChangeName;
        public event Action OnChange;
        public event Action<Vector2Int> OnTileSizeChange;
        public event Action<LBSLayer, LBSModule> OnAddModule;
        public event Action<LBSLayer, LBSModule> OnReplaceModule;
        public event Action<LBSLayer, LBSModule> OnRemoveModule;
        public event Action OnContextAdd;
        public event Action OnContextRemove;
        #endregion
        
        #region Constructors
        public LBSLayer()
        {
            modules ??= new List<LBSModule>();
            behaviours ??= new List<LBSBehaviour>();
            assistants ??= new List<LBSAssistant>();
            generatorRules ??= new List<LBSGeneratorRule>();

            IsVisible = true;
            ID = GetType().Name;
        }

        public LBSLayer(
            IEnumerable<LBSModule> modules,
            IEnumerable<LBSAssistant> assistant,
            IEnumerable<LBSGeneratorRule> rules,
            IEnumerable<LBSBehaviour> behaviours,
            string ID, bool visible, string name, string iconGuid, Vector2Int tileSize) : this()
        {
            if (modules != null) foreach (LBSModule m in modules) AddModule(m);
            if (assistant != null) foreach (LBSAssistant a in assistant) AddAssistant(a);
            if (rules != null) foreach (LBSGeneratorRule r in rules) AddGeneratorRule(r);
            if (behaviours != null) foreach (LBSBehaviour b in behaviours) AddBehaviour(b);

            this.ID = ID;
            IsVisible = visible;
            this.name = name;
            this.iconGuid = iconGuid;
            TileSize = tileSize;

            InitializeContextEvents();
        }
        #endregion

        #region modules
        public bool AddModule(LBSModule module)
        {
            if (module == null) return false;
            if (modules.Contains(module)) return false;

            modules.Add(module);
            module.OnAttach(this);
            OnAddModule?.Invoke(this, module);
            return true;
        }

        public bool RemoveModule(LBSModule module)
        {
            if (module == null) return false;

            var removed = modules.Remove(module);
            if (removed)
            {
                try { module.OnDetach(this); } catch { /* swallow detach errors */ }
                OnRemoveModule?.Invoke(this, module);
            }
            return removed;
        }

        public LBSModule GetModule(int indexPos) => modules[indexPos];

        public LBSModule GetModule(string moduleID)
            => modules.FirstOrDefault(m => string.Equals(m?.ID, moduleID, StringComparison.Ordinal));

        public T GetModule<T>(string moduleID = "") where T : LBSModule
        {
            if (string.IsNullOrEmpty(moduleID))
                return modules.OfType<T>().FirstOrDefault();
            return modules.FirstOrDefault(m => (m is T || Reflection.IsSubclassOfRawGeneric(typeof(T), m.GetType())) && m.ID == moduleID) as T;
        }

        internal void SetModule<T>(T module, string key = "") where T : LBSModule
        {
            if (module == null) return;

            var idx = string.IsNullOrEmpty(key) ? modules.FindIndex(m => m is T) : modules.FindIndex(m => m is T && m.ID == key);

            if (idx < 0 || idx >= modules.Count) throw new IndexOutOfRangeException("Module to replace not found.");

            // detach old then attach new
            modules[idx].OnDetach(this);
            modules[idx] = module;
            modules[idx].OnAttach(this);
            modules[idx].OwnerLayer = this;
            OnReplaceModule?.Invoke(this, module);
        }

        public void ReplaceModule(LBSModule oldModule, LBSModule newModule)
        {
            if (oldModule == null || newModule == null) return;
            var idx = modules.IndexOf(oldModule);
            if (idx < 0) return;

            RemoveModule(oldModule);
            modules.Insert(idx, newModule);
            OnReplaceModule?.Invoke(this, newModule);
        }
        #endregion
        
        #region behaviors
        public void AddBehaviour(LBSBehaviour behaviour)
        {
            if (behaviour == null) return;
            if (behaviours.Contains(behaviour))
            {
                Debug.LogWarning($"[ISI Lab]: This layer already contains the behaviour {behaviour.GetType().Name}.");
                return;
            }

            behaviours.Add(behaviour);

            // ensure required modules exist
            var req = behaviour.GetRequiredModules();
            if (req != null)
            {
                foreach (Type rt in req)
                {
                    if (modules.All(m => m.GetType() != rt))
                        AddModule(Activator.CreateInstance(rt) as LBSModule);
                }
            }

            behaviour.OnAttachLayer(this);
        }

        public void RemoveBehaviour(LBSBehaviour behaviour)
        {
            if (behaviour == null) return;
            if (behaviours.Remove(behaviour))
                behaviour.OnDetachLayer(this);
        }

        public T GetBehaviour<T>(string idParam = "") where T : LBSBehaviour
        {
            if (string.IsNullOrEmpty(idParam))
                return behaviours.OfType<T>().FirstOrDefault();

            return behaviours.FirstOrDefault(b => (b is T || Reflection.IsSubclassOfRawGeneric(typeof(T), b.GetType())) && b.Name == idParam) as T;
        }
        #endregion

      #region Assistants
        public void AddAssistant(LBSAssistant assistant)
        {
            if (assistant == null) return;
            if (assistants.Any(a => a.GetType() == assistant.GetType()))
            {
                Debug.LogWarning($"[ISI Lab]: This layer already contains the assistant {assistant.GetType().Name}.");
                return;
            }

            assistants.Add(assistant);

            var req = assistant.GetRequiredModules();
            if (req != null)
            {
                foreach (Type rt in req)
                {
                    if (modules.All(m => m.GetType() != rt))
                        AddModule(Activator.CreateInstance(rt) as LBSModule);
                }
            }

            assistant.OnAttachLayer(this);
        }

        public void RemoveAssistant(LBSAssistant assistant)
        {
            if (assistant == null) return;
            if (assistants.Remove(assistant))
                assistant.OnDetachLayer(this);
        }

        public LBSAssistant GetAssistant(int indexPos) => assistants[indexPos];

        public T GetAssistant<T>(string idParam = "") where T : LBSAssistant
        {
            if (string.IsNullOrEmpty(idParam)) return assistants.OfType<T>().FirstOrDefault();
            return assistants.FirstOrDefault(a => (a is T || Reflection.IsSubclassOfRawGeneric(typeof(T), a.GetType())) && a.Name == idParam) as T;
        }
        #endregion

        #region Generator rules
        public void AddGeneratorRule(LBSGeneratorRule rule)
        {
            if (rule == null) return;
            generatorRules.Add(rule);
        }

        public bool RemoveGeneratorRule(LBSGeneratorRule rule) => generatorRules.Remove(rule);
        #endregion
              
        #region Utility
        public void Reload()
        {
            foreach (LBSModule module in modules) module.OnAttach(this);
            foreach (LBSAssistant assistant in assistants) assistant.OnAttachLayer(this);
            // generator rules intentionally not auto-attached here
            foreach (LBSBehaviour behaviour in behaviours) behaviour.OnAttachLayer(this);

            InitializeContextEvents();
        }

        public void RemoveAll()
        {
            // iterate safely from end to start
            for (int i = modules.Count - 1; i >= 0; i--) RemoveModule(modules[i]);
            for (int i = behaviours.Count - 1; i >= 0; i--) RemoveBehaviour(behaviours[i]);
            for (int i = assistants.Count - 1; i >= 0; i--) RemoveAssistant(assistants[i]);
            for (int i = generatorRules.Count - 1; i >= 0; i--) RemoveGeneratorRule(generatorRules[i]);
        }

        public Vector2Int ToFixedPosition(Vector2 position)
        {
            Vector2 pos = position / (TileSize * LBSSettings.Instance.general.TileSize);

            if (pos.x < 0) pos.x -= 1;
            if (pos.y < 0) pos.y -= 1;

            pos = new Vector2(pos.x, -pos.y);
            return pos.ToInt();
        }

        public Vector2Int ToFixedPositionOffset(Vector2 position, Vector2 offset) => ToFixedPosition(position + offset);
        public Vector2Int ToFixedPositionOffset(Vector2 position, float offset) => ToFixedPosition(position + Vector2.one * offset);

        public Vector2 FixedToPosition(Vector2Int position, bool invertY = false)
        {
            var tileSizeX = TileSize.x * LBSSettings.Instance.general.TileSize.x;
            var tileSizeY = TileSize.y * LBSSettings.Instance.general.TileSize.y;
            if (invertY) tileSizeY = -tileSizeY;
            return new Vector2(position.x * tileSizeX, position.y * tileSizeY);
        }

        public (Vector2Int min, Vector2Int max) ToFixedPosition(Vector2 startPos, Vector2 endPos)
        {
            Vector2Int sPos = ToFixedPosition(startPos);
            Vector2Int ePos = ToFixedPosition(endPos);

            Vector2Int min = new Vector2Int(Mathf.Min(sPos.x, ePos.x), Mathf.Min(sPos.y, ePos.y));
            Vector2Int max = new Vector2Int(Mathf.Max(sPos.x, ePos.x), Mathf.Max(sPos.y, ePos.y));
            return (min, max);
        }

        public void ClearEvents()
        {
            OnChangeName = null;
            OnChange = null;
            OnTileSizeChange = null;
            OnAddModule = null;
            OnReplaceModule = null;
            OnRemoveModule = null;
            // keep context events if they're needed elsewhere
        }
        
        public object Clone()
        {
            // Clone modules via provided helper, clone lists of polymorphic objects by calling Clone() on each
            List<LBSModule> clonedModules = this.modules.Clone(); // assuming Clone extension returns List<LBSModule>
            List<LBSAssistant> clonedAssistants = this.assistants.Select(a => a.Clone() as LBSAssistant).ToList();
            List<LBSGeneratorRule> clonedRules = this.generatorRules.Select(r => r.Clone() as LBSGeneratorRule).ToList();
            List<LBSBehaviour> clonedBehaviours = this.behaviours.Select(b => b.Clone() as LBSBehaviour).ToList();

            return new LBSLayer(clonedModules, clonedAssistants, clonedRules, clonedBehaviours, id, visible, name, iconGuid, TileSize);
        }

        public override bool Equals(object obj)
        {
            if (obj is not LBSLayer other) return false;
            if (other.id != id || other.name != name) return false;
            if (!modules.SequenceEqual(other.modules)) return false;
            if (!behaviours.SequenceEqual(other.behaviours)) return false;
            if (!assistants.SequenceEqual(other.assistants)) return false;
            if (!generatorRules.SequenceEqual(other.generatorRules)) return false;
            if (!settings.Equals(other.settings)) return false;
            return true;
        }

        public override int GetHashCode() => id.GetHashCode();

        #endregion
        
        #region Events
        public void OnChangeUpdate() => OnChange?.Invoke();
        public void InvokeNameChanged() => OnChangeName?.Invoke();
        public void OnContextAddInvoke() => OnContextAdd?.Invoke();
        public void OnContextRemoveInvoke() => OnContextRemove?.Invoke();

        #endregion
        
        #region Inits
        private void InitializeContextEvents()
        {
            // Example: exterior-specific wiring
            if (ID != "Exterior") return;

            ConnectedTileMapModule connectedTM = GetModule<ConnectedTileMapModule>();
            if (connectedTM == null) return;

            switch (connectedTM.GridType)
            {
                case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                    OnContextAdd = VertexExteriorAdd;
                    OnContextRemove = VertexExteriorRemove;
                    break;
                default:
                    OnContextAdd = null;
                    OnContextRemove = null;
                    break;
            }

            // local functions
            void VertexExteriorAdd()
            {
                SectorizedTileMapModule sectorTM = new SectorizedTileMapModule();
                Assert.IsTrue(AddModule(sectorTM));

                // clone connectedTM as a temporary connected module
                if (new List<LBSModule> { connectedTM }.Clone()[0] is ConnectedTileMapModule zoneConnected)
                {
                    zoneConnected.ID = "TempConnectedModule";

                    var floorTags = GetBehaviour<ExteriorBehaviour>()?.NavigableTags;
                    sectorTM.BuildFromExterior(connectedTM, zoneConnected, floorTags);
                    Assert.IsTrue(AddModule(zoneConnected));
                }
            }

            void VertexExteriorRemove()
            {
                Assert.IsTrue(RemoveModule(GetModule<SectorizedTileMapModule>()));
                Assert.IsTrue(RemoveModule(GetModule<ConnectedTileMapModule>("TempConnectedModule")));
            }
        }
        
                    
        #endregion
    }
}


/* OLD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Assistants;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Generators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace LBS.Components
{
    [Serializable]
    public class LBSLayer : ICloneable
    {
        #region META-FIELDS
        [SerializeField, JsonRequired, HideInInspector]
        private bool visible = true;

        [SerializeField, JsonRequired, HideInInspector]
        private bool blocked;

        [SerializeField, JsonRequired]
        public string iconGuid = "Icon/Default";
        #endregion

        #region FIELDS
        [SerializeField, JsonRequired]
        private string id = "Default ID";

        [SerializeField, JsonRequired]
        private string name = "Layer name";

        [JsonIgnore]
        private LBSLevelData _parent;

        [SerializeField, SerializeReference]
        private List<LBSModule> modules = new();

        [SerializeField, SerializeReference]
        private List<LBSBehaviour> behaviours = new();

        [SerializeField, SerializeReference]
        private List<LBSAssistant> assistants = new();

        [SerializeReference]
        private List<LBSGeneratorRule> generatorRules = new();

        [SerializeField]
        private Generator3D.Settings settings = new();
       
        [SerializeField, JsonRequired]
        public int index;

        #endregion

        #region META-PROPERTIES
        [JsonIgnore]
        public bool IsVisible
        {
            get => visible;
            set => visible = value;
        }

        [JsonIgnore]
        public bool IsBlocked
        {
            get => blocked;
            set => blocked = value;
        }
        #endregion

        #region PROPERTIES
        [JsonIgnore]
        public bool IsLocked
        {
            get => blocked;
            set => blocked = value;
        }

        [JsonIgnore]
        public LBSLevelData Parent
        {
            get => _parent;
            set => _parent = value;
        }
        
        [JsonIgnore]
        public string ID
        {
            get => id;
            set => id = value;
        }

        [JsonIgnore]
        public string Name
        {
            get => name;
            set => name = value;
        }

        [JsonIgnore]
        public List<LBSModule> Modules => new(modules);

        [JsonIgnore]
        public List<LBSBehaviour> Behaviours => new(behaviours);

        [JsonIgnore]
        public List<LBSAssistant> Assistants => new(assistants);

        [JsonIgnore]
        public List<LBSGeneratorRule> GeneratorRules => new(generatorRules);


        [JsonIgnore]
        public Generator3D.Settings Settings
        {
            get => settings;
            set => settings = value;
        }

        [JsonIgnore]
        public Vector2Int TileSize
        {
            get =>
                new((int)settings.scale.x,
                    (int)settings.scale.y);
            set
            {
                settings.scale.x = value.x;
                settings.scale.y = value.y;
                OnTileSizeChange?.Invoke(value);
            }
        }
        #endregion

        #region EVENTS

        public event Action OnChangeName;
        public event Action OnChange; // call whenever needing to update a change on a single layer
        public event Action<Vector2Int> OnTileSizeChange;
        public event Action<LBSLayer, LBSModule> OnAddModule;
        public event Action<LBSLayer, LBSModule> OnReplaceModule;
        public event Action<LBSLayer, LBSModule> OnRemoveModule;

        public event Action OnContextAdd;
        public event Action OnContextRemove;
        
        #endregion

        #region  CONSTRUCTORS
        public LBSLayer()
        {
            modules = new List<LBSModule>();
            
            IsVisible = true;
            ID = GetType().Name;
        }

        public LBSLayer(
            IEnumerable<LBSModule> modules,
            IEnumerable<LBSAssistant> assistant,
            IEnumerable<LBSGeneratorRule> rules,
            IEnumerable<LBSBehaviour> behaviours,
            string ID, bool visible, string name, string iconGuid, Vector2Int tileSize)
        {
            foreach (LBSModule m in modules)
            {
                AddModule(m);
            }

            foreach(LBSAssistant a in assistant)
            {
                AddAssistant(a);
            }

            foreach(LBSGeneratorRule r in rules)
            {
                AddGeneratorRule(r);
            }

            foreach(LBSBehaviour b in behaviours)
            {
                AddBehaviour(b);
            }

            this.ID = ID;
            IsVisible = visible;
            this.name = name;
            this.iconGuid = iconGuid;
            TileSize = tileSize;

            InitializeContextEvents();
        }
        #endregion

        #region  METHODS
        public void ReplaceModule(LBSModule oldModule, LBSModule newModule)
        {
            int indexOf = modules.IndexOf(oldModule);
            RemoveModule(oldModule);
            modules.Insert(indexOf, newModule);
            OnReplaceModule?.Invoke(this, newModule);
        }

        public void Reload()
        {
            foreach (LBSModule module in modules)
            {
                module.OnAttach(this);
            }

            foreach(LBSAssistant assistant in assistants)
            {
                assistant.OnAttachLayer(this);
            }

            foreach (LBSGeneratorRule rule in generatorRules)
            {
                // rule.OnAttachLayer(this);
            }

            foreach (LBSBehaviour behaviour in behaviours)
            {
                behaviour.OnAttachLayer(this);
            }

            InitializeContextEvents();
        }

        public void AddBehaviour(LBSBehaviour behaviour)
        {
            if (behaviours.Contains(behaviour))
            {
                Debug.Log("[ISI Lab]: This layer already contains the behavior " + behaviour.GetType().Name + ".");
                return;
            }

            behaviours.Add(behaviour);

            // check if the layer have necessary 'Modules'
            List<Type> reqModules = behaviour.GetRequiredModules();
            foreach (Type type in reqModules)
            {
                if (modules.All(e => e.GetType() != type))         
                {
                    AddModule(Activator.CreateInstance(type) as LBSModule);
                }
            }

            behaviour.OnAttachLayer(this);
        }

        public void RemoveBehaviour(LBSBehaviour behaviour)
        {
            behaviours.Remove(behaviour);
            behaviour.OnDetachLayer(this);
        }

        public void AddGeneratorRule(LBSGeneratorRule rule)
        {
            generatorRules.Add(rule);
        }

        public bool RemoveGeneratorRule(LBSGeneratorRule rule)
        {
            return generatorRules.Remove(rule);
        }

        public void AddAssistant(LBSAssistant assistant)
        {
            if (assistants.Find( a => assistant.GetType() == a.GetType()) != null)
            {
                Debug.Log("[ISI Lab]: This layer already contains the assistant " + assistant.GetType().Name + ".");
                return;
            }

            assistants.Add(assistant);

            List<Type> reqModules = assistant.GetRequiredModules();
            foreach (Type type in reqModules)
            {
                if (modules.All(e => e.GetType() != type))
                {
                    AddModule(Activator.CreateInstance(type) as LBSModule);
                }
            }
            assistant.OnAttachLayer(this);

        }

        public void RemoveAssistant(LBSAssistant assistant)
        {
            assistants.Remove(assistant);
            assistant.OnDetachLayer(this);
        }

        public LBSAssistant GetAssistant(int indexPos)
        {
            return assistants[indexPos];
        }

        public bool AddModule(LBSModule module)
        {
            if(modules.Contains(module))
            {
                return false;
            }

            modules.Add(module);
            module.OnAttach(this);

            OnAddModule?.Invoke(this, module);

            return true;
        }

        public bool RemoveModule(LBSModule module)
        {
            if (module is null) return false;

            bool removed = modules.Remove(module);
            module.OnDetach(this);
            OnRemoveModule?.Invoke(this,module);
            return removed;
        }

        public LBSModule GetModule(int indexPos)
        {
            return modules[indexPos];
        }

        public LBSModule GetModule(string ModuleID)
        {
            foreach (LBSModule module in modules)
            {
                if(module.ID == ModuleID) return module;
                    
            }
            return null;
        }

        public T GetModule<T>(string ModuleID = "") where T : LBSModule
        {
            Type t = typeof(T);
            foreach (LBSModule module in modules)
            {
                if (module is T || Reflection.IsSubclassOfRawGeneric(t,module.GetType()))
                {
                    if(ModuleID.Equals("") || module.ID.Equals(ModuleID))
                    {
                        return module as T;
                    }
                }
            }
            return null;
        }

        public T GetAssistant<T>(string ID = "") where T : LBSAssistant
        {
            Type t = typeof(T);
            foreach (LBSAssistant a in assistants)
            {
                if (a is T || Reflection.IsSubclassOfRawGeneric(t, a.GetType()))
                {
                    if (ID.Equals("") || a.Name.Equals(ID))
                    {
                        return a as T;
                    }
                }
            }
            return null;
        }

        public object GetModule(Type type ,string ID = "")
        {
            foreach (LBSModule module in modules)
            {
                if (module.GetType().Equals(type) || Reflection.IsSubclassOfRawGeneric(type, module.GetType()))
                {
                    if (ID.Equals("") || module.ID.Equals(ID))
                    {
                        return module;
                    }
                }
            }
            return null;

        }
        public T GetBehaviour<T>(string ID = "") where T : LBSBehaviour
        {
            Type t = typeof(T);
            foreach (LBSBehaviour b in behaviours)
            {
                if (b is T || Reflection.IsSubclassOfRawGeneric(t, b.GetType()))
                {
                    if (ID.Equals("") || b.Name.Equals(ID))
                    {
                        return b as T;
                    }
                }
            }
            return null;
        }

        internal void SetModule<T>(T module, string key = "") where T : LBSModule
        {
            int index = -1;
            if (key.Equals(""))
            {
                index = modules.FindIndex(m => m is T);
                modules[index] = module;
                return;
            }

            index = modules.FindIndex(m => m is T && m.ID.Equals(key));

            modules[index].OnDetach(this);
            modules[index] = module;
            modules[index].OnAttach(this);
            modules[index].OwnerLayer = this;
        }

        public void RemoveAll()
        {
            for (int i = 0; i < modules.Count; i++)
                RemoveModule(modules[i--]);
            for (int i = 0; i < behaviours.Count; i++)
                RemoveBehaviour(behaviours[i--]);
            for (int i = 0; i < assistants.Count; i++)
                RemoveAssistant(assistants[i--]);
            for (int i = 0; i < generatorRules.Count; i++)
                RemoveGeneratorRule(generatorRules[i--]);
        }

        // esto tiene que ir en una extension (?)
        public Vector2Int ToFixedPosition(Vector2 position) 
        {
            Vector2 pos = position / (TileSize * LBSSettings.Instance.general.TileSize);

            if (pos.x < 0)
                pos.x -= 1;

            if (pos.y < 0)
                pos.y -= 1;

            pos = new Vector2(pos.x, -pos.y);

            return pos.ToInt();
        }

        public Vector2Int ToFixedPositionOffset(Vector2 position, Vector2 offset) => ToFixedPosition(position + offset);
        public Vector2Int ToFixedPositionOffset(Vector2 position, float offset) => ToFixedPosition(position + Vector2.one * offset);

        public Vector2 FixedToPosition(Vector2Int position, bool invertY = false) 
        {
            float tileSizeX = TileSize.x * LBSSettings.Instance.general.TileSize.x;
            float tileSizeY = TileSize.y * LBSSettings.Instance.general.TileSize.y;
            if(invertY) tileSizeY = -tileSizeY;
            return new Vector2(position.x * tileSizeX, position.y * tileSizeY);
        }
        
        public (Vector2Int,Vector2Int) ToFixedPosition(Vector2 startPos, Vector2 endPos)
        {
            Vector2Int sPos = ToFixedPosition(startPos);
            Vector2Int ePos = ToFixedPosition(endPos);

            Vector2Int min = new Vector2Int(
                (sPos.x < ePos.x) ? sPos.x : ePos.x,
                (sPos.y < ePos.y) ? sPos.y : ePos.y);

            Vector2Int max = new Vector2Int(
                (sPos.x > ePos.x) ? sPos.x : ePos.x,
                (sPos.y > ePos.y) ? sPos.y : ePos.y);

            return (min, max);
        }

        public void ClearEvents() // Volver a revisar si es adecuado llamarlo al quitar un modulo. Esto se creo para el Automap de Simulation.
        {
            OnChangeName = null;
            OnChange = null;
            OnTileSizeChange = null;
            OnAddModule = null;
            OnReplaceModule = null;
            OnRemoveModule = null;

            //OnContextAdd = null;
            //OnContextRemove = null;
        }
        

        public object Clone()
        {
            List<LBSModule> modules = this.modules.Clone(); // CloneRef
            IEnumerable<LBSAssistant> assistants = this.assistants.Select(a => a.Clone() as LBSAssistant);
            IEnumerable<LBSGeneratorRule> rules = generatorRules.Select(r => r.Clone() as LBSGeneratorRule);
            IEnumerable<LBSBehaviour> behaviours = this.behaviours.Select(b => b.Clone() as LBSBehaviour);

            LBSLayer layer = new LBSLayer(modules, assistants, rules, behaviours, id, visible, name, iconGuid, TileSize);
            return layer;
        }

        public override bool Equals(object obj)
        {
            LBSLayer other = obj as LBSLayer;

            // check if the same type
            if (other == null) return false;

            // check if have the same ID
            if(other.id != id) return false;

            // check if have the same name
            if (other.name != name) return false;

            // get amount of modules
            int mCount = other.modules.Count;

            // cheack amount of modules
            if (modules.Count != mCount) return false;

            for (int i = 0; i < mCount; i++)
            {
                LBSModule m1 = other.modules[i];
                LBSModule m2 = modules[i];
                if (!m1.Equals(m2)) return false;
            }

            // get amount of modules
            int bCount = other.behaviours.Count;
            // cheack amount of behaviours
            if (behaviours.Count != bCount) return false;

            for (int i = 0; i < bCount; i++)
            {
                LBSBehaviour b1 = other.behaviours[i];
                LBSBehaviour b2 = behaviours[i];

                if (!b1.Equals(b2)) return false;
            }

            // get amount of modules
            int aCount = other.assistants.Count;

            // cheack amount of behaviours
            if (assistants.Count != aCount) return false;

            for (int i = 0; i < aCount; i++)
            {
                LBSAssistant a1 = other.assistants[i];
                LBSAssistant a2 = assistants[i];

                if (!a1.Equals(a2)) return false;
            }

            // get amount of modules
            int gCount = other.generatorRules.Count;

            // cheack amount of behaviours
            if (generatorRules.Count != gCount) return false;

            for (int i = 0; i < gCount; i++)
            {
                LBSGeneratorRule g1 = other.generatorRules[i];
                LBSGeneratorRule g2 = generatorRules[i];

                if (!g1.Equals(g2)) return false;
            }
            // check if have EQUALS settings
            if (!settings.Equals(other.settings)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void CleanGraphViews()
        {
            foreach (LBSBehaviour behaviour in behaviours)
            {
                
            }
        }


        /*
        public bool InsertModule(int index, LBSModule module)
        {
            if (modules.Contains(module))
            {
                return false;
            }
            if (!(modules.ContainsIndex(index) || index == modules.Count))
            {
                return false;
            }
            modules.Insert(index, module);
            module.Owner = this;
            module.OnChanged += (mo) => { this.onModuleChange(this); };
            return true;
        }
        */

        /*
        public LBSModule RemoveModuleAt(int index)
        {
            var module = modules[index];
            RemoveModule(module);
            return module;
        }
        
        public void OnChangeUpdate()
        {
            OnChange?.Invoke();
        }
        #endregion

        public void InvokeNameChanged()
        {
            OnChangeName?.Invoke();
        }

        public void OnContextAddInvoke()
        {
            //Debug.Log($"Layer {Name} OnContextAddInvoke.");
            OnContextAdd?.Invoke();
        }
        public void OnContextRemoveInvoke()
        {
            //Debug.Log($"Layer {Name} OnContextRemoveInvoke.");
            OnContextRemove?.Invoke();
        }

        private void InitializeContextEvents()
        {
            ConnectedTileMapModule connectedTM;

            switch (ID)
            {
                case "Interior":
                    break;

                case "Exterior":
                    connectedTM = GetModule<ConnectedTileMapModule>();
                    if(connectedTM == null) return;
                    ConnectedTileMapModule.ConnectedTileType grid = connectedTM.GridType;
                    switch (connectedTM.GridType)
                    {
                        case ConnectedTileMapModule.ConnectedTileType.EdgeBased:
                            break;
                        case ConnectedTileMapModule.ConnectedTileType.VertexBased:
                            OnContextAdd = VertexExteriorAdd;
                            OnContextRemove = VertexExteriorRemove;
                            break;
                    }
                    break;

                case "Population":
                    break;

                case "Quest":
                    break;

                case "Simulation":
                    break;

            }

            return; /// END OF METHOD

            /// Local functions

            void VertexExteriorAdd()
            {
                //Debug.Log("Vertex Exterior Context Add");
                SectorizedTileMapModule sectorTM = new();
                Assert.IsTrue(AddModule(sectorTM));
                ConnectedTileMapModule zoneConnected = new List<LBSModule>() { connectedTM }.Clone()[0] as ConnectedTileMapModule;
                zoneConnected.ID = "TempConnectedModule";
                List<string> floorTags = GetBehaviour<ExteriorBehaviour>()?.NavigableTags;
                sectorTM.BuildFromExterior(connectedTM, zoneConnected, floorTags);
                Assert.IsTrue(AddModule(zoneConnected));
            }

            void VertexExteriorRemove()
            {
                //Debug.Log("Vertex Exterior Context Remove");
                Assert.IsTrue(RemoveModule(GetModule<SectorizedTileMapModule>()));
                Assert.IsTrue(RemoveModule(GetModule<ConnectedTileMapModule>("TempConnectedModule")));
            }
        }
    }
}

*/

