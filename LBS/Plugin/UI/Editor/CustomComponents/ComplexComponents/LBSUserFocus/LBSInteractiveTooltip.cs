using System;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class LBSInteractiveTooltip : VisualElement
    {
        #region FIELDS
        private Label _titleLabel;
        private Label _descriptionLabel;
        private VisualElement _iconElement;
        
        private LBSCustomButton _actionButton;
        
        private VisualElement _container;
        private Action _onClick;
        #endregion

        #region PROPERTIES

        [UxmlAttribute("icon-image")]
        public UnityEngine.Object IconImageReference
        {
            get => _iconImageRef;
            set
            {
                _iconImageRef = value;
                if (_iconElement == null) return;
                _iconElement.style.backgroundImage = Extensions.VisualElementExtensions.ConvertToBackground(value);
            }
        }
        private UnityEngine.Object _iconImageRef;
        
        [UxmlAttribute]
        public Color ButtonTint
        {
            get => _actionButton.ButtonTint;
            set => _actionButton.ButtonTint = value;
        }
        public LBSCustomButton ActionButton
        {
          get => _actionButton;
          set => _actionButton = value;
        }
      
        [UxmlAttribute]
        public string TitleText
        {
            get => _titleLabel.text;
            set => _titleLabel.text = value;
        }

        [UxmlAttribute]
        public string DescriptionText
        {
            get => _descriptionLabel.text; 
            set => _descriptionLabel.text = value;
        }

        [UxmlAttribute]
        public string ButtonLabel
        {
            get => _actionButton.text;
            set => _actionButton.text = value;
        }

        [UxmlAttribute]
        public bool DisplayButton
        {
            get => _actionButton.style.display.value == DisplayStyle.Flex; 
            set =>  _actionButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
        
        #region CONSTRUCTORS
        public LBSInteractiveTooltip()
        {
            style.flexDirection = FlexDirection.Column;
            style.paddingTop = 6;
            style.paddingBottom = 6;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.backgroundColor = LBSSettings.Instance.view.toolkitNormalDark;

            SetVisualLayout();
           
            // default uxml values
            _titleLabel.text = TitleText;
            _descriptionLabel.text = DescriptionText;
            _actionButton.text = ButtonLabel;
            _actionButton.style.display = DisplayButton ? DisplayStyle.Flex : DisplayStyle.None;
            _iconElement.style.backgroundImage =  _iconElement.style.backgroundImage;
        }
        
        #endregion

        #region METHODS
        /// <summary>
        /// Add a handler for the action button.
        /// </summary>
        public void SetAction(Action onClick)
        {
            _onClick = onClick;
            _actionButton.clicked -= OnButtonClicked;
            _actionButton.clicked += OnButtonClicked;
        }

        private void SetVisualLayout()
        {
            _container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.FlexStart,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4,
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                    borderBottomWidth = 1
                }
            };

            _iconElement = new VisualElement
            {
                name = "icon",
                style =
                {
                    width = 24,
                    height = 24,
                    marginRight = 8,
                    flexShrink = 0
                }
            };

            VisualElement textGroup = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    flexBasis = Length.Percent(60),
                    marginRight = 6
                }
            };

            _titleLabel = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12
                }
            };

            _descriptionLabel = new Label
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)),
                    whiteSpace = WhiteSpace.Normal
                }
            };

            textGroup.Add(_titleLabel);
            textGroup.Add(_descriptionLabel);

            _actionButton = new LBSCustomButton()
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = Length.Percent(40),
                    fontSize = 10,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginLeft = 6,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            _actionButton.AddToClassList(_actionButton.LBSClassName);
            _actionButton.clicked += OnButtonClicked;

            _container.Add(_iconElement);
            _container.Add(textGroup);
            _container.Add(_actionButton);

            Add(_container);
        }
        
        // Use to assign values at editor time
        private void SetTooltipContent(string titleText, string descriptionText, string buttonText,
            bool displayButton, StyleBackground icon)
        {
            _titleLabel.text = titleText;
            _descriptionLabel.text = descriptionText;
            _actionButton.text = buttonText;
            _actionButton.style.display = displayButton ? DisplayStyle.Flex : DisplayStyle.None;
            _iconElement.style.backgroundImage =  icon;
        }
        
        private void OnButtonClicked()
        {
            _onClick?.Invoke();
        }
        #endregion
    }
}
