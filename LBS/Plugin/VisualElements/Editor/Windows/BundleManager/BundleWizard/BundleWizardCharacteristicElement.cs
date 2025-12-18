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

        }

        public void EnableToggleCallback()
        {
            Toggle.RegisterValueChangedCallback(toggleCallback);
        }

        public void DisableToggleCallback()
        {
            Toggle.UnregisterValueChangedCallback(toggleCallback);
        }

        //public override bool Equals(object obj)
        //{
        //    if (obj is not BundleWizardCharacteristicElement other) return false;
        //    return index == other.index;
        //}
    }
}

