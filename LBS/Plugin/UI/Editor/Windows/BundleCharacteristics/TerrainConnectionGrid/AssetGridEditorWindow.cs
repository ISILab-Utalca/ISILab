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
    /// <summary>
    /// Asset Grid Editor Windows work as editors for the <b>Asset Grids</b> stored in <b>Terrain Connection Grid</b> characteristics. The editor
    /// creates a visual representation of the Asset Grid, as well as overlaying a square grid on top for easy modification of its terrain flag array.
    /// </summary>
    public class AssetGridEditorWindow : VisualElement
    {
        #region FIELDS
        AssetConnectionGrid assetGrid;
        VisualElement gridContainer;
        #endregion

        #region VISUAL ELEMENTS
        VisualElement thumbnail;
        VisualElement highlight;
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
        /// <summary>
        /// Points to the <b>Asset Grid</b> tied to this editor.
        /// </summary>
        public AssetConnectionGrid AssetGrid => assetGrid;
        /// <summary>
        /// Points to the container for this editor's terrain flag editor grid.
        /// </summary>
        public VisualElement GridContainer => gridContainer;
        /// <summary>
        /// References the asset tied to this <b>Asset Grid</b> for ease of 3D generation purposes.
        /// </summary>
        public Asset AssetReference => AssetGrid.AssetReference;
        //For tool usage
        /// <summary>
        /// Points to the main editor window for the <b>Terrain Connection Grid</b> characteristic.
        /// </summary>
        public TerrainConnectionGridEditorWindow WindowOwner => windowOwner;
        /// <summary>
        /// The FOV scale for the window's asset preview.
        /// </summary>
        public float FOVScale => windowOwner.fovScale;
        /// <summary>
        /// References the current active tool in the <b>Terrain Connection Grid</b> editor window for ease of modification.
        /// </summary>
        public TerrainConnectionGridEditorWindow.GridTerrainTool ActiveTool => WindowOwner.ActiveTool;
        /// <summary>
        /// References the current active color key in the <b>Terrain Connection Grid</b> editor window.
        /// </summary>
        public int CurrentColorID => windowOwner.currentColor;
        /// <summary>
        /// References the <b>Terrain Connection Grid</b>'s color key palette.
        /// </summary>
        public Dictionary<int, UnityEngine.Color> ColorPaletteKey => WindowOwner.ColorPaletteKey;
        /// <summary>
        /// References the length of the <b>Asset Grid</b> to generate the terrain flag grid.
        /// </summary>
        public int GridLength => AssetGrid.TerrainFlag.Length;
        /// <summary>
        /// The square root of the <b>Asset Grid</b>'s length. Defines the length of the grid's borders.
        /// </summary>
        public int GridLengthSqr { get { return Mathf.RoundToInt(Mathf.Sqrt(GridLength)); } }

        #endregion

        #region EVENTS
        /// <summary>
        /// Called when the object is removed from the hierarchy it lives in. Cleans the render preview to avoid memory leaks.
        /// </summary>
        public Action OnRemove;
        /// <summary>
        /// Called when the color list from the <b>Terrain Connection Grid Editor</b> is modified. If any recently removed terrain flag coincides with terrain flags
        /// present in this <b>Asset Grid</b>, they will automatically be turned back to their default state.
        /// </summary>
        public Action OnColorListModified;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Basic constructor. Initializes the editor's base characteristics.
        /// </summary>
        /// <param name="grid">The <b>Asset Grid</b> to associate to this editor window.</param>
        /// <param name="owner">The <b>Terrain Connection Grid</b> editor window currently creating this visual element (ideally).</param>
        public AssetGridEditorWindow(AssetConnectionGrid grid, TerrainConnectionGridEditorWindow owner)
        {
            windowOwner = owner;
            assetGrid = grid;

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AssetGridEditorWindow");
            visualTree.CloneTree(this);

            gridContainer = this.Q<VisualElement>("GridContainer");
            thumbnail = this.Q<VisualElement>("Thumbnail");
            highlight = this.Q<VisualElement>("Highlight");

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

        /// <summary>
        /// Sets up the proper <b>Asset Grid</b> editor by overlaying a square grid of modifiable panels linked to the object's terrain flag list. </br>
        /// It individually adds each modifiable square in sequence while automatically setting up their modifiable characteristics, as well as how they
        /// interact with tools and what happens if the color palette is modified.
        /// </summary>
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
                    _tile.OnTileClicked += () => { UseToolOnTile(_tile, false); };
                    _tile.OnTileRightClicked += () => { UseToolOnTile(_tile, true); };
                    _tile.OnValueUpdated += () => {
                        if(ColorPaletteKey.ContainsKey(_tile.ColorValue))
                        {
                            _tile.ChangeColor(ColorPaletteKey[_tile.ColorValue]);
                        }
                        //Border
                        else if (_tile.ColorValue == -1)
                        {
                            _tile.ChangeColor(new Color(0.8f, 0.8f, 0.8f));
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
                        } else if (assetGrid.TerrainFlag[pos] == -1)
                        {
                            _tile.ChangeValue(-1);
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
    
        /// <summary>
        /// Updates the <b>Asset Grid</b> editor's asset preview's scale, making the object closer or further away from the preview square.
        /// </summary>
        public void UpdateFOVScale()
        {
            StepPreview();
        }

        /// <summary>
        /// Uses the currently selected tool in the <b>Terrain Connection Grid</b> editor on the selected tile.
        /// </summary>
        /// <param name="tile">The tile to work on.</param>
        /// <param name="rightClick">Check if right click was used to select the tile.</param>
        public void UseToolOnTile(AssetGridTile tile, bool rightClick)
        {
            switch(ActiveTool)
            {
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Brush:
                    BrushTool(tile, rightClick);
                    break;
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Fill:
                    FillTool(tile, rightClick);
                    break;
                case TerrainConnectionGridEditorWindow.GridTerrainTool.Eraser:
                    EraserTool(tile, rightClick);
                    break;
            }
        }

        /// <summary>
        /// Saves any changes made to the <b>Asset Grid</b>. Changes are not saved automatically and the Asset Grid can only be modified with this function.
        /// </summary>
        public void SaveChanges()
        {
            foreach(AssetGridTile tile in tiles)
            {
                tile.OnValueSaved?.Invoke();
            }
        }

        /// <summary>
        /// Cleans up any unsaved changes done to the <b>Asset Grid</b> in the editor, reverting it back to its saved state.
        /// </summary>
        public void RevertChanges()
        {
            foreach (AssetGridTile tile in tiles)
            {
                tile.OnValueReverted?.Invoke();
            }
        }

        /// <summary>
        /// Uses the <b>Brush</b> tool on the selected tile, painting it with the currently selected color. If right clicked, it'll erase the current flag instead.
        /// </summary>
        /// <param name="tile">The tile to modify.</param>
        /// <param name="rightClick">Check if right click was used to select the tile.</param>
        public void BrushTool(AssetGridTile tile, bool rightClick)
        {
            tile.ChangeValue(rightClick ? 0 : CurrentColorID);
        }
        /// <summary>
        /// Uses the <b>Eraser</b> tool on the selected tile, erasing its current terrain flag and replacing it with a default (0).
        /// </summary>
        /// <param name="tile">The tile to modify.</param>
        /// <param name="rightClick">Check if right click was used to select the tile.</param>
        public void EraserTool(AssetGridTile tile, bool rightClick)
        {
            tile.ChangeValue(0);
        }
        /// <summary>
        /// Uses the <b>Fill</b> tool on the selected tile. It paints the selected tile with the currently selected color (or the default if right clicked),
        /// then checks nearby tiles for the modified tile's original color. The fill tool will then recurse itself to propagate in any tiles with the same 
        /// original color.
        /// </summary>
        /// <param name="tile">The tile to modify.</param>
        /// <param name="rightClick">Check if right click was used to select the tile.</param>
        public void FillTool(AssetGridTile tile, bool rightClick)
        {
            var _oldColor = tile.ColorValue;
            if (_oldColor == (rightClick ? 0 : CurrentColorID)) return;

            tile.ChangeValue(rightClick ? 0 : CurrentColorID);
        
            //Now we propagate it by looking for anything with the same old color
        
            //right
            if((tile.GridPosition%GridLengthSqr + 1) < GridLengthSqr)
            {
                if (tiles[tile.GridPosition + 1].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition + 1], rightClick);
                }
            }
            //left
            if ((tile.GridPosition % GridLengthSqr) - 1 > -1)
            {
                if (tiles[tile.GridPosition - 1].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition - 1], rightClick);
                }
            }
            //up
            if ((tile.GridPosition) - GridLengthSqr > -1)
            {
                if (tiles[tile.GridPosition - GridLengthSqr].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition - GridLengthSqr], rightClick);
                }
            }
            //down
            if ((tile.GridPosition) + GridLengthSqr < GridLength)
            {
                if (tiles[tile.GridPosition + GridLengthSqr].ColorValue == _oldColor)
                {
                    FillTool(tiles[tile.GridPosition + GridLengthSqr], rightClick);
                }
            }

        }

        /// <summary>
        /// Completely returns the grid back to default values.
        /// </summary>
        public void ClearGrid()
        {
            foreach (AssetGridTile __tile in tiles)
            {
                __tile.ChangeValue(0);
            }
        }
        /// <summary>
        /// Makes the visual highlight for the default asset visible on the asset grid if it's currently the selected default asset.
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleHighlight(bool toggle)
        {
            highlight.visible = toggle;
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
        #endregion
    }
}
