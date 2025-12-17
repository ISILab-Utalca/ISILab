using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager.BundleWizard
{
    [UxmlElement]
    public partial class BundleWizardCharacteristicElement : LBSComplexVisualElement
    {

        private Toggle toggle;
        private Label charLabel;

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

        public BundleWizardCharacteristicElement() : base()
        {
            GetVisualTreeForThis();

            toggle = this.Q<Toggle>();
            charLabel = this.Q<Label>();

        }
    }
}

