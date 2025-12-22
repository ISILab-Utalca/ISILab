using ISILab.Commons.Extensions;
using ISILab.LBS.Components;
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
        BundleTileMapAddons addons;
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
                if (addons.Patrol.Loop) addons.Patrol.Points.Add(addons.Patrol.Points[0]);
                return addons.Patrol.Points;
            }
        }

        public BundleTileMapAddons Addons
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
            // set triggers first
            foreach (TileTrigger triggerArea in addons.Triggers)
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

            GenEventHooker.AssignEvents(addons.Interact);
            GenEventHooker.AssignEvents(addons.Destroyed);

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