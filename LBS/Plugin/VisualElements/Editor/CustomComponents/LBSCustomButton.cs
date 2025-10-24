using System;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSCustomButton: Button
    {
        public readonly String LBSClassName = "lbs-button";
        
        [UxmlAttribute]
        public Color ButtonTint
        {
            get => buttonTint;
            set
            {
                buttonTint = value;
                if (value != Color.white)
                {
                    style.backgroundColor = new StyleColor(buttonTint);
                }
            }
        }

        [UxmlAttribute]
        public Color HoverColor { get => hoverColor; set => hoverColor = value; }
        [UxmlAttribute]
        public Color PressedColor { get => pressedColor; set => pressedColor = value; }
        
        
        private Color buttonTint = Color.white;
        private Color hoverColor = Color.white;
        private Color pressedColor = Color.white;
        
        
        public LBSCustomButton() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(LBSClassName);
            buttonTint = this.style.backgroundColor.value;
            
            
            SetOverlayColors(buttonTint);
            
            RegisterCallback<MouseEnterEvent>((_evt => SetOverlayColors(hoverColor)));
            RegisterCallback<MouseLeaveEvent>((_evt => SetOverlayColors(buttonTint)));
            RegisterCallback<MouseDownEvent>((_evt => SetOverlayColors(pressedColor)));
            RegisterCallback<MouseUpEvent>((_evt => SetOverlayColors(buttonTint)));
            RegisterCallback<AttachToPanelEvent>((_evt => SetOverlayColors(buttonTint)));
            RegisterCallbackOnce<NavigationSubmitEvent>((_evt =>
            {
             Debug.Log("Navigation submitted");   
            }));
        }



        public void SetOverlayColors(Color _newColor = new Color())
        {
            if (_newColor != Color.white)
            {
                style.backgroundColor = new StyleColor(buttonTint);
                
            }
        }
    }
}

