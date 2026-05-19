using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEngine;
namespace ISILab.LBS.Plugin.MapTools.Generators
{
    /// <summary>
    /// Represents a branching condition in the quest system.
    /// Defines child triggers, a destination trigger, and logic
    /// for AND/OR evaluation.
    /// </summary>
    [DisallowMultipleComponent]
    [Serializable]
    public class QuestTriggerBranch : QuestTrigger
    {
        #region FIELDS

        [SerializeField]
        private bool Or;
        #endregion

        #region PROPERTIES
        public bool IsAnd => Or == false;
        public bool IsOr => Or == true;

        
        public override void InitTrigger(GraphNode paramNode, LBSGenerator3DSettings settings = null, float pivotY = 0)
        {
            Or = paramNode as OrNode != null;
        }

        #endregion

        #region METHODS



        protected override bool CanComplete()
        {
            if (IsAnd)
            {
                foreach(var trigger in AllPrevious)
                {
                    if (trigger.State != QuestState.Completed) 
                        return false;
                }
                return true;
            }

            // Is or
            foreach(var trigger in AllPrevious)
            {
                if (trigger.State == QuestState.Completed) 
                    return true;
            }
            return false;
            
        }



        #endregion


    }
}
