using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LBS.Components
{
    [Serializable]
    public class LBSFloor
    {
        [SerializeField, SerializeReference] private List<LBSModule> modules = new List<LBSModule>();

        public List<LBSModule> Modules { get { return modules; } }

        public LBSFloor(IEnumerable<LBSModule> modules = null)
        {
            if (modules is null) return;
            this.modules = modules.ToList().Clone();
        }

    }
}