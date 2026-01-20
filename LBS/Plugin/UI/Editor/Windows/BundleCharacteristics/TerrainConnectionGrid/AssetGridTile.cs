using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleCharacteristics.TerrainConnectionGrid
{
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
        public VisualElement ColorMultiplier => colorMultiplier;
        //Represents its position on the grid from 0 to its limit
        public int GridPosition => gridPosition;
        //Represents the color value it stores
        public int ColorValue => colorValue;
        #endregion

        #region EVENTS
        //Should change according to the tool
        public Action OnTileClicked;
        public Action OnTileRightClicked;
        public Action OnValueUpdated;
        public Action OnValueSaved;
        public Action OnValueReverted;
        #endregion

        #region CONSTRUCTOR
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
        public void OnMouseEnter(MouseEnterEvent evt)
        {
            if (canHighlight)
            {
                tileBorder.visible = false;
                tileBorderHovered.visible = true;
            }
        }

        public void OnMouseLeave(MouseLeaveEvent evt)
        {
            tileBorder.visible = true;
            tileBorderHovered.visible = false;
        }

        public void OnMouseRightClick(MouseDownEvent evt)
        {
            if(evt.button == 1)
            {
                OnTileRightClicked?.Invoke();
            }
        }

        public void ChangeValue(int newValue)
        {
            if (newValue == colorValue) return;
            colorValue = newValue;
            colorMultiplier.style.visibility = colorValue != 0 ? Visibility.Visible : Visibility.Hidden;
            OnValueUpdated?.Invoke();
        }
        public void ChangeColor(Color color)
        {
            colorMultiplier.style.backgroundColor = color * new Color(1, 1, 1, 0.5f);
        }
        #endregion
    }
}

