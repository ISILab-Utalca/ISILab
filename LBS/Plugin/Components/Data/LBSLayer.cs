using ISILab.Commons.Utility;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Assistants;
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
        [SerializeField, JsonRequired] private string subTypeId = "None";
        [SerializeField, JsonRequired] private string name = "Layer name";
        [SerializeField] private Vector2Int tileSize = new Vector2Int(2, 2);

        [SerializeField, SerializeReference] private LBSFloor[] floors = new LBSFloor[10];
        [SerializeField, SerializeReference] private List<LBSBehaviour> behaviours = new();
        [SerializeField, SerializeReference] private List<LBSAssistant> assistants = new();
        [SerializeField, SerializeReference] private List<LBSGeneratorRule> generatorRules = new();

        [JsonIgnore] private LBSLevelData _parent;
        [JsonIgnore] private int activeFloor = 0;
        [HideInInspector, SerializeField, JsonRequired] public int index;

        #endregion

        #region Properties

        [JsonIgnore] public bool IsVisible { get => visible; set => visible = value; }
        [JsonIgnore] public bool IsBlocked { get => blocked; set => blocked = value; }
        [JsonIgnore] public bool IsLocked { get => blocked; set => blocked = value; }

        [JsonIgnore] public LBSLevelData Parent { get => _parent; set => _parent = value; }
        [JsonIgnore] public string ID { get => id; }
        [JsonIgnore] public string SubTypeID { get => subTypeId; }
        [JsonIgnore] public string Name { get => name; set => name = value; }
        [JsonIgnore] public int ActiveFloor { get => activeFloor; }
        [JsonIgnore] public int FloorCount { get => floors.Length; }

        // Return copies to protect internal lists
        [JsonIgnore] public List<LBSBehaviour> Behaviours => new(behaviours);
        [JsonIgnore] public List<LBSAssistant> Assistants => new(assistants);
        [JsonIgnore] public List<LBSGeneratorRule> GeneratorRules => new(generatorRules);

        // "First" lists are less safe, but are meant to be used in editor
        // as a quick way to make design changes.
        [JsonIgnore] public List<LBSModule> FirstModules => floors[0].Modules;
        [JsonIgnore] public List<LBSBehaviour> FirstBehaviours => behaviours;
        [JsonIgnore] public List<LBSAssistant> FirstAssistants => assistants;
        [JsonIgnore] public List<LBSGeneratorRule> FirstGeneratorRules => generatorRules;

        [JsonIgnore]
        public Vector2Int TileSize
        {
            get => tileSize;
            set
            {
                tileSize = value;
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
            behaviours ??= new List<LBSBehaviour>();
            assistants ??= new List<LBSAssistant>();
            generatorRules ??= new List<LBSGeneratorRule>();
            floors = new LBSFloor[LBSSettings.Instance.general.defaultFloorCount];
            for(int i = 0; i < floors.Length; i++)
            {
                floors[i] ??= new ();
            }

            IsVisible = true;
            id = GetType().Name;
        }

        public LBSLayer(
            LBSFloor[] modules,
            IEnumerable<LBSAssistant> assistant,
            IEnumerable<LBSGeneratorRule> rules,
            IEnumerable<LBSBehaviour> behaviours,
            LBSLevelData parent,
            string ID, string SubTypeID, bool visible, string name, string iconGuid, Vector2Int tileSize) : this()
        {
            floors = new LBSFloor[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                floors[i] ??= new();
                if (modules[i] != null) foreach (LBSModule m in modules[i].Modules) AddModule(m, i);
            }
            if (assistant != null) foreach (LBSAssistant a in assistant) AddAssistant(a);
            if (rules != null) foreach (LBSGeneratorRule r in rules) AddGeneratorRule(r);
            if (behaviours != null) foreach (LBSBehaviour b in behaviours) AddBehaviour(b);

            Parent = parent;
            id = ID;
            subTypeId = SubTypeID;
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
            if (newFloor < 0 || newFloor >= floors.Length) return;
            //if (newFloor == activeFloor) return;

            var prevFloor = activeFloor;
            activeFloor = newFloor;
            Reload();

            foreach (var behaviour in Behaviours)
            {
                behaviour.ChangeLevelRender(prevFloor, newFloor);
                behaviour.FloorChangedCallback?.Invoke(newFloor);
            }//*/
        }
        #endregion

        #region Modules
        public List<LBSModule> Modules(int floorIndex = -1)
        {
            if (floorIndex < 0) floorIndex = activeFloor;
            if (floors[floorIndex] == null)
                ;
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
            //if (module == null) return false;

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

        public void RemoveModuleInAllFloors(LBSModule module)
        {
            for (int i = 0; i < floors.Length; i++)
            {
                var toRemove = floors[i].Modules.Find(m => m.ID == module.ID);
                if (toRemove != null)
                {
                    floors[i].Modules.Remove(toRemove);
                    try { toRemove.OnDetach(this); } catch { /* swallow detach errors */ }
                    OnRemoveModule?.Invoke(this, toRemove);
                }
            }
        }

        public LBSModule GetModule(int levelIndex, int posIndex) => floors[levelIndex].Modules[posIndex];

        public LBSModule GetModule(string moduleID)
            => floors[activeFloor].Modules.FirstOrDefault(m => string.Equals(m?.ID, moduleID, StringComparison.Ordinal));

        public T GetModule<T>(string moduleID = "", int index = -1) where T : LBSModule
        {
            if (index < 0) index = activeFloor;
            if (floors is null)
                ;
            if (floors[index] is null)
                ;
            if (floors[index].Modules is null)
                ;
            if (floors[index].Modules.OfType<T>() is null)
                ;
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
        
        #region Behaviors
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

        /// <summary>
        /// ID is set on creation and should not be changed, but this allows for manual setting
        /// if needed (e.g. storing presets on Layer Template).
        /// </summary>
        /// <remarks>
        /// Use on your own risk - changing ID can break references in modules and behaviours
        /// that reference the layer by ID.
        /// </remarks>
        /// <param name="newID">New identification string. Must be not null or empty.</param>
        public void SetID(string newID)
        {
            if (string.IsNullOrEmpty(newID)) return;
            id = newID;
        }

        public void SetSubTypeID(string newSubTypeID)
        {
            if (string.IsNullOrEmpty(newSubTypeID)) return;
            subTypeId = newSubTypeID;
        }

        public void ChangeFloorCount(uint newCount)
        {
            var prevCount = floors.Length;
            if (newCount < 1 || newCount == prevCount) return;

            floors = floors.Resize((int)newCount);
            for (int i = 0; i < floors.Length; i++) 
            { 
                if (floors[i] == null) floors[i] = new LBSFloor(FirstModules); 
            }
        }

        public void Reload()
        {
            foreach (var floor in floors) { foreach (LBSModule module in floor.Modules) module.OnAttach(this); }
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
            LBSFloor[] clonedModules = floors != null ? CloneFloorArray(floors) : Array.Empty<LBSFloor>();

            List<LBSAssistant> clonedAssistants = new();
            if (assistants != null)
            {
                clonedAssistants = assistants
                    .Where(a => a != null)
                    .Select(a => a.Clone() as LBSAssistant)
                    .Where(cloned => cloned != null)
                    .ToList();
            }

            List<LBSGeneratorRule> clonedRules = new();
            if (generatorRules != null)
            {
                clonedRules = generatorRules
                    .Where(r => r != null)
                    .Select(r => r.Clone() as LBSGeneratorRule)
                    .Where(cloned => cloned != null)
                    .ToList();
            }

            List<LBSBehaviour> clonedBehaviours = new();
            if (behaviours != null)
            {
                clonedBehaviours = behaviours
                    .Where(b => b != null)
                    .Select(b => b.Clone() as LBSBehaviour)
                    .Where(cloned => cloned != null)
                    .ToList();
            }

            return new LBSLayer(
                clonedModules,
                clonedAssistants,
                clonedRules,
                clonedBehaviours,
                Parent,
                id,
                subTypeId,
                visible,
                name,
                iconGuid,
                TileSize
            );
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
            if (!tileSize.Equals(other.tileSize)) return false;
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
            components.AddRange(clone.floors);
            components.AddRange(clone.Behaviours);
            components.AddRange(clone.Assistants);
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

        public bool MergeLayerData(object incoming, bool overwrite)
        {
            var Merger = incoming as LBSLayer;
            if (Merger == null) return false;

            List<object> mergerComponents = new();
            mergerComponents.AddRange(Merger.Modules());
            mergerComponents.AddRange(Merger.Behaviours);
            mergerComponents.AddRange(Merger.Assistants);

            List<object> components = new();
            components.AddRange(Modules());
            components.AddRange(Behaviours);
            components.AddRange(Assistants);

            CloneRefs.Start();

            foreach (object comp in components)
            {
                if (comp is IBlueprintable blueprintable)
                {
                    foreach (object mergerComp in mergerComponents)
                    {
                        blueprintable.MergeLayerData(mergerComp, overwrite);
                    }
                }
            }

            CloneRefs.End();


            return true;
        }
        #endregion

    }
}
