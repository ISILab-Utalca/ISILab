

using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSCustomLabel: Label
    {
        
        private Color hintColor;
        
        [UxmlAttribute]
        public Color HintColor {
            get { return hintColor; }
            set
            {
                this.hintColor = value;
            } 
        }
        
        
        
        public LBSCustomLabel() : base()
        {
            RemoveFromClassList("");
            AddToClassList("lbs-label");
        }

        public LBSCustomLabel(string _text = "") : this()
        {
            this.text = _text;
        }
        
        
    }
}
