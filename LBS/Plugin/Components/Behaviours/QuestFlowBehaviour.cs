using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;


namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class QuestFlowBehaviour : LBSBehaviour
    {
        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();
        
        public QuestFlowBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override void OnGUI()
        {

        }
        
        public override object Clone()
        {
            return new QuestFlowBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
        }

        public override void OnDetachLayer(LBSLayer layer) { }

        public override void CheckKeys() { }
    }
}