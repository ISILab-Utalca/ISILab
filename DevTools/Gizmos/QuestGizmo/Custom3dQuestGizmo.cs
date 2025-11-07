using System.Collections.Generic;
using ISILab.LBS;
using UnityEditor;
using UnityEngine;

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
            get => tracker;
            set => tracker = value;
        }

        public QuestTrigger Trigger
        {
            get => trigger;
            set => trigger = value;
        }
        
        [HideInInspector]
        public List<Vector3> Positions = new();
        

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            foreach (Vector3 Position in Positions)
            {
                Gizmos.DrawLine(transform.position, Position);
            }
        }
    }
}
