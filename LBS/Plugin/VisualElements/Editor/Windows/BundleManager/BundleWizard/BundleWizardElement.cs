using ISILab.LBS.CustomComponents;
using LBS.Bundles;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISI_Lab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager
{
    [UxmlElement]
    public partial class BundleWizardElement : LBSComplexVisualElement, IBundleElement
    {
        // External references
        private Bundle _bundleRef;
        
        private ListView _listRef;

        private LBSCustomTextField _nameField;
        private LBSCustomButton _deleteButton;


        private readonly IMGUIContainer _bundleIcon;
        private readonly IMGUIContainer _selectIcon;

        public Bundle BundleRef { get => _bundleRef; set => _bundleRef = value; }
        public ListView ListRef { get => _listRef; set => _listRef = value; }

        //private readonly IMGUIContainer _warningIcon;

        public BundleWizardElement()
        {
            GetVisualTreeForThis();

            _bundleIcon = this.Q<IMGUIContainer>("BundleIcon");
            _selectIcon = this.Q<IMGUIContainer>("SelectIcon");

            _deleteButton = this.Q<LBSCustomButton>("DeleteButton");

            AddToClassList("lbs-list-item");


        }

        public void SetBundleReference(Bundle bundle, ListView list, bool _)
        {
            _bundleRef = bundle;

            _listRef = list;

            if(bundle.Icon != null)
            {
                _bundleIcon.style.backgroundImage = new StyleBackground(bundle.Icon);
            }
        }

        public void SetIconDisplay(string iconName, bool display)
        {
            DisplayStyle displayStyle = display ? DisplayStyle.Flex : DisplayStyle.None;

            switch (iconName)
            {
                case "Bundle":
                    _bundleIcon.style.display = displayStyle;
                    break;
                case "Select":
                    _bundleIcon.style.display = displayStyle;
                    break;
            }
        }
    }
}

