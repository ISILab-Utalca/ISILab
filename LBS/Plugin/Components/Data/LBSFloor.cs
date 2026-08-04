using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace LBS.Components
{
    [Serializable]
    public class LBSFloor
    {
        [SerializeField, SerializeReference] private List<LBSModule> modules = new List<LBSModule>();

        public List<LBSModule> Modules => modules;

        public LBSFloor(IEnumerable<LBSModule> modules = null)
        {
            if (modules is null) return;
            this.modules = modules.ToList().Clone();
        }

    }
}