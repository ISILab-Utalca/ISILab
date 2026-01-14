using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements.Editor
{
    [UxmlElement]
    public partial class LBSSelectableButton : VisualElement
    {
        #region UXMLFACTORY
        [UxmlElementAttribute]
        public new class UxmlFactory { }
        #endregion

        #region VIEW ELEMENTS
        public Button selectableButton;
        private VisualElement selector;
        #endregion

        #region FIELDS
        // stored data (for colors, it should be an ID that the interface will relate to a color!)
        private int data;
        //Can this be selected?
        private bool canHighlight;
        private bool selected;
        private bool removable;
        #endregion

        #region PROPERTIES
        public int Data
        {
            get => data;
            set => data = value;
        }

        public Color ButtonColor
        {
            get => selectableButton.style.backgroundColor.value;
            set => selectableButton.SetBackgroundColor(value);
        }

        public bool Selected => selected;
        #endregion

        #region EVENTS
        public Action OnExecute;
        public Action OnRemove;
        //Checks if the button is selected or not.
        public Action OnButtonDeselected;
        public Action OnButtonSelected;
        #endregion

        #region CONSTRUCTORS
        public LBSSelectableButton() : this(UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f), true) { }
        public LBSSelectableButton(Color32 _backgroundColor, bool removable)
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSSelectableButton");
            visualTree.CloneTree(this);

            selectableButton = this.Q<Button>("ColoredButton");
            selectableButton.clicked += ButtonClicked;
            selectableButton.SetBackgroundColor(_backgroundColor);

            selector = this.Q<VisualElement>("Selector");

            //Right click stuff
            if (removable)
            {
                ContextualMenuManipulator m = new ContextualMenuManipulator(RemoveButtonOption);
                m.target = this;
            }
            //Decoratives to check if the button is highlighted or not
            OnButtonDeselected += () =>
            {
                selected = false;
                selector.visible = false;
            };

            OnButtonSelected += () =>
            {
                selected = true;
                selector.visible = true;
            };
        }

        #endregion
        void RemoveButtonOption(ContextualMenuPopulateEvent evt)
        {
            // Remove this
            evt.menu.AppendAction("Remove", action =>
            {
                OnRemove?.Invoke();
                RemoveFromHierarchy();
            }
            );
        }

        public void ButtonClicked()
        {
            OnExecute?.Invoke();
        }

        public void ToggleButtonSelected(bool check)
        {
            switch(check)
            {
                case true: OnButtonSelected?.Invoke(); break;
                case false: OnButtonDeselected?.Invoke(); break;
            }
        }
    }
}