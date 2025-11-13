using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Behaviours
{

    [System.Serializable]
    [RequieredModule(typeof(ConnectedTileMapModule))]
    public class SimpleConectedBehaviour : LBSBehaviour
    {
        public SimpleConectedBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint) { }

        public override object Clone()
        {
            return new SimpleConectedBehaviour(IconGuid, Name, ColorTint);
        }

        public override void OnGUI()
        {
        }
        
        public override void OnAttachLayer(LBSLayer layer)
        {
        }

        public override void OnDetachLayer(LBSLayer layer)
        {
        }
    }
}