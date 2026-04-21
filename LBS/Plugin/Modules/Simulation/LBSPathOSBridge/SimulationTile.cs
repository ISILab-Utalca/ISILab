using System;
using System.Collections.Generic;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using Newtonsoft.Json;
using PathOS;
using UnityEngine;

// GABO TODO: FALTA VER COMO TRATAR ELEMENT TAGS Y EVENT TAGS.
namespace ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge
{
    [System.Serializable]
    public class SimulationTile
    {
        #region FIELDS
        [SerializeField, SerializeReference, JsonRequired]
        private SimulationBehaviour owner;
        [SerializeField, JsonRequired]
        private int x, y;
        [SerializeField, JsonRequired]
        //private PathOSTag tag;
        private LBSTag tag;
        [SerializeField, JsonRequired]
        private bool lockedDoorPOI;
        [SerializeField, JsonRequired]
        private EntityType entityType;
        // Booleanos para Event Tags
        [SerializeField, JsonRequired]
        private bool isDynamicTagObject = false;
        [SerializeField, JsonRequired]
        private bool isDynamicObstacleObject = false;
        [SerializeField]
        private LBSSimulationObstacleConnections obstacles;
        [SerializeField]
        private SimulationDynamicTagConnections dynamicTagTiles;
        #endregion

        #region CONSTRUCTORS
        public SimulationTile(SimulationBehaviour owner, int x, int y, EntityType type, LBSTag tag = null, bool lockedDoorPOI = false)
        {
            this.owner = owner;
            this.x = x;
            this.y = y;
            obstacles = new LBSSimulationObstacleConnections(isNull: true);
            dynamicTagTiles = new SimulationDynamicTagConnections(isNull: true);
            entityType = type;
            if (tag != null) { this.tag = tag; }

            LockedDoorPOI = lockedDoorPOI;
        }
        #endregion

        #region EVENTS
        public Action OnAddObstacle;
        public Action OnRemoveObstacle;
        public Action OnAddDynamicTag;
        public Action OnRemoveDynamicTag;
        public Action OnConvertingToObstacleTrigger;
        public Action OnConvertingToObstacleObject;
        public Action OnConvertingToDynamicTagTrigger;
        public Action OnConvertingToDynamicTagObject;
        public Action OnRevertingFromObstacleTrigger;
        public Action OnRevertingFromObstacleObject;
        public Action OnRevertingFromDynamicTagTrigger;
        public Action OnRevertingFromDynamicTagObject;
        #endregion

        #region PROPERTIES
        public SimulationBehaviour Owner { get { return owner; } set { Owner = value; } }
        public int X { get { return x; } set { x = value; } }
        public int Y { get { return y; } set { y = value; } }
        public Vector2Int Position { get { return new Vector2Int(x, y); } }
        public LBSTag Tag { get { return tag; } set { tag = value; } }
        public bool LockedDoorPOI { get => lockedDoorPOI; set => lockedDoorPOI = value; }
        public EntityType EntityType { get => entityType; set => entityType = value; }
        public bool IsDynamicTagObject
        {
            get { return isDynamicTagObject; }
            set
            {
                bool lastValue = isDynamicTagObject;

                isDynamicTagObject = value;

                // Eventos de conversion y reversion (solo si cambio es no redundante)
                if (value && value != lastValue) { OnConvertingToDynamicTagObject?.Invoke(); }
                else if (!value && value != lastValue) { OnRevertingFromDynamicTagObject?.Invoke(); }
            }
        }
        public bool IsDynamicObstacleObject
        {
            get { return isDynamicObstacleObject; }
            set
            {
                bool lastValue = isDynamicObstacleObject;

                isDynamicObstacleObject = value;

                // Eventos de conversion y reversion (solo si cambio es no redundante)
                if (value && value != lastValue) { OnConvertingToObstacleObject?.Invoke(); }
                else if (!value && value != lastValue) { OnRevertingFromObstacleObject?.Invoke(); }
            }
        }
        public bool IsDynamicTagTrigger
        {
            get { return !dynamicTagTiles.IsNull; }
            set
            {
                bool lastValue = !dynamicTagTiles.IsNull;

                if (value)
                {
                    // Solo instanciar si no existe
                    if (dynamicTagTiles.IsNull)
                    {
                        dynamicTagTiles = new(this, new());
                    }
                }
                else
                {
                    dynamicTagTiles = new SimulationDynamicTagConnections(isNull: true);
                }

                // Eventos de conversion y reversion (solo si cambio es no redundante)
                if (value && value != lastValue) { OnConvertingToDynamicTagTrigger?.Invoke(); }
                else if (!value && value != lastValue) { OnRevertingFromDynamicTagTrigger?.Invoke(); }
            }
        }
        public bool IsDynamicObstacleTrigger
        {
            get { return !obstacles.IsNull; }
            set
            {
                bool lastValue = !obstacles.IsNull;

                if (value)
                {
                    // Solo instanciar si no existe
                    if (obstacles.IsNull)
                    {
                        obstacles = new(this, new());
                    }
                }
                else
                {
                    obstacles = new LBSSimulationObstacleConnections(isNull: true);
                }

                // Eventos de conversion y reversion (solo si cambio es no redundante)
                if (value && value != lastValue) { OnConvertingToObstacleTrigger?.Invoke(); }
                else if (!value && value != lastValue) { OnRevertingFromObstacleTrigger?.Invoke(); }
            }
        }
        #endregion

        #region METHODS
        public List<(SimulationTile, LBSSimulationObstacleConnections.Category)> GetObstacles()
        {
            // Chequeo de existencia.
            if (obstacles.IsNull) return new(); // Devuelve lista vacia
            if (obstacles.Obstacles == null) return new();
            return obstacles.Obstacles;
        }
        // GABO TODO: Arreglar metodo cuando se arregle la clase y devolver VACIO similarmente a GetObstacles
        public List<(SimulationTile, SimulationTag)> GetDynamicTags()
        {
            return dynamicTagTiles.DynamicTagObjects;
        }

        public (SimulationTile, SimulationObstacleConnections.Category)? GetObstacle(int x, int y)
        {
            if (obstacles.IsNull) { return null; }
            return ((SimulationTile, SimulationObstacleConnections.Category)?)obstacles.GetObstacle(x, y);
        }

        public (SimulationTile, SimulationObstacleConnections.Category)? GetObstacle(SimulationTile tile)
        {
            if (obstacles.IsNull) { return null; }
            return obstacles.GetObstacle(tile) as (SimulationTile, SimulationObstacleConnections.Category)?;
        }
        public (SimulationTile, SimulationTag)? GetDynamicTag(int x, int y)
        {
            if (dynamicTagTiles.IsNull) { return null; }
            return dynamicTagTiles.GetDynamicTag(x, y);
        }
        public (SimulationTile, SimulationTag)? GetDynamicTag(SimulationTile tile)
        {
            if (dynamicTagTiles.IsNull) { return null; }
            return dynamicTagTiles.GetDynamicTag(tile);
        }

        public void AddObstacle(SimulationTile obstacleTile, LBSSimulationObstacleConnections.Category category)
        {
            // Chequeo de Condiciones
            if (obstacleTile == null) { Debug.LogWarning("Tile obstaculo es nulo!"); return; }
            if (!obstacleTile.isDynamicObstacleObject) { Debug.LogWarning("Tile dado no es obstaculo!"); return; }
            if (!IsDynamicObstacleTrigger)
            {
                IsDynamicObstacleTrigger = true;
            }
            obstacles.AddObstacle(obstacleTile, category);

            OnAddObstacle?.Invoke();
        }

        // *NOTA*: Segundo parametro indica si revisar condicion "IsDynamicObstacleObject".
        // Usado en "PathOSModule.CleanAllObstacleConnectionsTo" ya que su invocacion por suscripcion es
        // posterior al seteo de la propiedad en "False".
        public void RemoveObstacle(SimulationTile obstacleTile, bool checkIfObstacleObjectProperty = true)
        {
            // Chequeo de Condiciones
            if (obstacleTile == null) { Debug.LogWarning("Tile obstaculo es nulo!"); return; }
            if (!obstacleTile.isDynamicObstacleObject && checkIfObstacleObjectProperty) { Debug.LogWarning("Tile dado no es obstaculo!"); return; }
            if (!IsDynamicObstacleTrigger)
            {
                Debug.LogWarning("Este tile NO es DynamicObstacleTrigger!");
                return;
            }
            // Chequeo de existencia en mapa
            var currConnection = obstacles.GetObstacle(obstacleTile.x, obstacleTile.y);
            if (currConnection == null) { Debug.LogWarning("No existe tile en la posicion!"); return; }
            if (obstacleTile.Tag.Label != currConnection?.Item1.Tag.Label)
            {
                Debug.LogWarning("Tag.Label del tile a remover es distinto del existente!");
                return;
            }

            obstacles.RemoveObstacle(obstacleTile.X, obstacleTile.Y);

            OnRemoveObstacle?.Invoke();
        }

        public void AddDynamicTag(SimulationTile tagTile, SimulationTag tag)
        {
            // Chequeo de Condiciones
            if (tagTile == null) { Debug.LogWarning("Tag tile es nulo!"); return; }
            if (!tagTile.isDynamicTagObject) { Debug.LogWarning("Tile dado no es DynamicTagObject!"); return; }
            if (!IsDynamicTagTrigger)
            {
                IsDynamicTagTrigger = true;
            }
            dynamicTagTiles.AddDynamicTag(tagTile, tag);

            OnAddDynamicTag?.Invoke();
        }

        // *NOTA*: Segundo parametro indica si revisar condicion "IsDynamicTagObject".
        // Usado en "PathOSModule.CleanAllDynamicTagConnectionsTo" ya que su invocacion por suscripcion es
        // posterior al seteo de la propiedad en "False".
        public void RemoveDynamicTag(SimulationTile tagTile, bool checkIfDynamicTagObjectProperty = true)
        {
            // Chequeos
            if (tagTile == null) { Debug.LogWarning("Tag tile es nulo!"); return; }
            if (!tagTile.isDynamicTagObject && checkIfDynamicTagObjectProperty) { Debug.LogWarning("Tile dado no es DynamicTileObject!"); return; }
            if (!IsDynamicTagTrigger)
            {
                Debug.LogWarning("Este tile NO es DynamicTagTrigger!");
                return;
            }
            // Chequeo de existencia en mapa
            var currConnection = dynamicTagTiles.GetDynamicTag(tagTile.x, tagTile.y);
            if (currConnection == null) { Debug.LogWarning("No existe tile en la posicion!"); return; }
            if (tagTile.Tag.Label != currConnection?.Item1.Tag.Label)
            {
                Debug.LogWarning("Tag.Label del tile a remover es distinto del existente!");
                return;
            }

            dynamicTagTiles.RemoveDynamicTag(tagTile.X, tagTile.Y);

            OnRemoveDynamicTag?.Invoke();
        }

        public override bool Equals(object obj)
        {
            if (obj is not SimulationTile other) return false;

            if (!other.owner.Equals(owner)) return false;

            if(!other.Position.Equals(Position)) return false;

            if(!Equals(other.Tag, Tag)) return false;

            // Not tested yet
            //if(other.IsDynamicTagObject != IsDynamicTagObject) return false;
            //if(other.isDynamicObstacleObject != IsDynamicObstacleObject) return false;
            //if(other.IsDynamicTagTrigger != IsDynamicTagTrigger) return false;
            //if(other.IsDynamicObstacleTrigger != IsDynamicObstacleTrigger) return false;

            return true;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(
                owner,
                x,
                y,
                tag
            );
        }

        #endregion

        #region NOT_IN_USE
        //public void AddTag(PathOSTag tag)
        //{
        //    // Chequeo nulo
        //    if (tag == null) { Debug.LogWarning("PathOSTile.AddTag(): Tag nulo!"); return; }

        //    // Remocion tag antiguo (si existe) y agregar nuevo
        //    var t = GetTag(tag);
        //    if (t != null)
        //    {
        //        tags.Remove(t);
        //    }
        //    tags.Add(tag);
        //}

        //public PathOSTag GetTag(PathOSTag tag)
        //{
        //    if (tags.Count <= 0)
        //        return null;
        //    return tags.Find(t => t.Label.Equals(tag.Label));

        //}

        //public void RemoveTag(PathOSTag tag)
        //{
        //    // Chequeo nulo
        //    if (tag == null) { Debug.LogWarning("PathOSTile.RemoveTag(): Tag nulo!"); return; }

        //    var t = GetTag(tag);
        //    if (t != null)
        //    {
        //        tags.Remove(t);
        //    }
        //}
        #endregion


    }
}
