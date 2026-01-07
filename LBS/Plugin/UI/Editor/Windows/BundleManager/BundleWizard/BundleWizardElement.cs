using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using System.Drawing;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager.BundleWizard
{
    /// <summary>
    /// <b>For <see cref="BundleWizardPopup"/> exclusive use.</b><br />
    /// Visual element representing a child <see cref="Bundle"/> that can be renamed and selected or deselected.
    /// </summary>
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
        public int Index { get; set; }

        //private readonly IMGUIContainer _warningIcon;

        private readonly VectorImage _addIcon;
        private readonly UnityEngine.Color _addIconColor = new UnityEngine.Color32(118, 151, 67, 255);
        private readonly VectorImage _thrashIcon;

        private System.Action _currentCallback;

        public BundleWizardElement()
        {
            _addIcon = AssetDatabase.LoadAssetAtPath<VectorImage>(AssetDatabase.GUIDToAssetPath("ad8d08a3d465b3a438016713ae2f99c5"));

            GetVisualTreeForThis();

            _bundleIcon = this.Q<IMGUIContainer>("BundleIcon");
            _selectIcon = this.Q<IMGUIContainer>("SelectIcon");

            _nameField = this.Q<LBSCustomTextField>("NameField");
            _nameField.RegisterCallback<BlurEvent>(e =>
            {
                _nameField.value = _nameField.value.Replace(' ', '_');
                BundleRef.BundleName = _nameField.value;
                //Debug.Log($"{_listRef.itemsSource[Index]} {BundleRef.BundleName}");
            });

            _deleteButton = this.Q<LBSCustomButton>("DeleteButton");

            AddToClassList("lbs-list-item");

            EditorApplication.delayCall += () =>
            {
                _nameField.Focus();
                EditorApplication.delayCall += () =>
                {
                    _nameField.SelectAll();
                };
            };
        }

        public void SetBundleReference(Bundle bundle, ListView list, bool _)
        {
            _bundleRef = bundle;
            _nameField.SetValueWithoutNotify(bundle.BundleName);

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
                    _selectIcon.style.display = displayStyle;
                    break;
            }
        }

        public void SetRemoveCallback(System.Action removeCallback)
        {
            if (_currentCallback != null)
                this._deleteButton.clicked -= _currentCallback;

            _currentCallback = removeCallback;
            _deleteButton.clicked += _currentCallback;
        }

        public void RemoveDeleteIcon()
        {
            _deleteButton.SetBackgroundColor(_addIconColor);
            _deleteButton.iconImage = Background.FromVectorImage(_addIcon);
            //_deleteButton.visible = false;
        }
    }
}

