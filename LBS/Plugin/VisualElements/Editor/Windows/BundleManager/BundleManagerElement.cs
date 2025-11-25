using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Internal;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Bundles;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISI_Lab.LBS.Plugin.VisualElements.Editor.Windows.BundleManager
{
    
    [UxmlElement]
    public partial class BundleManagerElement : VisualElement, IBundleElement
    {
        // External references
        private Bundle _bundleRef;
        private BundleCollection _bundleCollectionRef;
        private ListView _listRef;
        private Button _deleteButton;
        
        // Internal references
        private readonly Label _bundleName;
        private readonly IMGUIContainer _bundleIcon;
        private readonly IMGUIContainer _mainIcon;
        private readonly IMGUIContainer _warningIcon;
        
        // Properties
        private bool _isMainBundle;

        public Bundle BundleRef { get => _bundleRef; set => _bundleRef = value; }
        public ListView ListRef { get => _listRef; set => _listRef = value; }

        public BundleManagerElement() : base()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("BundleManagerElement");
            visualTree.CloneTree(this);
            
            _bundleName = this.Q<Label>("BundleName");
            _bundleIcon = this.Q<IMGUIContainer>("BundleIcon");
            _mainIcon = this.Q<IMGUIContainer>("MasterIcon");
            _warningIcon = this.Q<IMGUIContainer>("WarningIcon");
            _deleteButton = this.Q<Button>("DeleteButton");
            
            this.AddToClassList("lbs-list-item");
            
            // Set DeleteBundle Button
            _deleteButton.clickable.clicked += () => DisplayDeleteBundleDialog();
        }
        
        public BundleManagerElement(int _size, int _gapSize): this()
        {
            // Set size for visualElements
            //this.style.height = size - gapSize;
            //this.Q<VisualElement>("DeleteButton").style.height = size - gapSize;
        }

        public void SetBundleReference(Bundle bundle, ListView list, bool mainBundle)
        {
            _bundleRef = bundle;
            _bundleCollectionRef = null;
            
            _bundleName.text = bundle == null ? "Empty Bundle" : _bundleRef.name;
            
            _listRef = list;
            _isMainBundle = mainBundle;

            if (bundle.Icon != null)
            {
                _bundleIcon.style.backgroundImage = new StyleBackground(bundle.Icon);   
            }
        }
        public void SetRefs(BundleCollection bundle, ListView list)
        {
            _bundleRef = null;
            _bundleCollectionRef = bundle;
            
            _bundleName.text = bundle == null ? "Empty Bundle Collection" : _bundleCollectionRef.name;
            
            _listRef = list;
            _isMainBundle = true;

            if (bundle.Icon != null)
            {
                _bundleIcon.style.backgroundImage = new StyleBackground(bundle.Icon);   
            }
        }

        private void RemoveFromList()
        {
            if (_isMainBundle)
            {
                foreach (BundleManagerWindow.BundleContainer item in _listRef.itemsSource)
                {
                    var bundle = item.GetMainBundle();
                    var collection = item.GetMainCollection();
                    
                    if ((bundle == null || !bundle.Equals(_bundleRef)) &&
                        (collection == null || !collection.Equals(_bundleCollectionRef))) continue;
                    
                    _listRef.itemsSource.Remove(item);
                    break;
                }
            }
            else
            {
                _listRef.itemsSource.Remove(_bundleRef);
                List<Bundle> parents = _bundleRef.Parents();
                foreach (Bundle p in parents)
                {
                    p.RemoveChild(_bundleRef);
                }
            }
            LBSAssetsStorage.Instance.RemoveElement(_bundleRef);
        }

        public void SetIconDisplay(Icons icon, bool display)
        {
            DisplayStyle displayStyle = display ? DisplayStyle.Flex : DisplayStyle.None; 
            
            switch (icon)
            {
                case Icons.Bundle:
                    _bundleIcon.style.display = displayStyle;
                    break;
                case Icons.Main:
                    _mainIcon.style.display = displayStyle;
                    break;
                case Icons.Warning:
                    _warningIcon.style.display = displayStyle;
                    break;
            }
        }

        public void SetIconDisplay(string iconName, bool display)
        {
            switch(iconName)
            {
                case "Bundle"   : SetIconDisplay(Icons.Bundle,  display); break;
                case "Main"     : SetIconDisplay(Icons.Main,    display); break;
                case "Warning"  : SetIconDisplay(Icons.Warning, display); break;
            }
        }

        public int DisplayDeleteBundleDialog()
        {
            int answer = EditorUtility.DisplayDialogComplex(
                "The bundle will be deleted",
                "The bundle: " + _bundleName.text + " will be deleted, are you sure you want to continue?",
                "Delete",
                "Cancel",
                "");
            switch (answer)
            {
                case 0: //Delete
                    string path = AssetDatabase.GetAssetPath(_bundleRef);
                    RemoveFromList();
                    Debug.Log(AssetDatabase.DeleteAsset(path)
                        ? "File at " + path + " successfully deleted"
                        : "File failed to delete");
                    _listRef.RefreshItems();
                    return 1;
                case 1: //Cancel
                    return 0;
            }
            return 0; // Do nothing
        }

        public enum Icons { Bundle, Main, Warning}
    }
}
