using System;
using System.Collections.Generic;
using ISILab.LBS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ISI_Lab.LBS.DevTools
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class Custom3dQuestGizmo : Custom3dGizmo
    {
        private QuestTrigger trigger;
        private QuestTracker tracker;
        public QuestTracker Tracker
        {
            get
            {
                tracker ??= GetComponent<QuestTracker>();
                return tracker;
            }
            set => tracker = value;
        }

        public QuestTrigger Trigger
        {
            get
            {
                trigger ??= GetComponent<QuestTrigger>();
                return trigger;
            }
            set => trigger = value;
        }
        
        [HideInInspector]
        public List<QuestTrigger> prevTriggers = new();
        

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            foreach (QuestTrigger prevTrigger in prevTriggers)
            {
                if(!prevTrigger) continue;
                Gizmos.DrawLine(transform.position, prevTrigger.transform.position);
                Custom3dQuestGizmo gizmo = prevTrigger.GetComponent<Custom3dQuestGizmo>();
                gizmo?.DrawCustomMesh();
            }
        }
    }
}
