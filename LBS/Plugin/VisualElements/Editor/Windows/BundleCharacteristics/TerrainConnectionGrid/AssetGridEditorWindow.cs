using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Bundles;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetGridEditorWindow : VisualElement
{
    #region FIELDS
    AssetConnectionGrid assetGrid;
    VisualElement gridContainer;
    #endregion

    #region PROPERTIES
    public AssetConnectionGrid AssetGrid => assetGrid;
    public VisualElement GridContainer => gridContainer;
    public Asset AssetReference => AssetGrid.AssetReference;
    #endregion

    #region EVENTS

    #endregion

    #region CONSTRUCTOR
    public AssetGridEditorWindow(AssetConnectionGrid grid)
    {
        assetGrid = grid;

        var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AssetGridEditorWindow");
        visualTree.CloneTree(this);

        gridContainer = this.Q<VisualElement>("GridContainer");
        SetGrid();
    }
    #endregion

    #region METHODS
    public void SetGrid()
    {
        
        int lngth = AssetGrid.TerrainFlag.Length;
        float _sqr = Mathf.Sqrt(lngth);
        if(_sqr - Mathf.RoundToInt(_sqr)!=0) { return; }

        //sqr is the length of the rows and columns alike, so we proceed
        for(int i=0; i<_sqr; i++)
        {
            var _row = new VisualElement();
            _row.style.flexDirection = FlexDirection.Row;
            _row.style.flexGrow = 1;
            gridContainer.Add(_row);
            
            for(int j=0; j<_sqr; j++)
            {
                var _tile = new AssetGridTile((j + (i*Mathf.RoundToInt(_sqr))));
                _tile.AddToClassList("asset-grid-tile");
                _tile.OnTileClicked += () => { UseToolOnTile(_tile); };
                _row.Add(_tile);
            }
        }
    }
    #endregion

    public void UseToolOnTile(AssetGridTile tile)
    {
        Debug.Log("using tile nº"+tile.GridPosition+", VALUE: "+tile.ColorValue);
        Debug.Log("terrain flag value: " + assetGrid.terrainFlag[tile.ColorValue]);
    }
}
