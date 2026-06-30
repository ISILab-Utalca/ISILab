using ISILab.LBS.Modules;
using UnityEngine;

namespace ISILab.LBS.Components
{


    public class BranchNode : Node
    {
        
        public BranchNode(string id, NodeKind kind, Vector2 position, Graph graph) : base(position, graph) 
        {
            this.id = id;
            this.kind = kind;
        }
        public override object Clone() => new BranchNode(ID, kind, Position, graph);
        public override string ToString() => $"Branch ({kind})";
        
        /*
        public new Rect Area
        {
            get
            {
                var pos = graph.OwnerLayer.ToFixedPosition(Position);
                return new Rect(pos, Area.size);
            }
        }
        */
    }

}
