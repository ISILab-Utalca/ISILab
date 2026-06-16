using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleCharacteristics.TerrainConnectionGrid
{
    /// <summary>
    /// The tiles conforming the visual representation of <b>Asset Grids</b> for editing purposes. A square grid of them is initialized for each asset grid
    /// for easy terrain flag manipulation.
    /// </summary>
    public class AssetGridTile : VisualElement
    {
        static VisualTreeAsset visualTree;
        #region FIELDS
        //VEs
        LBSCustomButton interactButton;
        VisualElement colorMultiplier;
        VisualElement tileBorder;
        VisualElement tileBorderHovered;

        //Can this be selected?
        private bool canHighlight = true;
        private int gridPosition;
        private int colorValue = 0;
        #endregion

        #region PROPERTIES
        /// <summary>
        /// References the color multiplier currently applied to the tile. Colors are used to represent the current terrain flag applied to the tile.
        /// </summary>
        public VisualElement ColorMultiplier => colorMultiplier;
        /// <summary>
        /// References the tile's position in the grid from 0 to its limit to allow easy translation into the <b>Asset Grid</b>'s terrain flag array.
        /// </summary>
        public int GridPosition => gridPosition;
        /// <summary>
        /// The terrain flag currently stored by this particular tile. Referenced on generation.
        /// </summary>
        public int ColorValue => colorValue;
        #endregion

        #region EVENTS
        /// <summary>
        /// Used to notify the <b>Asset Grid Editor</b> that the tile has been clicked. Currently used to apply tools onto the tile.
        /// </summary>
        public Action OnTileClicked;
        /// <summary>
        /// Used to notify the <b>Asset Grid Editor</b> that the tile has been right clicked.
        /// </summary>
        public Action OnTileRightClicked;
        /// <summary>
        /// Notifies the terrain flag for the tile has been modified.
        /// </summary>
        public Action OnValueUpdated;
        /// <summary>
        /// Notifies the data in the tile is attempting to be saved, imprinting it onto the <b>Asset Grid</b>.
        /// </summary>
        public Action OnValueSaved;
        /// <summary>
        /// Notifies the data in the tile is attempting to be reverted, restored to its previous saved state.
        /// </summary>
        public Action OnValueReverted;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// The main constructor for assed grid tiles. It initializes every required functionality for the tile, as well as assign its position and terrain flag
        /// for easy manipulation.
        /// </summary>
        /// <param name="_gridPosition">The position in the <b>Asset Grid</b> the tile represents in its terrain flag array.</param>
        /// <param name="_colorValue">The current color applied to the tile according to its current terrain flag.</param>
        public AssetGridTile(int _gridPosition, int _colorValue = -1)
        {
            gridPosition = _gridPosition;
            colorValue = _colorValue;

            if (visualTree == null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AssetGridTile");
            }
            visualTree.CloneTree(this);

            interactButton = this.Q<LBSCustomButton>("InteractButton");
            interactButton.RegisterCallback<ClickEvent>((evt) => OnTileClicked?.Invoke());
            interactButton.RegisterCallback<MouseDownEvent>(OnMouseRightClick);

            colorMultiplier = this.Q<VisualElement>("ColorMultiplier");
            colorMultiplier.style.visibility = colorValue != 0 ? Visibility.Visible : Visibility.Hidden;

            tileBorder = this.Q<VisualElement>("TileBorder");
            tileBorderHovered = this.Q<VisualElement>("TileBorderHovered");

            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Called when the tile is hovered. It applies a blue overlay on it to notify it's currently hovered.
        /// </summary>
        public void OnMouseEnter(MouseEnterEvent evt)
        {
            if (canHighlight)
            {
                tileBorder.visible = false;
                tileBorderHovered.visible = true;
            }
        }
        /// <summary>
        /// Called when the tile stops being hovered. Removes the overlay border, if any.
        /// </summary>
        public void OnMouseLeave(MouseLeaveEvent evt)
        {
            tileBorder.visible = true;
            tileBorderHovered.visible = false;
        }

        /// <summary>
        /// Filters whether the tile was left or right clicked. The event can only track clicks in general, so a manual filter must be introduced.
        /// </summary>
        public void OnMouseRightClick(MouseDownEvent evt)
        {
            if(evt.button == 1)
            {
                OnTileRightClicked?.Invoke();
            }
        }

        /// <summary>
        /// Changes the terrain flag on the tile and updates its color accordingly.
        /// </summary>
        /// <param name="newValue">The new terrain flag to apply to the tile.</param>
        public void ChangeValue(int newValue)
        {
            if (newValue == colorValue) return;
            colorValue = newValue;
            colorMultiplier.style.visibility = colorValue != 0 ? Visibility.Visible : Visibility.Hidden;
            OnValueUpdated?.Invoke();
        }
        /// <summary>
        /// Updtes the color of the tile. Called when the value is updated.
        /// </summary>
        /// <param name="color">The new color to apply.</param>
        public void ChangeColor(Color color)
        {
            colorMultiplier.style.backgroundColor = color * new Color(1, 1, 1, 0.5f);
        }
        #endregion
    }
}

