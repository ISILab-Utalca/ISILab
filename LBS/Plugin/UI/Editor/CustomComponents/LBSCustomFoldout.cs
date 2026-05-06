using ISILab.DevTools.Macros;
using ISILab.LBS.Macros;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using System.Runtime.CompilerServices;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomFoldout : Foldout
    {
        
        public enum FoldoutStyle {Classic, Modern}
        
        //Foldout
        public const string FOLDOUT_USS = "lbs-foldout";
        public const string FOLDOUT_CONTENT_PANEL = "lbs-foldout-panel";
        public const string FOLDOUT_CONTENT_MENU_BUTTON = "lbs-foldout-menu-button";
        public const string FOLDOUT_CHECKMARK = "lbs-custom-foldout-checkmark";
        public const string ICON_USS_CLASS = "lbs-icon";

        public bool initialValue = true;
        
        //backing fields
        private VectorImage arrowDownIcon;
        private VectorImage arrowSideIcon;
        private VectorImage dotsIcon;
        private VectorImage leftIcon;
        private Color iconColor = Color.white;
        private FoldoutStyle  foldoutStyle = FoldoutStyle.Classic;
        
        //Visual elements references
        private ToolbarMenu m_RightDropDown;
        private VisualElement m_LeftIconElement;
        private VisualElement arrowVisualElement;
        private VisualElement content;

        [UxmlAttribute]
        public VectorImage LeftIcon
        {
            get => leftIcon;
            set
            {
                leftIcon = value;
                if (m_LeftIconElement != null)
                {
                    m_LeftIconElement.style.backgroundImage = leftIcon? 
                        new StyleBackground(leftIcon) :
                        new StyleBackground(AssetMacro.LoadPlaceholderTexture());
                    m_LeftIconElement.style.display = leftIcon? DisplayStyle.Flex : DisplayStyle.None; 
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
                if (iconColor == null)
                {
                    iconColor = Color.white;
                }
                if (m_LeftIconElement != null)
                {
                    m_LeftIconElement.style.unityBackgroundImageTintColor = iconColor;
                }
            }
        }
            
        [UxmlAttribute]
        public bool InitialValue
        {
            get => initialValue;
            set
            {
                initialValue = value;
                this.value = initialValue;
                UpdateArrow(initialValue);
            }
        }

        [UxmlAttribute]
        public FoldoutStyle SelectedFoldoutStyle
        {
            get => foldoutStyle;
            set
            { 
                foldoutStyle = value; 
                switch (foldoutStyle) 
                { 
                    case FoldoutStyle.Classic:
                    { 
                        RemoveFromClassList("modern");
                        AddToClassList("classic");
                        break;
                    }
                    case FoldoutStyle.Modern:
                    {
                        RemoveFromClassList("classic");
                        AddToClassList("modern");
                        break;
                        
                    } 
                }
            }
        }
        
        
        public LBSCustomFoldout() : base()
        {
            this.AddToClassList(FOLDOUT_USS);
            this.AddToClassList("lbs");
            
            this.text = "LBS Custom Foldout";
            
            m_RightDropDown = new ToolbarMenu();
            m_RightDropDown.AddToClassList(FOLDOUT_CONTENT_MENU_BUTTON);
            m_RightDropDown.RemoveFromClassList(ToolbarMenu.ussClassName);
            
            m_LeftIconElement = new VisualElement();
            m_LeftIconElement.style.backgroundImage = AssetMacro.LoadPlaceholderTexture();
            m_LeftIconElement.AddToClassList(ICON_USS_CLASS);
            m_LeftIconElement.name = "Icon";
            m_LeftIconElement.style.marginLeft = 5;
            m_LeftIconElement.style.marginRight = 5;

            Toggle mToggle = this.Q<Toggle>();
            mToggle.RemoveFromClassList(Toggle.ussClassName);
            mToggle.RemoveFromClassList(toggleUssClassName);
            mToggle.AddToClassList(FOLDOUT_USS + "__toggle");
            
            content = this.Q<VisualElement>("unity-content");
            arrowVisualElement = this.Q<VisualElement>("unity-checkmark");
            arrowVisualElement.AddToClassList(FOLDOUT_CHECKMARK);
            
            Label contentLabel = this.Q<Label>(classes: textUssClassName);
            contentLabel.AddToClassList("unity-base-field__label");

            arrowDownIcon = AssetMacro.LoadAssetByGuid<VectorImage>("b570a25de51f01c41bd82dbe5372bb3f");
            arrowSideIcon = AssetMacro.LoadAssetByGuid<VectorImage>("83eafacbab9ab554299bc4d0f124d980");
            dotsIcon = AssetMacro.LoadAssetByGuid<VectorImage>("4fc870f9e2f488d4bb2c1bffe1f5b751");

            UpdateArrow(this.value);
            
            content.AddToClassList(FOLDOUT_CONTENT_PANEL);
            content.style.marginLeft = 0;
            
            mToggle.Add(m_RightDropDown);
            
            // hack!
            VisualElement labelContainer = contentLabel.parent;
            labelContainer.Add(m_LeftIconElement);
            m_LeftIconElement.PlaceBehind(contentLabel);
            if (leftIcon == null) m_LeftIconElement.style.display = DisplayStyle.None;
            
            VisualElement toolbarButtonIcon = this.Query<VisualElement>(classes: "unity-toolbar-menu__arrow");
            TextElement toolbarLabel  = m_RightDropDown.Query<TextElement>(classes: "unity-toolbar-menu__text");
            toolbarLabel.style.display = DisplayStyle.None;
            
            if (toolbarButtonIcon != null)
            {
                toolbarButtonIcon.style.backgroundImage = new StyleBackground(dotsIcon);
                m_RightDropDown.visible = false;
            }

            this.value = initialValue;
            mToggle.RegisterValueChangedCallback(OnChangeEvent);
            this.style.marginBottom = 2;
        }

        void OnChangeEvent(ChangeEvent<bool> _evt)
        {
            UpdateArrow(_evt.newValue);
            _evt.StopPropagation();
        }

        public void AddContent(VisualElement newContent)
        {
            content.Add(newContent);
        }

        private void UpdateArrow(bool isOpen)
        {
            if (arrowVisualElement != null && arrowDownIcon != null)
            {
                if (isOpen)
                {
                    arrowVisualElement.style.backgroundImage = new StyleBackground(arrowDownIcon);
                }
                else
                {
                    arrowVisualElement.style.backgroundImage = new StyleBackground(arrowSideIcon);
                }
            }
            else
            {
                arrowVisualElement.style.backgroundImage = AssetMacro.LoadPlaceholderTexture();
            }
        }
    }
}

