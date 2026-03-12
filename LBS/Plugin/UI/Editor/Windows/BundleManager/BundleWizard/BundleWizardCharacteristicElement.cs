using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// <b>For <see cref="BundleWizardSetCharacteristMenu"/> exclusive use.</b><br />
    /// Visual element for choosing a single <see cref="LBSCharacteristic"/>.
    /// </summary>
    [UxmlElement]
    public partial class BundleWizardCharacteristicElement : LBSComplexVisualElement
    {
        private Toggle toggle;
        private Label charLabel;
        private string questionTooltipText;
        private VisualElement questionTooltip;

        private int index;
        public EventCallback<ChangeEvent<bool>> toggleCallback;


        public Toggle Toggle
        {
            get => toggle;
            private set => toggle = value;
        }

        public Label CharLabel
        {
            get => charLabel;
            private set => charLabel = value;
        }

        /*
        public string QuestionTooltipText
        {
            get => questionTooltipText;
            private set
            {
                questionTooltipText = value;
                QuestionTooltip.tooltip = value;
            }
        }
        */

        public VisualElement QuestionTooltip
        {
            get => questionTooltip;
            private set => questionTooltip = value;
        }

        //public int Index
        //{
        //    get => index;
        //    set 
        //    {
        //        if(index != value) 
        //            Debug.LogWarning("Element " + CharLabel.text + " changed index: " + index + " -> " + value);
        //        index = value;
        //    }
        //}

        public BundleWizardCharacteristicElement() : base()
        {
            GetVisualTreeForThis();

            toggle = this.Q<Toggle>();
            charLabel = this.Q<Label>();
            questionTooltip = this.Q<VisualElement>("QuestionTooltip");

            AddToClassList("lbs-wizard-char-element");
        }

        /// <summary>
        /// Registers <see cref="toggleCallback"/> to <see cref="Toggle"/>.
        /// </summary>
        public void EnableToggleCallback()
        {
            Toggle.RegisterValueChangedCallback(toggleCallback);
        }

        /// <summary>
        /// Unregisters <see cref="toggleCallback"/> from <see cref="Toggle"/>.
        /// </summary>
        public void DisableToggleCallback()
        {
            Toggle.UnregisterValueChangedCallback(toggleCallback);
        }

        public override bool Equals(object obj)
        {
            if (obj is not BundleWizardCharacteristicElement other) return false;
            //return index == other.index;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

