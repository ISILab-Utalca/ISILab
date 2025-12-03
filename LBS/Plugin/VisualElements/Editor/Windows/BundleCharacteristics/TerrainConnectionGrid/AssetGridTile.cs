using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using System;
using UnityEngine;
using UnityEngine.UIElements;

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
    private int colorValue;
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
    public Action OnValueUpdated;
    #endregion

    #region CONSTRUCTOR
    public AssetGridTile(int _gridPosition, int _colorValue = 0)
    {
        this.gridPosition = _gridPosition;
        this.colorValue = _colorValue;

        if (visualTree == null)
        {
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AssetGridTile");
        }
        visualTree.CloneTree(this);

        interactButton = this.Q<LBSCustomButton>("InteractButton");
        interactButton.RegisterCallback<ClickEvent>((evt) => OnTileClicked?.Invoke());

        colorMultiplier = this.Q<VisualElement>("ColorMultiplier");
        tileBorder = this.Q<VisualElement>("TileBorder");
        tileBorderHovered = this.Q<VisualElement>("TileBorderHovered");

        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        RegisterCallback<MouseEnterEvent>(OnMouseEnter);
    }
    #endregion

    #region METHODS
    public void OnMouseEnter(MouseEnterEvent evt)
    {
        if(canHighlight)
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
    #endregion
}
