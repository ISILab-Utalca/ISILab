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
        [SerializeField, JsonRequired]
        List<AssetConnectionGrid> gridList = new List<AssetConnectionGrid>();
        
        [SerializeField, JsonRequired]
        List<UnityEngine.Color> colorPalette = new List<UnityEngine.Color>();
        
        [SerializeField, JsonRequired]
        List<int> colorPaletteID = new List<int>();
        
        [SerializeField, JsonRequired]
        int gridSize = 9;
        
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
        /// A list of all 'Asset-Connection Grids' stored in this characteristic.
        /// </summary>
        [JsonIgnore]
        public List<AssetConnectionGrid> GridList => gridList;
        /// <summary>
        /// The size of the 'Asset-Connection Grid' handled for each asset in the bundle. <br/>
        /// <b>NOTE</b>: Currently, grids have a locked size of 9, which cannot be manually modified. It currently does not work with different sizes.
        /// </summary>
        public int GridSize => gridSize;
        /// <summary>
        /// A list of every color handled in this characteristic's color palette.
        /// </summary>
        [JsonIgnore]
        public List<UnityEngine.Color> ColorPalette => colorPalette;
        /// <summary>
        /// A list storing the ID of every color handled in this characteristic's color palette. 
        /// </summary>
        public List<int> ColorPaletteID => colorPaletteID;
        /// <summary>
        /// The ID of the characteristic's default asset. This asset will be generated when no legal connections are found in a particular tile.
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
    /// <summary>
    /// A connection grid for a particular asset. Its main purpose consists of holding a number of flags, each assigned to a different directional slot in
    /// the asset, in order to allow easy comparisons between generated objects for the creation of consistent patterns.
    /// </summary>
    [System.Serializable]
    public class AssetConnectionGrid
    { 
        [SerializeField, JsonRequired]
        private int[] terrainFlag = new int[9];
        [SerializeField, JsonRequired]
        private Asset assetReference;
        /// <summary>
        /// An array holding every flag held by the Asset Grid. <b>NOTE</b>: This array currently has a set size of 9.
        /// </summary>
        public int[] TerrainFlag
        {
            get => terrainFlag;
            set => terrainFlag = value;
        }
        /// <summary>
        /// References the asset linked to the connection grid for 3D generation..
        /// </summary>
        public Asset AssetReference => assetReference;
        /// <summary>
        /// The size of the grid. It simply points to the length of the terrain flag array.
        /// </summary>
        public int GridSize => terrainFlag.Length;
        /// <summary>
        /// The size of the terrain grid's square borders. It currently points to the square root of the terrain flag array's length. <br/>
        /// <b>NOTE</b>: It may be personalizable to allow for non-square terrain grid sizes, but it currently depends on it being a perfect square.
        /// </summary>
        public int BorderSize => Mathf.RoundToInt(Mathf.Sqrt(terrainFlag.Length));

        /// <summary>
        /// A basic constructor for asset connection grids. Meant for copying already existing grids (or fully configurating one beforehand) 
        /// as it requires an existing terrain flag array.
        /// </summary>
        /// <param name="terrainFlag">A terrain flag array.</param>
        /// <param name="assetReference">The asset to reference for 3D generation and previews.</param>
        public AssetConnectionGrid(int[] terrainFlag, Asset assetReference)
        {
            this.terrainFlag = terrainFlag;
            this.assetReference = assetReference;
        }
        /// <summary>
        /// Alternative constructor. Initializes a basic terrain flag array, so it's used for asset grid initialization purposes.
        /// </summary>
        /// <param name="q">The size of the terrain flag array (normally 9).</param>
        /// <param name="assetReference">The asset to reference for 3D generation and previews.</param>
        public AssetConnectionGrid(int q, Asset assetReference)
        {
            terrainFlag = new int[q];
            for(int i=0; i<q; i++)
            {
                terrainFlag[i] = 0;
            }
            this.assetReference = assetReference;
        }
        /// <summary>
        /// Converts a Vector2 into an int value. Used to translate 2D coordinates into the 1D nature of the terrain flag array.
        /// </summary>
        /// <param name="vector">The vector to convert into an int value.</param>
        /// <returns></returns>
        /// <exception cref="Exception">The method will throw an exception if the array length doesn't have a square root.<br/>
        /// This is a failsafe to prevent issues with currently unimplemented non-square terrain grids.
        /// </exception>
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

        /// <summary>
        /// Obtains the terrain flag of a particular coordinate from a Vector2. Works on similar logic to the vector-to-int conversor.
        /// </summary>
        /// <param name="vec">The coordinate in vectors.</param>
        /// <returns>The flag in the array coordinate.</returns>
        public int FlagFromVector(Vector2 vec)
        {
            var vecInt = vec.ToInt();
            if((vec.x * vec.y) > GridSize) return 0;
            return terrainFlag[((vecInt.y * BorderSize) + vecInt.x)];
        }
        public int FlagFromVector(int x, int y) => FlagFromVector(new Vector2(x, y));

        /// <summary>
        /// Obtains the hash code of the object.
        /// </summary>
        /// <returns>Returns hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Compares two 'Asset Connection Grids'.
        /// </summary>
        /// <param name="obj">The object to compare to this.</param>
        /// <returns><c>true</c> if the objects equal each other. <c>false</c> otherwise.</returns>
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
}

