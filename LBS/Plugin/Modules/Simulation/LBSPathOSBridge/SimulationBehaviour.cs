using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using LBS.Components.TileMap;
using Newtonsoft.Json;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Behaviours
{
    [System.Serializable]
    [RequieredModule(typeof(SimulationModule))]
    public class SimulationBehaviour : LBSBehaviour
    {
        #region FIELDS
        [JsonRequired]
        private SimulationModule module;
        #endregion

        #region META-FIELDS
        [JsonIgnore]
        public Bundle selectedToSet;

        [JsonIgnore]
        public LBSTag lowStairTag;
        [JsonIgnore]
        public LBSTag highStairTag;

        public List<Bundle> pathOSBundles;
        #endregion

        #region PROPERTIES
        public List<SimulationTile> Tiles { get { return module.GetTiles(); } private set { } }

        public List<Bundle> Bundles
        {
            get
            {
                if(pathOSBundles == null || pathOSBundles.Count == 0)
                    pathOSBundles = LBSAssetsStorage.Instance.Get<Bundle>()
                        .Where(b => b.GetCharacteristics<LBSSimulationTagsCharacteristic>().Count > 0).ToList();
                return pathOSBundles;
            }
        }

        public bool AutoMap { get; private set; }
        #endregion

        #region EVENTS
        public event Action<SimulationModule, SimulationTile> OnAddTile
        {
            add { module.OnAddTile += value; }
            remove { module.OnAddTile -= value; }
        }
        public event Action<SimulationModule, SimulationTile> OnApplyEventTile
        {
            add { module.OnApplyEventTile += value; }
            remove { module.OnApplyEventTile -= value; }
        }
        public event Action<SimulationModule, SimulationTile> OnRemoveTile
        {
            add { module.OnRemoveTile += value; }
            remove { module.OnRemoveTile -= value; }
        }

        public Action AutoMapCallback;
        public Action RemoveAutoMapCallbacks;
        #endregion

        #region CONSTRUCTORS
        public SimulationBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }
        #endregion

        #region METHODS
        public void AddTile(LBSTag tag, int x, int y, EntityType type, bool lockedDoorPOI = false)
        {
            SimulationTile tile = new SimulationTile(this, x, y, type, tag, lockedDoorPOI);

            bool isElement = true;
            bool isEvent = false;

            // Add Tile or ApplyEventTile segun defina el tag asociado
            // Tags de Elementos
            //if (tag.Category == PathOSTag.PathOSCategory.ElementTag)
            if(isElement)
            {
                // Si el tile a agregar es del AGENTE, se restringe a uno:
                // Si ya existe, se borra el anterior.
                //if (tag.Label == "PathOSAgent")
                //{
                //    var oldAgentTile = module.GetTiles().Find(t => t.Tag.Label == "PathOSAgent");
                //    if (oldAgentTile != null)
                //    {
                //        module.RemoveTile(oldAgentTile);
                //        RequestTileRemove(oldAgentTile);
                //    }
                //}

                SimulationTile old = module.GetTile(tile.X, tile.Y);
                //PathOSTile old = module.GetTile(tile);
                if (old != null) 
                {
                    RequestTileRemove(old);
                }
                module.AddTile(tile);
                
                RequestTilePaint(tile);
            }
            // Tags de eventos
            //else if (tag.Category == PathOSTag.PathOSCategory.EventTag)
            else if(isEvent)
            {
                SimulationTile oldTile = module.GetTile(x, y);
                // El tile de agente no puede recibir eventos
                if (oldTile != null && oldTile.Tag.Label == "PathOSAgent") { return; }
                // Un tile de muro no puede recibir tags de Trigger
                if (oldTile != null &&
                    oldTile.Tag.Label == "Wall" &&
                    (tag.Label == "DynamicObstacleTrigger" || tag.Label == "DynamicTagTrigger")) { return; }

                if (module.ApplyEventTile(tile))
                {
                    // I know this looks weird but it works like this
                    RequestTileRemove(oldTile);
                    RequestTilePaint(tile);
                    RequestTilePaint(oldTile);
                }
            }
        }

        public void RemoveTile(int x, int y)
        {
            var t = module.GetTile(x, y);
            module.RemoveTile(t);
            RequestTileRemove(t);
        }

        public void RemoveTile(SimulationTile t)
        {
            module.RemoveTile(t);
            RequestTileRemove(t);
        }

        public SimulationTile GetTile(int x, int y)
        {
            return module.GetTile(x, y);
        }

        public void MapToPopulation(List<TileBundleGroup> groups, List<LBSTile> doorTiles, List<LBSTile> lockedDoorTiles, List<LBSTile> lowStairTiles, List<LBSTile> highStairTiles)
        {
            //string s = string.Empty;
            //foreach(KeyValuePair<EntityType, PathOSStorage.SimulationEntityData> pair in PathOSStorage.Instance.entityDataPool)
            //{
            //    s += "Entity Type: " + pair.Key + " | Texture: " + (pair.Value.image ? pair.Value.image.name : null) + "\n";
            //}

            Debug.Log("Simulation Mapping performed.");

            ClearMapping();

            foreach(TileBundleGroup group in groups)
            {
                BundleData bundle = group.BundleData;
                EntityType entityType = bundle.Bundle.EntityType;
                var characteristics = bundle.Bundle.GetCharacteristics<LBSTagsCharacteristic>();
                if(characteristics.Count == 0)
                {
                    Debug.LogWarning($"Bundle '{bundle.BundleName}' doesn't have any LBSTagsCharacteristic.");
                    continue;
                }
                LBSTag tag = null;
                bool validTag = false;
                //bool playerTag = false;
                for(int i = 0; i < characteristics.Count; i++)
                {
                    foreach(var ctag in characteristics[i].TagEntries)
                    {
                        tag = ctag.Value;
                        if (tag != null)
                        {
                            if (entityType != EntityType.ET_NONE)
                            {
                                validTag = true;
                                break;
                            }
                            else if (tag.Label.Equals("Player"))
                            {
                                //var pathOSTags = Bundles.Select(b => b.GetCharacteristics<LBSPathOSTagsCharacteristic>()[0]).ToList();
                                //var agentTag = pathOSTags//pathOSBundles.Select(b => b.GetCharacteristics<LBSPathOSTagsCharacteristic>()[0])
                                //    .FirstOrDefault(tag => tag.Value.Label.Equals("PathOSAgent"));
                                //tag = agentTag//pathOSBundles.Select(b => b.GetCharacteristics<LBSPathOSTagsCharacteristic>()[0])
                                //    .Value.ToLBSTag();

                                validTag = true;
                                break;
                            }
                        }
                    }
                }
                //if (playerTag) // Deberia crear un agente
                //    continue;
                if(!validTag)
                {
                    Debug.LogWarning($"Bundle '{bundle.BundleName}' tags are invalid. Check for null values at LBSTagsCharacteristic or the DefaultType property of your LBSTags assets.");
                    continue;
                }
                foreach(LBSTile tile in group.TileGroup)
                {
                    Vector2Int pos = tile.Position;
                    AddTile(tag, pos.x, pos.y, entityType);
                }
            }

            foreach (LBSTile door in doorTiles)
                AddTile(null, door.x, door.y, EntityType.ET_POI);
            foreach (LBSTile door in lockedDoorTiles)
                AddTile(null, door.x, door.y, EntityType.ET_POI, true);
            foreach (LBSTile stair in lowStairTiles)
                AddTile(lowStairTag, stair.x, stair.y, EntityType.ET_POI);
            foreach (LBSTile stair in highStairTiles)
                AddTile(highStairTag, stair.x, stair.y, EntityType.ET_POI);
        }

        public void ClearMapping()
        {
            while(Tiles.Count > 0)
            {
                RemoveTile(Tiles[0]);
            }
        }

        #region INHERITED METHODS

        public override void ChangeLevelRender(int prevLevelIndex, int nextLevelIndex)
        {
            List<SimulationTile> oldTiles = new List<SimulationTile>();
            List<SimulationTile> newTiles = new List<SimulationTile>();

            var prevModuleList = OwnerLayer.Modules(prevLevelIndex);
            var nextModuleList = OwnerLayer.Modules(nextLevelIndex);

            var prevSectorizedMod = prevModuleList.Find(
                m => m.GetType() == typeof(SimulationModule)) as SimulationModule;
            var nextSectorizedMod = nextModuleList.Find(
                m => m.GetType() == typeof(SimulationModule)) as SimulationModule;

            foreach (var pTile in prevSectorizedMod.GetTiles())
            {
                oldTiles.Add(pTile);
            }
            foreach (var pTile in nextSectorizedMod.GetTiles())
            {
                newTiles.Add(pTile);
            }

            RequestFullRepaint(oldTiles, newTiles);
        }
        public override object Clone()
        {
            return new SimulationBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
            module = OwnerLayer.GetModule<SimulationModule>();
            OwnerLayer.OnChange += AutoMapCallback;
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            RemoveAutoMapCallbacks?.Invoke();
            OwnerLayer = null;
        }

        public override void CheckKeys()
        {
            UpdateKeys(Tiles.ToList<object>());
        }

        public override void OnGUI()
        {
            
        }

        public override bool Equals(object obj)
        {
            var other = obj as SimulationBehaviour;

            if (other == null) return false;

            if(!Equals(Name, other.Name)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #endregion
    }
}
