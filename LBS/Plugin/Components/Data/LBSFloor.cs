using ISILab.LBS.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LBSFloor
{
    [SerializeField, SerializeReference] private List<LBSModule> modules = new List<LBSModule>();

    public List<LBSModule> Modules { get { return modules; } }

    public LBSFloor() { }
}
