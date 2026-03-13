using ISILab.Commons.Utility;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using Newtonsoft.Json;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.Image;
using Type = System.Type;

namespace LBS.Components    
{
    [Serializable]
    public class LBSLayer : ICloneable, IBlueprintable
    {
        #region Meta Fields
        [SerializeField, JsonRequired, HideInInspector] private bool visible = true;
        [SerializeField, JsonRequired, HideInInspector] private bool blocked;
        [SerializeField, JsonRequired] public string iconGuid = "915dd173939598c43ab48bbec50425e8";

        [SerializeField, JsonRequired] private string id = "Default ID";
        [SerializeField, JsonRequired] private string name = "Layer name";

        [JsonIgnore] private LBSLevelData _parent;

        [SerializeField, SerializeReference] private int activeFloor = 0;
        [SerializeField, SerializeReference] private LBSFloor[] floors = new LBSFloor[10];
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
        [JsonIgnore] public int ActiveFloor { get => activeFloor; }

        // Return copies to protect internal lists
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
            for(int i = 0; i < floors.Length; i++)
            {
                floors[i] ??= new ();
            }
            behaviours ??= new List<LBSBehaviour>();
            assistants ??= new List<LBSAssistant>();
            generatorRules ??= new List<LBSGeneratorRule>();

            IsVisible = true;
            ID = GetType().Name;
        }

        public LBSLayer(
            LBSFloor[] modules,
            IEnumerable<LBSAssistant> assistant,
            IEnumerable<LBSGeneratorRule> rules,
            IEnumerable<LBSBehaviour> behaviours,
            LBSLevelData parent,
            string ID, bool visible, string name, string iconGuid, Vector2Int tileSize) : this()
        {
            for(int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null) foreach (LBSModule m in modules[i].Modules) AddModule(m, i);
            }
            if (assistant != null) foreach (LBSAssistant a in assistant) AddAssistant(a);
            if (rules != null) foreach (LBSGeneratorRule r in rules) AddGeneratorRule(r);
            if (behaviours != null) foreach (LBSBehaviour b in behaviours) AddBehaviour(b);

            Parent = parent;
            this.ID = ID;
            IsVisible = visible;
            this.name = name;
            this.iconGuid = iconGuid;
            TileSize = tileSize;

            InitializeContextEvents();
        }
        #endregion

        #region Floors
        public void ChangeFloor(int newFloor)
        {
            if (newFloor < 0 || newFloor >= 10) return;

            var prevFloor = activeFloor;
            activeFloor = newFloor;
            foreach (var behaviour in Behaviours)
            {
                if(behaviour is SchemaBehaviour schema)
                {
                    schema.ChangeLevelRender(prevFloor, newFloor);
                    schema.LevelChangedAction?.Invoke();
                }
            }//*/
            Reload();
        }
        #endregion

        #region modules
        public List<LBSModule> Modules(int floorIndex = -1)
        {
            if (floorIndex < 0) floorIndex = activeFloor;
            return new(floors[floorIndex].Modules);
        }

        public bool AddModule(LBSModule module, int levelIndex = -1)
        {
            if (module == null) return false;
            if (levelIndex < 0) levelIndex = activeFloor;
            if (floors[levelIndex].Modules.Contains(module)) return false;

            floors[levelIndex].Modules.Add(module);
            module.OnAttach(this);
            OnAddModule?.Invoke(this, module);
            return true;
        }

        public bool RemoveModule(LBSModule module)
        {
            if (module == null) return false;

            bool removed = false;
            for (int i = 0; i < floors.Length; i++)
            {
                removed = floors[i].Modules.Remove(module);
                if (removed)
                {
                    try { module.OnDetach(this); } catch { /* swallow detach errors */ }
                    OnRemoveModule?.Invoke(this, module);
                }
                else break;
            }
            return removed;
        }

        public LBSModule GetModule(int levelIndex, int posIndex) => floors[levelIndex].Modules[posIndex];

        public LBSModule GetModule(string moduleID)
            => floors[activeFloor].Modules.FirstOrDefault(m => string.Equals(m?.ID, moduleID, StringComparison.Ordinal));

        public T GetModule<T>(string moduleID = "", int index = -1) where T : LBSModule
        {
            if (index < 0) index = activeFloor;
            if (string.IsNullOrEmpty(moduleID))
                return floors[index].Modules.OfType<T>().FirstOrDefault();

            return floors[index].Modules.FirstOrDefault(
                m => (m is T || Reflection.IsSubclassOfRawGeneric(typeof(T), m.GetType())) && m.ID == moduleID) as T;
        }

        public T GetRule<T>() where T : LBSGeneratorRule
        {
            return generatorRules.OfType<T>().FirstOrDefault();
        }

        internal void SetModule<T>(T module, string key = "") where T : LBSModule
        {
            if (module == null) return;

            var idx = string.IsNullOrEmpty(key) ? 
                floors[activeFloor].Modules.FindIndex(m => m is T) : floors[activeFloor].Modules.FindIndex(m => m is T && m.ID == key);

            if (idx < 0 || idx >= floors[activeFloor].Modules.Count) throw new IndexOutOfRangeException("Module to replace not found.");

            // detach old then attach new
            floors[activeFloor].Modules[idx].OnDetach(this);
            floors[activeFloor].Modules[idx] = module;
            floors[activeFloor].Modules[idx].OnAttach(this);
            floors[activeFloor].Modules[idx].OwnerLayer = this;
            OnReplaceModule?.Invoke(this, module);
        }

        public void ReplaceModule(LBSModule oldModule, LBSModule newModule)
        {
            if (oldModule == null || newModule == null) return;
            var idx = floors[activeFloor].Modules.IndexOf(oldModule);
            if (idx < 0) return;

            RemoveModule(oldModule);
            floors[activeFloor].Modules.Insert(idx, newModule);
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
                    for (int i = 0; i < floors.Length; i++)
                    {
                        if (floors[i].Modules.All(m => m.GetType() != rt))
                            AddModule(Activator.CreateInstance(rt) as LBSModule, i);
                    }
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
                    for (int i = 0; i < floors.Length; i++)
                    {
                        if (floors.All(m => m.GetType() != rt))
                            AddModule(Activator.CreateInstance(rt) as LBSModule, i);
                    }
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
            foreach (var level in floors) { foreach (LBSModule module in level.Modules) module.OnAttach(this); }
            foreach (LBSAssistant assistant in assistants) assistant.OnAttachLayer(this);
            // generator rules intentionally not auto-attached here
            foreach (LBSBehaviour behaviour in behaviours) behaviour.OnAttachLayer(this);

            InitializeContextEvents();
        }

        public void RemoveAll()
        {
            // iterate safely from end to start
            for(int i = floors.Length - 1; i >= 0; i--)
            {
                for (int j = floors[i].Modules.Count - 1; j >= 0; j--) RemoveModule(floors[i].Modules[j]);
            }
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
            LBSFloor[] clonedModules = CloneFloorArray(this.floors);
            List<LBSAssistant> clonedAssistants = this.assistants.Select(a => a.Clone() as LBSAssistant).ToList();
            List<LBSGeneratorRule> clonedRules = this.generatorRules.Select(r => r.Clone() as LBSGeneratorRule).ToList();
            List<LBSBehaviour> clonedBehaviours = this.behaviours.Select(b => b.Clone() as LBSBehaviour).ToList();

            return new LBSLayer(clonedModules, clonedAssistants, clonedRules, clonedBehaviours, Parent, id, visible, name, iconGuid, TileSize);
        }

        public static LBSFloor[] CloneFloorArray(LBSFloor[] input)
        {
            LBSFloor[] output = new LBSFloor[input.Length];
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = new LBSFloor(input[i].Modules);
            }
            return output;
        }
        public override bool Equals(object obj)
        {
            if (obj is not LBSLayer other) return false;
            if (other.id != id || other.name != name) return false;
            if (!floors.SequenceEqual(other.floors)) return false;
            if (!behaviours.SequenceEqual(other.behaviours)) return false;
            if (!assistants.SequenceEqual(other.assistants)) return false;
            if (!generatorRules.SequenceEqual(other.generatorRules)) return false;
            if (!settings.Equals(other.settings)) return false;
            return true;
        }

        public override int GetHashCode() => id.GetHashCode();
        /*
        private LBSFloor[] CloneFloorArray(LBSFloor[] input)
        {
            LBSFloor[] output = new LBSFloor[input.Length];
            for(int i = 0; i < output.Length; i++)
            {
                output[i] = new LBSFloor(input[i].Modules);
            }
            return output;
        }//*/
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
                Assert.IsTrue(AddModule(sectorTM, activeFloor));

                // clone connectedTM as a temporary connected module
                if (new List<LBSModule> { connectedTM }.Clone()[0] is ConnectedTileMapModule zoneConnected)
                {
                    zoneConnected.ID = "TempConnectedModule";

                    var floorTags = GetBehaviour<ExteriorBehaviour>()?.NavigableTags;
                    sectorTM.BuildFromExterior(connectedTM, zoneConnected, floorTags);
                    Assert.IsTrue(AddModule(zoneConnected, activeFloor));
                }
            }

            void VertexExteriorRemove()
            {
                Assert.IsTrue(RemoveModule(GetModule<SectorizedTileMapModule>()));
                Assert.IsTrue(RemoveModule(GetModule<ConnectedTileMapModule>("TempConnectedModule")));
            }
        }


        #endregion

        #region Types Boolean

        public bool IsExteriorLayer(ConnectedTileMapModule.ConnectedTileType type)
        {
            ExteriorBehaviour eb = GetBehaviour<ExteriorBehaviour>();
            if (eb is null) return false;
            return eb.GridType == type;
        }

        public bool IsPopulationLayer()
        {
            PopulationBehaviour pb = GetBehaviour<PopulationBehaviour>();
            return pb != null;
        }

        public bool IsQuestLayer()
        {
            QuestBehaviour qb = GetBehaviour<QuestBehaviour>();
            return qb != null;
        }

        public bool IsInteriorLayer()
        {
            SchemaBehaviour sb = GetBehaviour<SchemaBehaviour>();
            return sb != null;
        }

        #endregion

        #region BlueprintClone

        /// <summary>
        /// Returns a clone that only has data within a given area
        /// </summary>
        /// <param name="s">Start Position</param>
        /// <param name="e">End Position</param>
        /// <returns></returns>
        public LBSLayer GetAreaClone(Vector2Int s, Vector2Int e)
        {
            LBSLayer clone = Clone() as LBSLayer;

            List<object> components = new();
            //components.AddRange(clone.Modules);
            //components.AddRange(clone.Behaviours);
            //components.AddRange(clone.Assistants);
            components.AddRange(floors);
            components.AddRange(behaviours);
            components.AddRange(assistants);
            bool validLayer = false;
            
            foreach (object comp in components)
            {
                if(comp is IBlueprintable blueprintable)
                {
                    bool hasData = blueprintable.CaptureAreaData(s, e);
                    if(!validLayer && hasData) validLayer = true;
                }
            }

            if(validLayer) return clone;
            else return null;
        }

        public Vector2Int GetAnchor()
        {
            var mainAnchor = new Vector2Int(int.MaxValue, int.MinValue);

            List<object> components = new();
            components.AddRange(floors);
            components.AddRange(Behaviours);
            components.AddRange(Assistants);

            foreach (object comp in components)
            {
                if (comp is IBlueprintable blueprintable)
                {
                    var anchor = blueprintable.GetAnchor();
                    if (anchor.x < mainAnchor.x) mainAnchor.x = anchor.x;
                    if (anchor.y > mainAnchor.y) mainAnchor.y = anchor.y;
                }
            }

            return mainAnchor;
        }

        public void SetPosition(Vector2Int parentAnchor, Vector2Int delta)
        {
            List<object> components = new();
            components.AddRange(floors);
            components.AddRange(Behaviours);
            components.AddRange(Assistants);


            foreach (object comp in components)
            {
                if (comp is IBlueprintable blueprintable)
                {
                    blueprintable.SetPosition(parentAnchor, delta);
                }
            }
        }

        // never called
        public bool CaptureAreaData(Vector2Int min, Vector2Int max)
        {
            return true;
        }
        #endregion
    }
}
