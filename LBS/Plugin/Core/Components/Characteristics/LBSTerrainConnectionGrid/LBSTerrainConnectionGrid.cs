using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using ISILab.Extensions;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine.UIElements;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// The main class handling the 'Terrain Connection Grid' sorting characteristic. <br/> 
    /// A Connection Grid allows its respective bundle to flag their assets' 
    /// possible connections individually, allowing consistent pattern generation.
    /// </summary>
    [System.Serializable]
    public class LBSTerrainConnectionGrid : LBSCharacteristic, ICloneable
    {
        /// <summary>
        /// A list of all 'Asset-Connection Grids' stored in this characteristic.
        /// </summary>
        [SerializeField, JsonRequired]
        List<AssetConnectionGrid> gridList = new List<AssetConnectionGrid>();
        /// <summary>
        /// A list of every color handled in this characteristic's color palette.
        /// </summary>
        [SerializeField, JsonRequired]
        List<UnityEngine.Color> colorPalette = new List<UnityEngine.Color>();
        /// <summary>
        /// A list storing the ID of every color handled in this characteristic's color palette. 
        /// </summary>
        [SerializeField, JsonRequired]
        List<int> colorPaletteID = new List<int>();
        /// <summary>
        /// The size of the 'Asset-Connection Grid' handled for each asset in the bundle. <br/>
        /// <b>NOTE</b>: Currently, grids have a locked size of 9, which cannot be manually modified. It currently does not work with different sizes.
        /// </summary>
        [SerializeField, JsonRequired]
        int gridSize = 9;
        /// <summary>
        /// The ID of the characteristic's default asset. This asset will be generated when no legal connections are found in a particular tile.
        /// </summary>
        [SerializeField, JsonRequired]
        int defaultAsset = 0;

        #region PROPERTIES
        /// <summary>
        /// Points to the current assets stored in this characteristic's bundle. This list is used to assign each asset a corresponding blank 'Asset-Connection Grid'
        /// on generation of the characteristic.
        /// </summary>
        [JsonIgnore]
        public List<Asset> Assets
        {
            get => Owner.Assets;
        }
        /// <summary>
        /// Pointer to the 'Asset-Connection Grid' list.
        /// </summary>
        [JsonIgnore]
        public List<AssetConnectionGrid> GridList => gridList;
        /// <summary>
        /// Pointer to the 'Asset-Connection Grid' size variable.
        /// </summary>
        public int GridSize => gridSize;
        /// <summary>
        /// Pointer to the characteristic's color palette.
        /// </summary>
        [JsonIgnore]
        public List<UnityEngine.Color> ColorPalette => colorPalette;
        /// <summary>
        /// Pointer to the characteristic's color palette ID list.
        /// </summary>
        public List<int> ColorPaletteID => colorPaletteID;
        /// <summary>
        /// Pointer to the characteristic's default asset to generate.
        /// </summary>
        public int DefaultAsset
        {
            get => defaultAsset;
            set => defaultAsset = Mathf.Clamp(value, 0, gridList.Count - 1);
        }
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Empty constructor. Necessary for the manipulation of the characteristic in Visual Elements.
        /// </summary>
        public LBSTerrainConnectionGrid() : base()
        {
        }
        /// <summary>
        /// Alternative constructor with modifiable grid size.
        /// <b>NOTE</b>: Unused. Only the empty constructor is used.
        /// </summary>
        public LBSTerrainConnectionGrid(int gSize = 9)
        {
            gridSize = gSize;
        }
        #endregion
        
        #region METHODS - COLORS
        /// <summary>
        /// Adds a new color ID to the grid's color palette. It is simultaneously added to both the color palette and the color ID palette.
        /// </summary>
        /// <param name="id">The ID of the new color.</param>
        /// <param name="color">The new color assigned.</param>
        public void AddColor(int id, UnityEngine.Color color)
        {
            if (ColorExists(id)) return;
            colorPalette.Add(color);
            colorPaletteID.Add(id);
        }
        /// <summary>
        /// Removes a color ID. It uses the provided ID to simultaneously remove it from the color ID palette and the color palette.
        /// </summary>
        /// <param name="id">The ID of the color to remove.</param>
        public void RemoveColor(int id)
        {
            if (!ColorExists(id)) return;
            colorPalette.Remove(FindColor(id));
            colorPaletteID.Remove(id);
        }
        /// <summary>
        /// Checks if a color exists on the provided ID.
        /// </summary>
        /// <param name="id">The ID to search.</param>
        /// <returns><c>true</c> if the the ID succesfully returns a color in the color ID palette. <c>false</c> otherwise.</returns>
        public bool ColorExists(int id)
        {
            return colorPaletteID.Any(c => c == id);
        }
        /// <summary>
        /// Returns a color in the color palette as per a provided ID.
        /// </summary>
        /// <param name="id">The ID to search.</param>
        /// <returns>The color in the same slot as the searched color ID. Returns <c>null</c> if it doesn't exist.</returns>
        public UnityEngine.Color FindColor(int id)
        {
            return ColorPalette[colorPaletteID.IndexOf(id)];
        }

        #endregion

        #region METHODS - GRIDS
        /// <summary>
        /// Sets the individual ID of every asset in the bundle if it doesn't exist. This allows the 'Terrain Connection Grid' Editor to properly differentiate
        /// between different iterations of the exact same asset within the bundle without modifications required.
        /// </summary>
        public void Init()
        {
            foreach (Asset asset in Assets)
            {
                if (asset.id == null) asset.SetID();
            }
        }
        /// <summary>
        /// Obtains a particular 'Asset-Connection Grid' by looking for its asset reference.
        /// </summary>
        /// <param name="asset">The asset to be searched.</param>
        /// <returns>Returns the first Asset-Connection Grid that contains the given asset. Returns <c>null</c> otherwise.</returns>
        public AssetConnectionGrid GetGrid(Asset asset)
        {
            var match = gridList.Find(c => c.AssetReference.Equals(asset));
            return match;
        }
        /// <summary>
        /// Obtains multiple 'Asset-Connection Grids' by looking for a particular asset reference.
        /// </summary>
        /// <param name="asset">The asset to be searched.</param>
        /// <returns>Returns a list of every Asset-Connection Grid that contains the given asset. The list may be empty.</returns>
        public List<AssetConnectionGrid> GetGrids(Asset asset)
        {
            return gridList.FindAll(c => c.AssetReference.Equals(asset));
        }
        /// <summary>
        /// Obtains a particular 'Asset-Connection Grid' by looking for a GameObject. The object is initially searched on the bundle's asset list.
        /// </summary>
        /// <param name="obj">The GameObject to find.</param>
        /// <returns>Returns the first Asset-Connection Grid that contains the given GameObject (inside of an asset). Returns <c>null</c> otherwise.</returns>
        public AssetConnectionGrid GetGrid(GameObject obj) => GetGrid(Assets.Find(c => c.obj == obj));

        public void SetGridSize(int gSize)
        {
            gridSize = gSize;
            foreach (AssetConnectionGrid grid in gridList)
            {
                grid.TerrainFlag = new int[gSize];
            }
        }
        /// <summary>
        /// Creates a new Grid List for the characteristic, then automatically populates it according to the bundle's Asset list. <br/>
        /// To populate the Grid List, it individally checks every available Asset, then checks if an 'Asset-Connection Grid' exists for it. If multiple
        /// grids exist for the same asset, these are chequed in sequence and added accordingly. If no grid is found for a particular asset, 
        /// a blank grid is created.
        /// </summary>
        public void UpdateGridList()
        {
            if (gridList == null) gridList = new List<AssetConnectionGrid>();
            var updatedGridList = new List<AssetConnectionGrid>();
            foreach (Asset asset in Assets)
            {
                var existingAssets = GetGrids(asset);
                AssetConnectionGrid verifiedGrid = null;
                foreach (AssetConnectionGrid grid in existingAssets)
                {
                    //Ignore asset already added
                    if (updatedGridList.Contains(grid))
                    {
                        continue;
                    }
                    else
                    {
                        verifiedGrid = grid;
                        break;
                    }
                }
                if (verifiedGrid == null)
                {
                    asset.SetID();
                    verifiedGrid = new AssetConnectionGrid(gridSize, asset);
                }
                updatedGridList.Add(verifiedGrid);
            }
            gridList = updatedGridList;
        }

        #endregion

        #region METHODS - OTHER
        /// <summary>
        /// Unimplemented.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override object Clone()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Compares two 'Terrain Connection Grids'.
        /// </summary>
        /// <param name="obj">The object to compare to this.</param>
        /// <returns><c>true</c> if the objects equal each other. <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as LBSTerrainConnectionGrid;
            if(other!=null)
            {
                if (this.Assets.Count != other.Assets.Count) return false;
                if (this.gridSize != other.gridSize) return false;
                if (this.gridList.Count != other.gridList.Count) return false;
            }
            for (int i = 0; i < Assets.Count; i++)
            {
                if (!Assets[i].Equals(other.Assets[i])) return false;
            }
            for(int i = 0; i < gridList.Count; i++)
            {
                if (!gridList[i].Equals(other.gridList[i])) return false;
            }
            for (int i = 0; i < colorPalette.Count; i++)
            {
                if (!colorPalette[i].Equals(other.colorPalette[i])) return false;
            }
            for (int i = 0; i < colorPaletteID.Count; i++)
            {
                if (!colorPaletteID[i].Equals(other.colorPaletteID[i])) return false;
            }
            return true;
        }
        /// <summary>
        /// Obtains the hash code of the object.
        /// </summary>
        /// <returns>Returns hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Validates the object and checks if it was correctly created and initialized.
        /// </summary>
        /// <returns>Returns a list of warnings if any discrepances are found.</returns>
        public override List<string> Validate()
        {
            List<string> warnings = new List<string>();
            if (Assets.Count == 0) {
                warnings.Add("Bundle contains no assets");
                return warnings; 
            }
            foreach(Asset asset in Assets)
            {
                if (GetGrids(asset).Count == 0)
                {
                    warnings.Add("gridlist for " + asset + " is null.");
                }
            }
            return warnings;
        }
        
        #endregion
        
        

    }  

    [System.Serializable]
    public class AssetConnectionGrid
    { 
        [SerializeField, JsonRequired]
        private int[] terrainFlag = new int[9];
        [SerializeField, JsonRequired]
        private Asset assetReference;

        public int[] TerrainFlag
        {
            get => terrainFlag;
            set => terrainFlag = value;
        }
        public Asset AssetReference => assetReference;
        public int GridSize => terrainFlag.Length;
        public int BorderSize => Mathf.RoundToInt(Mathf.Sqrt(terrainFlag.Length));

        public AssetConnectionGrid(int[] terrainFlag, Asset assetReference)
        {
            this.terrainFlag = terrainFlag;
            this.assetReference = assetReference;
        }
        public AssetConnectionGrid(int q, Asset assetReference)
        {
            terrainFlag = new int[q];
            for(int i=0; i<q; i++)
            {
                terrainFlag[i] = 0;
            }
            this.assetReference = assetReference;
        }

        public int VectorToInt(Vector2 vector)
        {
            //If not a square, return
            var lengthSqrt = Mathf.Sqrt(TerrainFlag.Length);
            if (lengthSqrt != Mathf.RoundToInt(lengthSqrt))
            {
                throw new Exception("Terrain flag is not a square!");
            }
            //If over length, return
            if ((vector.x * vector.y) > (terrainFlag.Length)) { return -1; }
            
            //Make into int
            var vecInt = vector.ToInt();

            //This will likely explode in my face in the future and ONLY works if the terrain is a square. Oh well!
            return vecInt.x + (vecInt.y * Mathf.RoundToInt(lengthSqrt));
        }

        public int FlagFromVector(Vector2 vec)
        {
            var vecInt = vec.ToInt();
            if((vec.x * vec.y) > GridSize) return 0;
            return terrainFlag[((vecInt.y * BorderSize) + vecInt.x)];
        }
        public int FlagFromVector(int x, int y) => FlagFromVector(new Vector2(x, y));

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals (object obj)
        {
            var other = obj as AssetConnectionGrid;
            if (!assetReference.Equals(other.assetReference)) return false;
            if (terrainFlag.Length != other.terrainFlag.Length) return false;
            for(int i=0; i<terrainFlag.Length;i++)
            {
                if (terrainFlag[i] != other.terrainFlag[i]) return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class IndividualAsset
    {
        Asset assetReference;
        string id;

        IndividualAsset(Asset refer, int index)
        {
            assetReference = refer;
            id = refer.obj.name.GetHashCode().ToString() + index.ToString() + refer.probability.GetHashCode();
        }
    }
}

