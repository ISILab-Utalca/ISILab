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
        private NodeKind branchType;
        #endregion

        #region PROPERTIES
        public bool IsAnd => branchType == NodeKind.And;
        public bool IsOr => branchType == NodeKind.Or;

        
        public override void InitTrigger(Node paramNode, LBSGenerator3DSettings settings = null, float pivotY = 0)
        {
            if(paramNode is BranchNode bn)
            {
                branchType = bn.Kind;
            }
        }

        #endregion

        #region METHODS



        protected override bool CanComplete()
        {
            if (IsAnd)
            {
                foreach(var trigger in Previous)
                {
                    if (trigger.State != QuestState.Completed) 
                        return false;
                }
                return true;
            }

            // Is or
            foreach(var trigger in Previous)
            {
                if (trigger.State == QuestState.Completed) 
                    return true;
            }
            return false;
            
        }

        internal override void Activate()
        {
            base.Activate();
            TryComplete();
        }

        public override string ToString() => IsAnd ? "And" : "Or";
        #endregion


    }
}
