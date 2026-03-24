using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Trigger : BundleTileMapAddons
    {
        [SerializeReference]
        private List<TileTrigger> triggers = new();

        public List<TileTrigger> Triggers { get => triggers; set => triggers = value; }

        public Addon_Trigger() { }

        public Addon_Trigger(List<TileTrigger> InTriggers)
        {
            triggers = InTriggers;
        }

        public override object Clone()
        {
            Addon_Trigger clone = new Addon_Trigger();
            foreach(var trigger in Triggers)
            {
                clone.Triggers.Add(trigger.Clone() as TileTrigger);
            }
            return clone;
        
        }
    }
}
