using ISILab.LBS.Plugin.MapTools.Generators;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.CustomGizmo.QuestGizmo
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
                UnityEngine.Gizmos.DrawLine(transform.position, prevTrigger.transform.position);
                Custom3dQuestGizmo gizmo = prevTrigger.GetComponent<Custom3dQuestGizmo>();
                gizmo?.DrawCustomMesh();
            }
        }
    }
}
