using ISILab.Commons.Utility.Editor;
using LBS;
using System;
using System.Collections;
using System.Collections.Generic;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    
    
    [UxmlElement]
    public partial class ToolButton : Toggle, IGrupable
    {
        #region FIELDS

        private Color _color ; //LBSSettings.Instance.view.toolkitNormal;
        private Color _selected; //LBSSettings.Instance.view.newToolkitSelected;
        #endregion

        #region FIELDS VIEW
        private readonly VisualElement icon;
        private VectorImage toolIconBackground;
        private VisualElement father;
        #endregion
        
        
        #region PROPERTIES

        [UxmlAttribute]
        public VectorImage ToolIconBackground
        {
            get => toolIconBackground;
            set
            {
                toolIconBackground = value;
                if (icon != null)
                {
                    if (toolIconBackground != null)
                    {
                        icon.style.backgroundImage = new StyleBackground(toolIconBackground);
                        icon.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        icon.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        public VisualElement Father => father;
        
        #endregion
        
        #region EVENTS
        public event Action OnFocusEvent;
        public event Action OnBlurEvent;
        #endregion

        #region CONSTRUCTORS

        public ToolButton() : base()
        {
            _color = style.backgroundColor.value;
            icon = new VisualElement();
            icon.AddToClassList("prop-centered");
            this.Add(icon);
            AddToClassList("lbs-rounded-button");
            
            RemoveFromClassList("unity-button");
            RemoveFromClassList("unity-text-element");
            RemoveFromClassList("unity-base-field");
        }
        
        
        public ToolButton(LBSTool _tool, VisualElement content) : this()
        {
            father = content;

            VisualElement checkbox = this.Q<VisualElement>(classes: "unity-base-field__input");
            checkbox.style.display = DisplayStyle.None;

            if (_tool.Icon == null) return;
                icon.style.backgroundImage = new StyleBackground(_tool.Icon); 
            tooltip = _tool.Name;
        }
        #endregion

        #region IGRUPABLE
        public void AddGroupEvent(Action action)
        {
            RegisterCallback<ClickEvent>(evt => action?.Invoke());
        }

        public void OnBlur()
        {
            RemoveFromClassList("prop-state--checked");
            OnBlurEvent?.Invoke();
        }

        public void OnFocus()
        {
            AddToClassList("prop-state--checked");
            OnFocusEvent?.Invoke();
        }

        public void OnFocusWithoutNotify()
        {
            AddToClassList("prop-state--checked");
        }

        public void OnBlurWithoutNotify()
        {
            RemoveFromClassList("prop-state--checked");
        }

        public void SetColorGroup(Color color, Color selected)
        {
            _color = color;
            _selected = selected;
        }

        public string GetLabel()
        {
            return tooltip;
        }
        #endregion
    }
}