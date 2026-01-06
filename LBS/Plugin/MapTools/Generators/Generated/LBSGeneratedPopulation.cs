using ISILab.Commons.Extensions;
using ISILab.LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
                    if(addon is Addon_Patrol patrol)
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
            foreach(var addon in addons)
            {
                if(addon is Addon_Trigger trigger)
                    MakeTriggers(trigger);
                
                if(addon is Addon_Interact interact) 
                    GenEventHooker.AssignEvents(interact.Interact);

                if(addon is Addon_Destruct destroy)
                    GenEventHooker.AssignEvents(destroy.Destroyed);
            }        

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
        #endregion
    }

}