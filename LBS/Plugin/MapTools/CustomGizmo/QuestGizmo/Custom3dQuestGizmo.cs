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
        
        public QuestTrigger Trigger
        {
            get
            {
                trigger ??= GetComponent<QuestTrigger>();
                return trigger;
            }
            set => trigger = value;
        }
       
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            foreach (QuestTrigger prevTrigger in trigger.Previous)
            {
                if(!prevTrigger) continue;
                UnityEngine.Gizmos.DrawLine(transform.position, prevTrigger.transform.position);
                Custom3dQuestGizmo gizmo = prevTrigger.GetComponent<Custom3dQuestGizmo>();
                gizmo?.DrawCustomMesh();
            }

            foreach (QuestTrigger nextTrigger in trigger.Next)
            {
                if (!nextTrigger) continue;
                UnityEngine.Gizmos.DrawLine(transform.position, nextTrigger.transform.position);
                Custom3dQuestGizmo gizmo = nextTrigger.GetComponent<Custom3dQuestGizmo>();
                gizmo?.DrawCustomMesh();
            }
        }
    }
}
