using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements.Editor
{
    /// <summary>
    /// A personalized version of the <b>LBS Custom Button</b> made for easily selectable buttons and tools.
    /// </summary>
    [UxmlElement]
    public partial class LBSSelectableButton : VisualElement
    {
        #region UXMLFACTORY
        [UxmlElementAttribute]
        public new class UxmlFactory { }
        #endregion

        #region VIEW ELEMENTS
        /// <summary>
        /// Stores the object's interactable button.
        /// </summary>
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
        /// <summary>
        /// References the data stored by the button. The data can be extracted and assigned appropriately when the button is selected.
        /// </summary>
        public int Data
        {
            get => data;
            set => data = value;
        }
        /// <summary>
        /// References the button's background color. Colors can be easily references and set.
        /// </summary>
        public Color ButtonColor
        {
            get => selectableButton.style.backgroundColor.value;
            set => selectableButton.SetBackgroundColor(value);
        }

        /// <summary>
        /// References whether the button is currently selected by its interface.
        /// </summary>
        public bool Selected => selected;
        #endregion

        #region EVENTS
        /// <summary>
        /// Called when the button is pressed.
        /// </summary>
        public Action OnExecute;
        /// <summary>
        /// Called when the button is attempted to be removed from its current hierarchy.
        /// </summary>
        public Action OnRemove;
        /// <summary>
        /// Called when the button is deselected.
        /// </summary>
        public Action OnButtonDeselected;
        /// <summary>
        /// Called when the button is selected.
        /// </summary>
        public Action OnButtonSelected;
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// An empty version of the main constructor. Simply initializes the object with a random color, as well as allowing it to be removable by default.
        /// </summary>
        public LBSSelectableButton() : this(UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f), true) { }
        /// <summary>
        /// The main constructor for the selectable button. It initializes the main button and any visual characteristics.
        /// </summary>
        /// <param name="_backgroundColor">The button's color.</param>
        /// <param name="removable">Defines if the button can be removed by right clicking it. Useful when the button is being initialized for
        /// personalizable lists of objects, like the <b>LBS Terrain Connection Grid Editor Window</b>'s color palettes.</param>
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
        /// <summary>
        /// Calls the execute action on the button when clicked.
        /// </summary>
        public void ButtonClicked()
        {
            OnExecute?.Invoke();
        }

        /// <summary>
        /// Calls the corresponding action for the button (whether it's selected or deselected) depending on its current selection status.
        /// </summary>
        /// <param name="check">True if the button is selected, false if the button is deselected.</param>
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