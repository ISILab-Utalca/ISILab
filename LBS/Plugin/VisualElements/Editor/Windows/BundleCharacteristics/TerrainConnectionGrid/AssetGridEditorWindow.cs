using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.Bundles;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetGridEditorWindow : VisualElement
{
    #region FIELDS
    AssetConnectionGrid assetGrid;
    VisualElement gridContainer;
    #endregion

    #region VISUAL ELEMENTS
    VisualElement thumbnail;
    #endregion

    #region SQUARE PREVIEW ELEMENTS

    //Sourced from BundleDirectionEditorWindow
    private Texture2D renderTexture;
    private GameObject previewPrefab;
    private PreviewRenderUtility prevRenderUtil;

    private float fovScale;

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

        //Setting preview...
        //Code is sourced from BundleDirectionEditorWindow. Let's see how much it can be translated from it
        thumbnail = this.Q<VisualElement>("Thumbnail");
        renderTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        thumbnail.style.backgroundImage = new StyleBackground(renderTexture);

        prevRenderUtil = new PreviewRenderUtility();
        prevRenderUtil.cameraFieldOfView = 30f;

        //Use AssetReference.obj to refer to the prefab
        var _prefab = AssetReference.obj;
        if (_prefab != null)
        {
            previewPrefab = prevRenderUtil.InstantiatePrefabInScene(_prefab);
            previewPrefab.transform.position = Vector3.zero;
        }
        EditorApplication.delayCall += StepPreview;

        //SetGrid();
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
        Debug.Log("terrain flag value: " + assetGrid.TerrainFlag[tile.ColorValue]);
    }

    private void StepPreview()
    {
        prevRenderUtil.BeginStaticPreview(new Rect(0, 0, 512, 512));

        prevRenderUtil.camera.transform.position = new Vector3(0, 10, 0);
        prevRenderUtil.camera.transform.rotation = Quaternion.Euler(90, 0, 0);

        prevRenderUtil.camera.orthographic = true;

        prevRenderUtil.camera.orthographicSize = fovScale;
        prevRenderUtil.camera.nearClipPlane = 0.1f;
        prevRenderUtil.camera.farClipPlane = 100f;

        prevRenderUtil.lights[0].intensity = 1f;
        prevRenderUtil.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);

        prevRenderUtil.camera.Render();

        renderTexture = prevRenderUtil.EndStaticPreview();
        thumbnail.style.backgroundImage = new StyleBackground(renderTexture);
    }
}
