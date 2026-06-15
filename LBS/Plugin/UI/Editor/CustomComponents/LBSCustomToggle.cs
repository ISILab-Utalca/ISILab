using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomToggle : UnityEngine.UIElements.Toggle
    {
        private VectorImage activeImage;
        private VectorImage inactiveImage;
        private VisualElement checkmark;

        [UxmlAttribute]
        public VectorImage ActiveImage
        {
            get => activeImage;
            set
            {
                activeImage = value;
                UpdateVisuals();
            }
        }

        [UxmlAttribute]
        public VectorImage InactiveImage
        {
            get => inactiveImage;
            set
            {
                inactiveImage = value;
                UpdateVisuals();
            }
        }

        public LBSCustomToggle() : base()
        {
            RemoveFromClassList("unity-toggle");
            AddToClassList("lbs-custom-toggle");

            // Listen for state changes (checked / unchecked)
            RegisterCallback<ChangeEvent<bool>>(OnValueChanged);

            // Wait until the element is inside the panel hierarchy to grab the checkmark safely
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            // Find Unity's internal checkmark element
            checkmark = this.Q(className: "unity-toggle__checkmark");
            UpdateVisuals();
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // If the element hasn't finished loading into the panel hierarchy yet, skip
            if (checkmark == null) return;

            // If no custom vectors are provided, clear overrides and let default Unity USS take over
            if (activeImage == null && inactiveImage == null)
            {
                checkmark.style.backgroundImage = StyleKeyword.Null;
                checkmark.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                return;
            }

            // Force vector graphic scale details to render beautifully without color distortion tints
           // checkmark.style.unityBackgroundImageTintColor = Color.white;

            // Swap out the vector asset depending on the current boolean choice value
            if (value) // 'value' is inherited from Toggle
            {
                checkmark.style.backgroundImage = activeImage != null
                    ? new StyleBackground(activeImage)
                    : StyleKeyword.Null;
            }
            else
            {
                checkmark.style.backgroundImage = inactiveImage != null
                    ? new StyleBackground(inactiveImage)
                    : StyleKeyword.Null;
            }

            checkmark.AddToClassList("lbs-icon");
        }
    }
}