using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomObjectField: ObjectField
    {
        public static readonly string LBSClassName = "lbs-field";
        public static readonly string LBSFieldClassName = "lbs-object-field";
        
        private VectorImage iconImage;
        private IconPosition iconPosition = IconPosition.Left;

        private VisualElement iconVisualElement;

        private bool _useCustomFilter = false;
        private bool _selectorHooked;

        public Action<Action<UnityEngine.Object>> CustomFilter { get; set; }
        public string InvalidSelectionMessage { get; set; }

        #region Properties

        [UxmlAttribute]
        public VectorImage IconImage
        {
            get => iconImage;
            set
            {
                iconImage = value;
                if (iconVisualElement != null)
                {
                    if (iconImage == null)
                    {
                        iconVisualElement.style.display = DisplayStyle.None;
                        return;
                    }
                    iconVisualElement.style.display = DisplayStyle.Flex;
                    iconVisualElement.style.backgroundImage = new StyleBackground(iconImage);
                }
            }
        }

        [UxmlAttribute]
        public IconPosition IconSide
        {
            get => iconPosition;
            set
            {
                iconPosition = value;
                
                if (iconVisualElement != null)
                {
                    SetIconPosition(iconPosition);
                }
            }
        }

        [UxmlAttribute]
        public bool UseCustomFilter
        {
            get => _useCustomFilter;
            set
            {
                _useCustomFilter = value;
                if (_useCustomFilter)
                {
                    TryHookSelector();
                }
            }
        }

        #endregion

        public LBSCustomObjectField() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(LBSClassName);
            AddToClassList(LBSFieldClassName);
            
            iconVisualElement = new VisualElement();
            iconVisualElement.AddToClassList(LBSCustomStyle.LBS_ICON);
            this.Add(iconVisualElement);
            
            if (iconImage != null)
            {
                iconVisualElement.style.backgroundImage = new StyleBackground(iconImage);
                SetIconPosition(iconPosition);
            }
            else
            {
                iconVisualElement.style.display = DisplayStyle.None;
            }

            RegisterCallback<AttachToPanelEvent>(_ => TryHookSelector());
            RegisterCallback<GeometryChangedEvent>(_ => TryHookSelector());
        }

        void SetIconPosition(IconPosition _iconPosition)
        {
            switch (_iconPosition)
            {
                case IconPosition.Left:
                    iconVisualElement.style.display = DisplayStyle.Flex;
                    iconVisualElement.SendToBack();
                    break;
                case IconPosition.None:
                    iconVisualElement.style.display = DisplayStyle.None;
                    break;
                case IconPosition.Right:
                    iconVisualElement.style.display = DisplayStyle.Flex;
                    iconVisualElement.BringToFront();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OpenCustomFilter()
        {
            if (!_useCustomFilter || CustomFilter == null)
                return;

            CustomFilter(obj =>
            {
                if (obj == null)
                    return;

                if (objectType != null && !objectType.IsInstanceOfType(obj))
                {
                    NotifyInvalid();
                    return;
                }

                if (!allowSceneObjects && !EditorUtility.IsPersistent(obj))
                {
                    NotifyInvalid();
                    return;
                }

                value = obj;
            });
        }

        private void NotifyInvalid()
        {
            if (!string.IsNullOrEmpty(InvalidSelectionMessage))
            {
                LBSMainWindow.MessageNotify(new LBSLog(InvalidSelectionMessage, LogType.Warning));
            }
        }

        private void TryHookSelector()
        {
            if (!_useCustomFilter || _selectorHooked)
                return;

            var selector = this.Q(className: "unity-object-field__selector");
            if (selector == null) return;

            _selectorHooked = true;

            //Stops the default object field behavior and opens the custom picker instead

            selector.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                OpenCustomFilter();
            }, TrickleDown.TrickleDown);

            selector.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            selector.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
                {
                    evt.StopImmediatePropagation();
                    evt.StopPropagation();
                    OpenCustomFilter();
                }
            }, TrickleDown.TrickleDown);
        }

    }

    
}
