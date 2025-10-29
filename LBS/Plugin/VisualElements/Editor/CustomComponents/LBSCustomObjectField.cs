using ISILab.LBS.Editor.Windows;
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

        private bool _useCustomPicker = false;
        private bool _selectorHooked;

        public Action<Action<UnityEngine.Object>> CustomPicker { get; set; }
        public Func<UnityEngine.Object, bool> CustomFilter { get; set; }
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
        public bool UseCustomPicker
        {
            get => _useCustomPicker;
            set
            {
                _useCustomPicker = value;
                if (_useCustomPicker)
                {
                    TryHookSelector(); // engancha el botón nativo
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

        public void OpenCustomPicker()
        {
            if (!_useCustomPicker || CustomPicker == null)
                return;

            CustomPicker(obj =>
            {
                if (obj == null)
                    return;

                // Validación por tipo y escena
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

                // Validación extra del usuario
                if (CustomFilter != null && !CustomFilter(obj))
                {
                    NotifyInvalid();
                    return;
                }

                // Asigna el valor (dispara el ValueChanged del ObjectField)
                value = obj;
            });
        }

        private void NotifyInvalid()
        {
            if (!string.IsNullOrEmpty(InvalidSelectionMessage))
            {
                LBSMainWindow.MessageNotify(InvalidSelectionMessage, LogType.Warning);
            }
        }

        private void TryHookSelector()
        {
            if (!_useCustomPicker || _selectorHooked)
                return;

            // En versiones nuevas: ObjectField.selectorUssClassName
            var selector = this.Q(className: "unity-object-field__selector");
            if (selector == null) return;

            _selectorHooked = true;

            selector.RegisterCallback<PointerDownEvent>(evt =>
            {
                // Cancelar el selector estándar y abrir el custom
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                OpenCustomPicker();
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
                    OpenCustomPicker();
                }
            }, TrickleDown.TrickleDown);
        }

    }
}
