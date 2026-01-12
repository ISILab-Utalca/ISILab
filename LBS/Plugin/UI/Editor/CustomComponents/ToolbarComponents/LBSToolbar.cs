using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSToolbar: Toolbar
    {
        public enum AccentColor { DEFAULT, DARKEST }

        private AccentColor toolbarAccentColor = AccentColor.DEFAULT;

        [UxmlAttribute]
        public AccentColor ToolbarAccentColor
        {
            get =>toolbarAccentColor;
            set
            {
                toolbarAccentColor = value;
                switch (value)
                {
                    case AccentColor.DEFAULT:
                    {
                        AddToClassList("prop-default-bg");
                        RemoveFromClassList("prop-darkest-bg");
                        break;   
                    }
                    case AccentColor.DARKEST:
                    {
                        AddToClassList("prop-darkest-bg");
                        RemoveFromClassList("prop-default-bg");
                        break;
                    }
                }
            }
        }
        
        readonly string _lbsClassName = "lbs-toolbar";
        public LBSToolbar() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(_lbsClassName);
            ToolbarAccentColor = toolbarAccentColor;
        }
    }
}


