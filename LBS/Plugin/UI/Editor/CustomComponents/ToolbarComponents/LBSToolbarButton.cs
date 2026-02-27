using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;



namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSToolbarButton: ToolbarButton
    {
        readonly string lbsClassName = "lbs-toolbar-button";
        
        private Color iconColor = Color.white;
        private HintType buttonHintMode = HintType.Normal;
        
        public enum HintType{ Normal, Info, Success, Warning, Error }
        
        [UxmlAttribute]
        public Color IconColor
        {
            get => iconColor;
            set
            {
                iconColor = value;
                Image imageVe = this.Q<Image>(classes:"unity-button__image");
                if (iconColor != Color.white && imageVe != null)
                {
                    //imageVe.style.unityBackgroundImageTintColor = new StyleColor(iconColor);
                    imageVe.tintColor = IconColor;
                    
                }
            }
        }


        [UxmlAttribute]
        public HintType ButtonHintMode
        {
            get => buttonHintMode;
            set
            {
                buttonHintMode = value;
                switch (buttonHintMode)
                {
                    case HintType.Normal:
                        ClearClassList();
                        AddToClassList("lbs-toolbar-button");
                        AddToClassList("normal-color");
                        break;
                    case HintType.Error:
                        ClearClassList();
                        AddToClassList("lbs-toolbar-button");
                        AddToClassList("alert-color");
                        break;
                }
            }

        }



        public LBSToolbarButton() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(lbsClassName);
            AddToClassList("normal-color");
            
        }
    }
}

