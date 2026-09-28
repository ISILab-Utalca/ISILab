using LBS.Components;
using PathOS;
using ISILab.LBS.Assistants;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    public class SimulationAssistant : LBSAssistant
    {

        public System.Action OnDetach;

#if UNITY_EDITOR
        private PathOSWindow pathOSOriginalWindow;
        public PathOSWindow PathOSOriginalWindow { get => pathOSOriginalWindow; set => pathOSOriginalWindow = value; }
#endif

        public SimulationAssistant(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override object Clone()
        {
            return new SimulationAssistant(IconGuid, Name, ColorTint);
        }

        public override void OnGUI() { }

        public override void OnDetachLayer(LBSLayer layer)
        {
            base.OnDetachLayer(layer);
            OnDetach?.Invoke();
#if UNITY_EDITOR
            Object.DestroyImmediate(pathOSOriginalWindow);
#endif
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