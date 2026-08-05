using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
using System;
using System.Drawing;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

namespace LBS.VisualElements
{
    [UxmlElement]
    public partial class SimplePallete : VisualElement
    {
        #region DATA FIELDS

        private OptionView[] optionViews;
        private object[] options;
        private object selected;
        private object collectionSelected;


        #endregion

        #region UI VISUAL ELEMENTS REFERENCES 
        private string nameLabel = "";
        private VectorImage image;
        private bool displayAddElement = true;
        private bool displayRemoveElement = true;
        private bool showNoElement = true;

        private Label nameLabelElement;
        private LBSCustomImage icon;
        private Button noElement;
        private VisualElement toolBar;
        private LBSCustomButton addButton;
        private LBSCustomButton removeButton;

        private new readonly VisualElement contentContainer;
        private static VisualTreeAsset visualTree;
        #endregion

        #region EVENTS
        //public event Action<ChangeEvent<string>> OnChangeGroup;
        public event Action<object> OnSelectOption;
        public event Action<object> OnRemoveOption;
        public event Action OnAddOption;
        public event Action OnRepaint;
        public event Func<object,string> OnSetTooltip;
        
        private Action<OptionView, object> onSetView;

        #endregion

        #region PROPERTIES


        [UxmlAttribute]
        public VectorImage Icon
        {
            get => image;
            set
            {
                image = value;
                if (icon != null)
                {
                    if (image != null)
                    {
                        icon.LBSImage = null;
                        icon.style.backgroundImage = new StyleBackground(image);
                    }

                    icon.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        [UxmlAttribute]
        public bool DisplayAddElement
        {
            get => displayAddElement;
            set
            {
                displayAddElement = value;
                if (addButton != null)
                {
                    addButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }
        
        
        [UxmlAttribute]
        public bool DisplayRemoveElement
        {
            get => displayRemoveElement;
            set
            {
                displayRemoveElement = value;
                if (removeButton != null)
                {
                    removeButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }


        [UxmlAttribute]
        public string NameLabel
        {
            get => nameLabel;
            set
            {
                nameLabel = value;
                if (nameLabelElement != null)
                {
                    nameLabelElement.style.display = !string.IsNullOrEmpty(value) ? DisplayStyle.Flex : DisplayStyle.None;
                    nameLabelElement.text = nameLabel;
                }
            }
        }

        [UxmlAttribute]
        public bool DisplayNoElement
        {
            get => ShowNoElement;
            set
            {
                ShowNoElement = (value);
            }
        }


        public object Selected
        {
            get => selected;
            set => selected = value;
        }

        public object CollectionSelected
        {
            get => collectionSelected;
            set => collectionSelected = value;
        }
        
        public object[] Options
        {
            get => options;
            set => options = value;
        }

        public bool ShowRemoveButton
        {
            set => removeButton.SetDisplay(value);
        }

        public bool ShowAddButton
        {
            set => addButton.SetDisplay(value);
        }
        
        public bool ShowNoElement
        {
            get => showNoElement;
            set
            {
                showNoElement = value;
                noElement.SetDisplay(showNoElement);
            }
        }

  

        #endregion

        #region CONSTRUCTORS
        public SimplePallete()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("SimplePallete");
            visualTree.CloneTree(this);
            AddToClassList("lbs-simple-palette");

            // toolbar
            toolBar = this.Q<VisualElement>("PaletteToolbar");
            // toolbar header
            nameLabelElement = this.Q<LBSCustomLabel>("MainLabel");
            icon = this.Q<LBSCustomImage>();
            // AddButton
            addButton = this.Q<LBSCustomButton>("AddButton");
            addButton.clicked += () => OnAddOption?.Invoke();
            // removeButton
            removeButton = this.Q<LBSCustomButton>("DeleteButton");
            removeButton.clicked += () => OnRemoveOption?.Invoke(selected);

            // Contents

            // Content
            contentContainer = this.Q<VisualElement>("Content");
            // NoElement
            noElement = this.Q<Button>("NoElement");

        }
        #endregion

        #region METHODS
        private void OnInternalSelectOption(object obj)
        {
            foreach (var optV in optionViews)
            {
                if(optV.Target != obj) { 
                optV.SetSelected(false);
                }
            }
            selected = obj;
            OnSelectOption?.Invoke(obj);
        }

        private void OnInternalRemoveOption(object obj)
        {
            foreach (var optV in optionViews)
            {
                optV.SetSelected(false);
            }
            selected = obj;
            OnRemoveOption?.Invoke(obj);
        }

        public void SetOptions(object[] options, Action<OptionView, object> onSetView)
        {
            this.options = options;
            this.onSetView = onSetView;
        }

        public void DisplayToolbar(bool show)
        {
            if (show) toolBar.style.display = DisplayStyle.Flex;
            else toolBar.style.display = DisplayStyle.None;
        }

        public void DisplayContent(bool show)
        {
            if (show) contentContainer.style.display = DisplayStyle.Flex;
            else contentContainer.style.display = DisplayStyle.None;
        }
        
        public void Repaint()
        {
            MarkDirtyRepaint();
            
            OnRepaint?.Invoke();
            contentContainer.Clear();

            if (options.Any())
            {
                optionViews = new OptionView[options.Length];

                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    var view = new OptionView(option, OnInternalSelectOption, OnRemoveOption, onSetView);
                    view.tooltip = OnSetTooltip?.Invoke(option);
                    optionViews[i] = view;
                    contentContainer.Add(view);
                }
            }
            else
            {
                if (displayAddElement)
                {
                    contentContainer.Add(noElement);
                }
            }

            if (selected == null) return;
            var ov = optionViews?.ToList().Find(o 
                => o != null && o.target != null && selected != null && o.target.Equals(selected));

            ov?.SetSelected(true);
        }
        #endregion
    }

}