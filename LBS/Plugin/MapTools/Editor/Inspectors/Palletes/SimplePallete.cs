using System;
using System.Linq;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.CustomComponents;
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

        private string nameLabel = ""; 
        #endregion

        
        #region UI VISUAL ELEMENTS REFERENCES 
        
        private bool displayAddElement = true;
        private bool displayRemoveElement = true;
        
        private VisualElement icon;
        private Label nameLabelElement;
        private Button noElement;
        private LBSToolbarButton addButton;
        private LBSToolbarButton removeButton;
        private LBSCustomDropdown dropdownGroup;
        private readonly VisualElement contentContainer;
        
        #endregion

        #region EVENTS
        public event Action<ChangeEvent<string>> OnChangeGroup;
        public event Action<object> OnSelectOption;
        public event Action<object> OnRemoveOption;
        public event Action OnAddOption;
        public event Action OnRepaint;
        public event Func<object,string> OnSetTooltip;
        
        private Action<OptionView, object> onSetView;
        #endregion

        #region PROPERTIES
        
        
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
                    nameLabelElement.style.display = value != "" ? DisplayStyle.Flex : DisplayStyle.None;
                    nameLabelElement.text = nameLabel;
                }
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

        public bool ShowGroups
        {
            set => dropdownGroup.SetDisplay(value);
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
            set => noElement.SetDisplay(value);
        }
        
        public bool ShowDropdown
        {
            set => dropdownGroup.SetDisplay(value);
        }
        
        #endregion
        
        #region CONSTRUCTORS
        public SimplePallete()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("SimplePallete");
            visualTree.CloneTree(this);
            this.AddToClassList("lbs-simple-palette");

            // Content
            contentContainer = this.Q<VisualElement>("Content");
            contentContainer.style.flexDirection = FlexDirection.Row;         // Horizontal layout
            contentContainer.style.justifyContent = Justify.FlexStart;     // Space items evenly
            contentContainer.style.alignItems = Align.Stretch;                 // Vertically center them
            
            // Change Group
            dropdownGroup = this.Q<LBSCustomDropdown>("DropdownGroup");
            dropdownGroup.RegisterCallback<ChangeEvent<string>>(evt => OnChangeGroup?.Invoke(evt));
            nameLabelElement = this.Q<LBSCustomLabel>("MainLabel");

            // AddButton
            addButton = this.Q<LBSToolbarButton>("AddButton");
            addButton.clicked += () => OnAddOption?.Invoke();
            // removeButton
            removeButton = this.Q<LBSToolbarButton>("DeleteButton");
            removeButton.clicked += () => OnRemoveOption?.Invoke(selected);

            // NoElement
            noElement = this.Q<Button>("NoElement");

            // Icon
            //icon = this.Q<VisualElement>("IconPallete");

        }
        #endregion

        #region METHODS
        private void OnInternalSelectOption(object obj)
        {
            foreach (var optV in optionViews)
            {
                optV.SetSelected(false);
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
        
        public void SetIcon(VectorImage icon, Color color)
        {
            dropdownGroup.IconImage = icon;
            dropdownGroup.IconColor = color;
        }
        
        public void SetName(string name)
        {
            
            dropdownGroup.label = name;
            dropdownGroup.style.display = name == "" ? DisplayStyle.None : DisplayStyle.Flex;
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