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
    [System.Serializable]
    //[LBSCharacteristic("Connection Grid", "")]
    public class LBSTerrainConnectionGrid : LBSCharacteristic, ICloneable
    {
        //Dictionary<Asset, AssetConnectionGrid> gridList = new Dictionary<Asset, AssetConnectionGrid>();
        [SerializeField, JsonRequired]
        List<AssetConnectionGrid> gridList = new List<AssetConnectionGrid>();

        //Dictionary<int, UnityEngine.Color> flagColorPalette = new Dictionary<int, UnityEngine.Color>();
        [SerializeField, JsonRequired]
        //List<KeyValuePair<int, Color>> colorPalette = new List<KeyValuePair<int, UnityEngine.Color>>();
        List<UnityEngine.Color> colorPalette = new List<UnityEngine.Color>();
        [SerializeField, JsonRequired]
        List<int> colorPaletteID = new List<int>();
        //List<UnityEngine.Color> colorPalette = new List<UnityEngine.Color>();

        [SerializeField, JsonRequired]
        int gridSize = 9;

        [SerializeField, JsonRequired]
        int defaultAsset = 0;

        #region PROPERTIES
        [JsonIgnore]
        public List<Asset> Assets
        {
            get => Owner.Assets;
        }

        [JsonIgnore]
        public List<AssetConnectionGrid> GridList => gridList;
        public int GridSize => gridSize;
        [JsonIgnore]
        public List<UnityEngine.Color> ColorPalette => colorPalette;
        public List<int> ColorPaletteID => colorPaletteID;

        public int DefaultAsset
        {
            get => defaultAsset;
            set => defaultAsset = Mathf.Clamp(value, 0, gridList.Count - 1);
        }
        #endregion

        #region CONSTRUCTOR
        public LBSTerrainConnectionGrid(int gSize = 9) {
            gridSize = gSize;
        }

        public LBSTerrainConnectionGrid() : base()
        {
        }
        #endregion

        #region METHODS
        public void AddColor(int id, UnityEngine.Color color)
        {
            if (ColorExists(id)) return;
            colorPalette.Add(color);
            colorPaletteID.Add(id);
        }

        public void RemoveColor(int id)
        {
            if (!ColorExists(id)) return;
            colorPalette.Remove(FindColor(id));
            colorPaletteID.Remove(id);
        }

        public bool ColorExists(int id)
        {
            return colorPaletteID.Any(c => c == id);
        }

        public UnityEngine.Color FindColor(int id)
        {
            return ColorPalette[colorPaletteID.IndexOf(id)];
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
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
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override List<string> Validate()
        {
            List<string> warnings = new List<string>();
            if (Assets.Count == 0) {
                warnings.Add("Bundle contains no assets");
                return warnings; 
            }
            foreach(Asset asset in Assets)
            {
                if (GetGrid(asset) == null)
                {
                    warnings.Add("gridlist for " + asset + " is null.");
                }
            }
            return warnings;
        }
        #endregion
        
        public AssetConnectionGrid GetGrid(Asset asset)
        {
            var match = gridList.Find(c => c.AssetReference.Equals(asset));
            return match;
        }

        public AssetConnectionGrid GetGrid(GameObject obj)
        {
            return gridList.Find(c => c.AssetReference.obj.Equals(obj));
        }

        public void SetGridSize(int gSize)
        {
            gridSize = gSize;
            foreach(AssetConnectionGrid grid in gridList)
            {
                grid.TerrainFlag = new int[gSize];
            }
        }

        public void UpdateGridList()
        {
            if (gridList == null) gridList = new List<AssetConnectionGrid>();
            var updatedGridList = new List<AssetConnectionGrid>();
            foreach(Asset asset in Assets)
            {
                var existingAsset = GetGrid(asset);
                updatedGridList.Add(existingAsset != null ? existingAsset : new AssetConnectionGrid(gridSize, asset));
            }
            gridList = updatedGridList;
        }
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

