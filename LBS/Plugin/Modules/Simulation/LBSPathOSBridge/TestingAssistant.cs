using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using LBS.Components;
using PathOS;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    [RequieredModule(typeof(PathOSModule))]
    public class TestingAssistant : LBSAssistant
    {
        private PathOSWindow pathOSOriginalWindow;

        public System.Action OnDetach;

        public PathOSWindow PathOSOriginalWindow { get => pathOSOriginalWindow; set => pathOSOriginalWindow = value; }

        public TestingAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override object Clone()
        {
            return new TestingAssistant(IconGuid, Name, ColorTint);
        }

        public override void OnGUI()
        {
            
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
            base.OnDetachLayer(layer);
            OnDetach?.Invoke();
            Object.DestroyImmediate(pathOSOriginalWindow);
        }

        public override bool Equals(object obj)
        {
            if(obj is not TestingAssistant other) return false;

            if(!Equals(Name, other.Name)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}