using System;
using ISILab.Extensions;
using UnityEditor.Graphs;
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
                    Color.RGBToHSV(buttonTint, out float h , out float s, out float v);
                    hoverButtonTint = Color.HSVToRGB(h, s, Mathf.Clamp( v + 0.1f,0f,1f));
                    pressedButtonTint = Color.HSVToRGB( h, 
                        Mathf.Clamp( s + 0.1f,0f,1f), 
                        Mathf.Clamp( v - 0.2f,0f,1f));
                    
                    SetOverlayColors(buttonTint);
                }
            }
        }
        
        
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
        
        private Color iconColor = Color.white;
        private Color buttonTint = Color.white;
        private Color hoverButtonTint = Color.white;
        private Color pressedButtonTint = Color.white;
        
        public LBSCustomButton() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(LBSClassName);
            //style.backgroundColor = new StyleColor(buttonTint);
            SetOverlayColors(buttonTint);
            RegisterCallback<MouseEnterEvent>((_evt => SetOverlayColors(hoverButtonTint)));
            RegisterCallback<MouseLeaveEvent>((_evt => SetOverlayColors(buttonTint)));
            RegisterCallback<ClickEvent>((_evt => SetOverlayColors(pressedButtonTint)));
            RegisterCallback<MouseUpEvent>((_evt => SetOverlayColors(buttonTint)));
            // RegisterCallback<AttachToPanelEvent>((_evt => SetOverlayColors(buttonTint)));
            
        }

        public void SetOverlayColors(Color _newColor = new Color())
        {
            if (_newColor != Color.white)
            {
                style.backgroundColor = new StyleColor(_newColor);
            }
        }
    }
}

