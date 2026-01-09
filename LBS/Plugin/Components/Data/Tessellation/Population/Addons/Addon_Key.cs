using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class Addon_Key : BundleTileMapAddons
    {

        [SerializeReference]
        private List<TileTrigger> triggers = new();

        public List<TileTrigger> Triggers { get => triggers; set => triggers = value; }

        public Addon_Key() { }

        public Addon_Key(List<TileTrigger> InTriggers)
        {
            triggers = InTriggers;
        }

    }
}
