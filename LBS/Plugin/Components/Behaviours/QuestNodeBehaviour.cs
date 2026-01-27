using ISILab.LBS.Modules;
using LBS.Components;
using Color = UnityEngine.Color;

namespace ISILab.LBS.Behaviours
{
    [RequieredModule(typeof(QuestGraph))]
    public class QuestNodeBehaviour : LBSBehaviour
    {
        public QuestGraph Graph => OwnerLayer.GetModule<QuestGraph>();
        
        /// <summary>
        /// Assigned from the QuestNodeView On MouseDown event. It will assign the current selected node, allowing to
        /// modify it based on its action type.
        /// </summary>
      

        public QuestNodeBehaviour(string IconGuid, string name, Color colorTint) : base(IconGuid, name, colorTint)
        {
        }

        public override void OnGUI()
        {
  
        }
        
        public override object Clone()
        {
            return new QuestNodeBehaviour(this.IconGuid, this.Name, this.ColorTint);
        }

        public override void OnAttachLayer(LBSLayer layer)
        {
            OwnerLayer = layer;
        }

        public override void OnDetachLayer(LBSLayer layer) { }
        
        private void ChangeVisuals()
        {
            RequestTileRemove(this);
            RequestTilePaint(this);
        }

        public override void CheckKeys() { } // Quest Behaviour does this for the rest of behaviours from the quest layer
    }
}