using System;
using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleCharacteristics.TerrainConnectionGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleCharacteristics
{
    public class AssetGridEditorWindow : VisualElement
    {
        #region FIELDS
        AssetConnectionGrid assetGrid;
        VisualElement gridContainer;
        #endregion

        #region VISUAL ELEMENTS
        VisualElement thumbnail;
        List<AssetGridTile> tiles = new List<AssetGridTile>();
        #endregion

        #region SQUARE PREVIEW ELEMENTS

        //Sourced from BundleDirectionEditorWindow
        private Texture2D renderTexture;
        private GameObject previewPrefab;
        private PreviewRenderUtility prevRenderUtil;
        private TerrainConnectionGridEditorWindow windowOwner;

        #endregion

        #region PROPERTIES
        public AssetConnectionGrid AssetGrid => assetGrid;
        public VisualElement GridContainer => gridContainer;
        public Asset AssetReference => AssetGrid.AssetReference;
        //For tool usage
        public TerrainConnectionGridEditorWindow WindowOwner => windowOwner;
        public float FOVScale => windowOwner.fovScale;
        public TerrainConnectionGridEditorWindow.GridTerrainTool ActiveTool => WindowOwner.ActiveTool;
        public int CurrentColorID => windowOwner.currentColor;
        public Dictionary<int, UnityEngine.Color> ColorPaletteKey => WindowOwner.ColorPaletteKey;
        public int GridLength => AssetGrid.TerrainFlag.Length;
        public int GridLengthSqr { get { return Mathf.RoundToInt(Mathf.Sqrt(GridLength)); } }

        #endregion

        #region EVENTS
        public Action OnRemove;
        public Action OnColorListModified;
        #endregion

        #region CONSTRUCTOR
        public AssetGridEditorWindow(AssetConnectionGrid grid, TerrainConnectionGridEditorWindow owner)
        {
            windowOwner = owner;
            assetGrid = grid;

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AssetGridEditorWindow");
            visualTree.CloneTree(this);

            gridContainer = this.Q<VisualElement>("GridContainer");
            thumbnail = this.Q<VisualElement>("Thumbnail");

            Init();
        }

        void Init()
        {
            //Setting preview...
            //Code is sourced from BundleDirectionEditorWindow. Let's see how much it can be translated from it
            renderTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
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
        
            SetGrid();

            OnRemove += prevRenderUtil.Cleanup;
            WindowOwner.OnColorRemoved += OnColorListModified;
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
                    int pos = (j + (i * Mathf.RoundToInt(_sqr)));
                    var _tile = new AssetGridTile(pos, assetGrid.TerrainFlag[pos]);
                    _tile.AddToClassList("asset-grid-tile");
                    _tile.OnTileClicked += () => { UseToolOnTile(_tile); };
                    _tile.OnValueUpdated += () => {
                        if(ColorPaletteKey.ContainsKey(_tile.ColorValue))
                        {
                            _tile.ChangeColor(ColorPaletteKey[_tile.ColorValue]);
                        }
                        else
                        {
                            _tile.ChangeValue(0);
                        }
                    };
                    _tile.OnValueSaved += () =>
                    {
                        assetGrid.TerrainFlag[pos] = _tile.ColorValue;
                    };
                    _tile.OnValueReverted += () =>
                    {
                        if (ColorPaletteKey.ContainsKey(assetGrid.TerrainFlag[pos]))
                        {
                            _tile.ChangeValue(assetGrid.TerrainFlag[pos]);
                        } else
                        {
                            //TODO: Work on a way to be able to revert the colors and such.
                            _tile.ChangeValue(0);
                        }
                    };

                    //I want to see if this will immediately erase the painted tiles of a color that has been removed
                    OnColorListModified += _tile.OnValueUpdated;

                    //Very rudimentary, but if it gets the job done...
                    _tile.OnValueUpdated?.Invoke();                

                    _row.Add(_tile);
                    tiles.Add(_tile);
                }
            }
        }
    
        public void UpdateFOVScale()
        {
            StepPreview();
        }

        #endregion

        public void UseToolOnTile(AssetGridTile tile)
        {
            switch(ActiveTool)
            {
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Brush:
                    BrushTool(tile);
                    break;
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Fill:
                    FillTool(tile);
                    break;
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Eraser:
                    EraserTool(tile);
                    break;
            }
        }
        public void SaveChanges()
        {
            foreach(AssetGridTile tile in tiles)
            {
                tile.OnValueSaved?.Invoke();
            }
        }
        public void RevertChanges()
        {
            foreach (AssetGridTile tile in tiles)
            {
                tile.OnValueReverted?.Invoke();
            }
        }

        public void BrushTool(AssetGridTile tile)
        {
            tile.ChangeValue(CurrentColorID);
        }
        public void EraserTool(AssetGridTile tile)
        {
            tile.ChangeValue(0);
        }
        public void FillTool(AssetGridTile tile)
        {
            var _oldColor = tile.ColorValue;
            tile.ChangeValue(CurrentColorID);
        
            //Now we propagate it by looking for anything with the same old color
        
            //right
            if((tile.GridPosition%GridLengthSqr + 1) < GridLengthSqr)
            {
                if (tiles[tile.GridPosition + 1].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition + 1]);
                }
            }
            //left
            if ((tile.GridPosition % GridLengthSqr) - 1 > -1)
            {
                if (tiles[tile.GridPosition - 1].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition - 1]);
                }
            }
            //up
            if ((tile.GridPosition) - GridLengthSqr > -1)
            {
                if (tiles[tile.GridPosition - GridLengthSqr].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition - GridLengthSqr]);
                }
            }
            //down
            if ((tile.GridPosition) + GridLengthSqr < GridLength)
            {
                if (tiles[tile.GridPosition + GridLengthSqr].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition + GridLengthSqr]);
                }
            }

        }

        public void ClearGrid()
        {
            foreach (AssetGridTile __tile in tiles)
            {
                __tile.ChangeValue(0);
            }
        }

        private void StepPreview()
        {
            prevRenderUtil.camera.backgroundColor = Color.red;

            prevRenderUtil.BeginStaticPreview(new Rect(0, 0, 256, 256));

            prevRenderUtil.camera.transform.position = new Vector3(0, 10, 0);
            prevRenderUtil.camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            prevRenderUtil.camera.orthographic = true;

            prevRenderUtil.camera.orthographicSize = FOVScale;
            prevRenderUtil.camera.nearClipPlane = 0.1f;
            prevRenderUtil.camera.farClipPlane = 100f;

            prevRenderUtil.lights[0].intensity = 1f;
            prevRenderUtil.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);

            prevRenderUtil.camera.Render();

            renderTexture = prevRenderUtil.EndStaticPreview();
            thumbnail.style.backgroundImage = new StyleBackground(renderTexture);
        }
    }
}
