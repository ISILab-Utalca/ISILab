using LBS.Components;
using PathOS;
using System.Collections;
using ISILab.LBS.Plugin.Components.Behaviours;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    public class SimulationAssistant : LBSAssistant
    {
        private PathOSWindow pathOSOriginalWindow;

        public System.Action OnDetach;

        public PathOSWindow PathOSOriginalWindow { get => pathOSOriginalWindow; set => pathOSOriginalWindow = value; }

        public SimulationAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override object Clone()
        {
            return new SimulationAssistant(IconGuid, Name, ColorTint);
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
            if(obj is not SimulationAssistant other) return false;

            if(!Equals(Name, other.Name)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}