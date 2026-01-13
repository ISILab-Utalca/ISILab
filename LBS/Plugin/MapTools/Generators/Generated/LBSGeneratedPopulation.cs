using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [Serializable]
    public class LBSGeneratedPopulation : LBSGenerated
    {
        #region FIELDS
        // addons from the tilegroup that was used to generate this object in the LBS tool
        [SerializeField, SerializeReference, HideInInspector]
        List<BundleTileMapAddons> addons = new();
        private LBSGeneratedEventHook genEventHooker;
        private object uhl;


        #endregion

        #region PROPERTIES

        public LBSGeneratedEventHook GenEventHooker
        {
            get
            {
                genEventHooker = gameObject.GetComponent<LBSGeneratedEventHook>();
                genEventHooker ??= gameObject.AddComponent<LBSGeneratedEventHook>();
                return genEventHooker;
            }
        }

        public List<Vector2> PatrolPoints
        {
            get
            {
                List<Vector2> points = new();
                foreach (var addon in addons)
                {
                    if (addon is Addon_Patrol patrol)
                    {
                        points = patrol.Points;

                        if (patrol.Loops)
                            patrol.Points.Add(patrol.Points[0]);

                        break;
                    }
                }
                return points;
            }
        }

        public List<BundleTileMapAddons> Addons
        {
            get => addons;
            set
            {
                addons = value;
                BindAddons();
            }
        }
        #endregion

        #region CONSTRUCTORS
        public LBSGeneratedPopulation() { }

        #endregion

        #region METHODS

        private void Awake()
        {
        }

        private void BindAddons()
        {
            foreach (BundleTileMapAddons addon in addons)
            {
                if (addon is Addon_Trigger trigger)
                    MakeTriggers(trigger);

                if (addon is Addon_Interact interact)
                    GenEventHooker.AssignEvents(interact.Interact);

                if (addon is Addon_Destruct destroy)
                    GenEventHooker.AssignEvents(destroy.Destroyed);

                if (addon is Addon_Unlock unlock)
                    MakeUnlock(unlock);

                if (addon is Addon_TriggerUnlock unlockTrigger)
                    MakeUnlockTrigger(unlockTrigger);
            }

        }

        private void MakeUnlock(Addon_Unlock unlock)
        {

            LBSGeneratedInterior[] interiors = FindObjectsByType<LBSGeneratedInterior>(FindObjectsSortMode.None);
            foreach (var interior in interiors)
            {
                // add component once
                if (gameObject.GetComponent<LBSUnlockHandler>() is not null) continue;

                if (!interior.Connection.Equals(unlock.Connection)) continue;

                AddUnlockHandle(interior.gameObject);

                foreach (var otherInt in interiors)
                {
                    if (interior.ConnectedTile != otherInt.Connection.tile) continue;
                    if (interior.Connection.IsConected(otherInt.Connection.connections))
                    {
                        AddUnlockHandle(otherInt.gameObject);
                    }
                }
            }
        }

        private void AddUnlockHandle(GameObject go)
        {
            LBSUnlockHandler ulh = go.AddComponent<LBSUnlockHandler>();
            ulh.KeyComponent = this;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * 2;
            box.isTrigger = true;
        }

        private void MakeUnlockTrigger(Addon_TriggerUnlock unlockTrigger)
        {
            var interiors = FindObjectsByType<LBSGeneratedInterior>(FindObjectsSortMode.None);

            foreach (TriggerUnlockEntry conn in unlockTrigger.Connections)
            {
                foreach (Addon_Unlock unlock in conn.Unlocks)
                {
                    foreach (LBSGeneratedInterior interior in interiors)
                    {
                        if (!interior.Connection.Equals(unlock.Connection))
                            continue;

                        // Prevent duplicates
                        if (interior.GetComponentInChildren<LBSTriggerHandler>() != null)
                            continue;

                        AddUnlockTriggerHandle(conn, interior.gameObject);

                        foreach (var otherInt in interiors)
                        {
                            if (interior.ConnectedTile != otherInt.Connection.tile) continue;
                            if (interior.Connection.IsConected(otherInt.Connection.connections))
                            {

                                AddUnlockTriggerHandle(conn, otherInt.gameObject);
                            }
                        }
                    }
                }
            }
        }


        private void AddUnlockTriggerHandle(TriggerUnlockEntry conn, GameObject interiorObject)
        {
            // no need to assign the KeyComponent as this unlock is opened via method: OnUnlock()
            LBSUnlockHandler ulh = interiorObject.AddComponent<LBSUnlockHandler>();

            GameObject child = new GameObject("UnlockTrigger");
  
            BoxCollider box = child.AddComponent<BoxCollider>();
            box.size = Vector3.one * 2;
            box.isTrigger = true;

            child.transform.SetParent(gameObject.transform);
            child.transform.position = gameObject.transform.position;

            LBSTriggerHandler handler = child.AddComponent<LBSTriggerHandler>();
            LBSGeneratedEventHook lgeh = child.AddComponent<LBSGeneratedEventHook>();

            LBSEventHooker hooker = new();

            UnityActionStored newAction = new UnityActionStored();
            newAction.componentName = nameof(LBSUnlockHandler);
            newAction.eventType = LBSEventType.TriggerEnter;
            newAction.methodName = LBSUnlockHandler.methodName;
            newAction.objectName = interiorObject.name;
            newAction.TriggerOnce = true;

            hooker.RegisteredActions.Add(newAction);
            hooker.Target = interiorObject;
            handler.ActivationMode = conn.ActivationMode;
            handler.Hooker = hooker;

        }

        private void MakeTriggers(Addon_Trigger TriggerAddon)
        {
            // set triggers first
            foreach (TileTrigger triggerArea in TriggerAddon.Triggers)
            {
                GameObject child = new GameObject("ChildTrigger");
                Collider collider;

                if (triggerArea.Ttype == TileTriggerType.Box)
                {
                    BoxCollider box = child.AddComponent<BoxCollider>();
                    box.size = Vector3.one * triggerArea.Range;
                    collider = box;
                }
                else
                {
                    SphereCollider sphere = child.AddComponent<SphereCollider>();
                    sphere.radius = triggerArea.Range;
                    collider = sphere;
                }

                collider.isTrigger = true;

                child.transform.SetParent(gameObject.transform);
                child.transform.position = gameObject.transform.position;

                LBSTriggerHandler handler = child.AddComponent<LBSTriggerHandler>();
                handler.Hooker = triggerArea._eventHooker;
                handler.ActivationMode = triggerArea.activationMode;

            }
        }

        private void OnDestroy()
        {
            //Destroy(gameObject);
            if (GenEventHooker != null) GenEventHooker.BroadcastEvent(Components.Data.LBSEventType.Destroy);
        }
        public void Interact()
        {
            if (GenEventHooker != null) GenEventHooker.BroadcastEvent(Components.Data.LBSEventType.Interact);
        }

        private bool IsUnlockable()
        {
            foreach (var addon in Addons)
            {
                if (addon is Addon_Unlock unlock) return true;
            }
            return false;
        }

        internal string GetID()
        {
            /* unlocables use their instanced ID just as when they are assigned to their 
             * connection/lock handler */
            if (IsUnlockable()) return gameObject.GetInstanceID().ToString();

            // common items use the bundle guid
            return AssetMacro.GetGuidFromAsset(BundleRef);
        }
        #endregion
    }

}