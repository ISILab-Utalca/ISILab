using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using LBS.Components;
using LBS.Components.TileMap;
using SharpNeatLib.Maths;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class ExteriorRuleGenerator : LBSGeneratorRule
    {
        public ExteriorRuleGenerator() : base() { }
        // For template construction
        public ExteriorRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }


        private Tuple<LBSDirection, int> GetBundle(LBSDirectionedGroup group, string[] conections)
        {
            // Get connections
            var connections = group.GetDirs();

            foreach (var connection in connections)
            {
                for (int i = 0; i < 4; i++)
                {
                    var curDir = connection.Connections.Rotate(i);
                    if (curDir.SequenceEqual(conections))
                    {
                       // Debug.Log("found");
                        return new Tuple<LBSDirection, int>(connection, i);
                    }
                }
            }
            return null;
        }


        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {

            var bundles = LBSAssetsStorage.Instance.Get<Bundle>();

            if (layer.Behaviours.Count == 0)
            {
                return new GeneratedGO(null,"No behaviours found");
            }
            
            var exteriorBehaviour = layer.Behaviours.Find(b => b is ExteriorBehaviour) as ExteriorBehaviour;
            var bundle = exteriorBehaviour?.Bundle; 
            if (bundle == null)
            {
                return new GeneratedGO(null, "Bundle not found");
            }
            
            var selected = bundle.GetCharacteristics<LBSDirectionedGroup>()[0];
            
            // Create pivot
            var mainPivot = new GameObject("Exterior");
            var scale = settings.scale;

            // Get modules
            var mapMod = layer.GetModule<TileMapModule>();
            var connctMod = layer.GetModule<ConnectedTileMapModule>();
            var tiles = new List<GameObject>();
            
            //So this is where I'm working on a little thing so the characteristic that chooses the tiles could be chosen.
            //Otherwise, it just keeps mapMod.Tiles as a default and randomizes the whole thing
            //This may take a bit, though! -Alice
            
            //We have the tiles here
            var chosenTiles = mapMod.Tiles;
            //var chosenTilesOrdered = chosenTiles.Select
            
            Debug.Log("MODULE | W: " + mapMod.Width + " | H: " + mapMod.Height + " | COUNT: "+chosenTiles.Count);
            var tilePrefPair = new Dictionary<LBSTile, GameObject>();

            for (int i = 0; i < chosenTiles.Count; i++)
            {
                // This gets the bundle immediately
                var pair = GetBundle(selected, connctMod.GetConnections(chosenTiles[i]).ToArray());

                //Get current bundle
                var currentBundle = pair?.Item1?.Owner;
                Debug.Log(chosenTiles[i].Position.x + " | " + chosenTiles[i].Position.y + " : " + currentBundle);
                
                //Then see if it has a selector. If not, we go for random!
                var patternSelector = currentBundle.GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault();

                if (patternSelector != null)
                {
                    var adjacentBundles = new Dictionary<string, Bundle>();
                    var adjacentPrefs = new Dictionary<string, GameObject>();

                    //Left!
                    var leftTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTiles[i].Position.x-1, chosenTiles[i].Position.y)));
                    if (leftTile != null)
                    {
                        var leftBundle = GetBundle(selected, connctMod.GetConnections(leftTile).ToArray()).Item1.Owner;
                        if (leftBundle.Equals(currentBundle)) {
                            adjacentBundles.Add("Left", leftBundle);
                            if (tilePrefPair.ContainsKey(leftTile)) adjacentPrefs.Add("Left", tilePrefPair[leftTile]);
                        }
                    }
                    //Right!
                    var rightTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTiles[i].Position.x + 1, chosenTiles[i].Position.y)));
                    if (rightTile != null)
                    {
                        var rightBundle = GetBundle(selected, connctMod.GetConnections(rightTile).ToArray()).Item1.Owner;
                        if (rightBundle.Equals(currentBundle))
                        {
                            adjacentBundles.Add("Right", rightBundle);
                            if (tilePrefPair.ContainsKey(rightTile)) adjacentPrefs.Add("Right", tilePrefPair[rightTile]);
                        }
                    }
                    //Up!
                    var upTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTiles[i].Position.x, chosenTiles[i].Position.y + 1)));
                    if (upTile != null)
                    {
                        var upBundle = GetBundle(selected, connctMod.GetConnections(upTile).ToArray()).Item1.Owner;
                        if (upBundle.Equals(currentBundle)) {
                            adjacentBundles.Add("Up", upBundle);
                            if (tilePrefPair.ContainsKey(upTile)) adjacentPrefs.Add("Up", tilePrefPair[upTile]);
                        }   
                    }
                    //Down!
                    var downTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTiles[i].Position.x, chosenTiles[i].Position.y - 1)));
                    if (downTile != null)
                    {
                        var downBundle = GetBundle(selected, connctMod.GetConnections(downTile).ToArray()).Item1.Owner;
                        if (downBundle.Equals(currentBundle))
                        {
                            adjacentBundles.Add("Down", downBundle);
                            if (tilePrefPair.ContainsKey(downTile)) adjacentPrefs.Add("Down", tilePrefPair[downTile]);
                        }
                    }

                    //var pref = pair?.Item1?.Owner?.Assets?.RandomRullete(w => w.probability)?.obj;
                    var pref = ChoosePatternByGrid(currentBundle, adjacentBundles, adjacentPrefs);
                    if(pref==null) {
                        Debug.Log("starter chosen instead of grid");
                        pref = pair?.Item1?.Owner?.Assets[0]?.obj;
                    }
                    Debug.Log("ADDING CHOSEN PREFERENCE: " + pref);
                    tilePrefPair.Add(chosenTiles[i], pref);
                }
                else
                {
                    //This is the same because I'm still figuring this out lol
                    var pref = pair?.Item1?.Owner?.Assets?.RandomRullete(w => w.probability)?.obj;
                    tilePrefPair.Add(chosenTiles[i], pref);
                }
            }

            foreach(KeyValuePair<LBSTile, GameObject> keyPair in tilePrefPair) {

                var tile = keyPair.Key;
                var pref = keyPair.Value;
                var pair = GetBundle(selected, connctMod.GetConnections(tile).ToArray());

                if (pref == null)
                {
 
                    Debug.LogWarning("[ISILab]: Element generation has failed, " +
                        "make sure you have properly configured and assigned " +
                        "the Bundles you want to generate with.");
                    continue;
                }

#if UNITY_EDITOR
                var go = PrefabUtility.InstantiatePrefab(pref,null) as GameObject;
#else
                var go = GameObject.Instantiate(pref,null);
#endif

                var pos = new Vector3(tile.Position.x * scale.x, 0, tile.Position.y * scale.y);
                var delta = (new Vector3(scale.x, 0, scale.y) / 2f);
                go.transform.position = settings.position + pos - delta;

                if (pair.Item2 % 2 == 0)
                    go.transform.rotation = Quaternion.Euler(0, 90 * (pair.Item2) % 360, 0);
                else
                    go.transform.rotation = Quaternion.Euler(0, 90 * (pair.Item2 - 2) % 360, 0);
                
                tiles.Add(go);

                var current = pair.Item1.Owner;
                // Add ref component
                LBSGenerated generatedComponent = go.AddComponent<LBSGenerated>();
                generatedComponent.BundleRef = current;
                
            }

            //Warning
            if (tiles.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(mainPivot);
                return new GeneratedGO(null, "No tiles were created in the tool. Can't generate game object.");
            }

            //Decides the position of the pivot based on the average position of every object generated
            //This is after we've created every object, so don't touch it, Alice!
            var x = tiles.Average(t => t.transform.position.x);
            var y = tiles.Min(t => t.transform.position.y);
            var z = tiles.Average(t => t.transform.position.z);

            mainPivot.transform.position = new Vector3(x, y, z);

            foreach (var tile in tiles)
            {
                tile.transform.parent = mainPivot.transform;
            }

            mainPivot.transform.position += settings.position;

            return new GeneratedGO(mainPivot, null);
        }

        private GameObject ChoosePatternByGrid(Bundle currentBundle, Dictionary<string, Bundle> adjacentBundles, Dictionary<string, GameObject> adjacentPreferences)
        {
            //We know the current bundle has a selector, but we'll still put a failsafe.
            var gridSelector = currentBundle.GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault();
            if (gridSelector == null) return null;
            if ((adjacentBundles.Count == 0) && (adjacentPreferences.Count == 0))
            {
                return null;
            }

            //Make a list with a copy of every Asset Grid available.
            var assetGridList = new  List<AssetConnectionGrid>();
            foreach(AssetConnectionGrid assetGrid in gridSelector.GridList)
            {
                assetGridList.Add(assetGrid);
            }

            //Now, we check each adjacent bundle. The sequence is like this:
            var checkedGrids = new Dictionary<string, AssetConnectionGrid>();
            //1. Check if each direction's preference exists. If it doesn't, it's ignored.
            //2. Check if each bundle exists and get its grid. If it fails in any of these, it's ignored.

            //string debugDirect = "asset has the following: | ";

            //3. If the direction has a preference, find, inside the direction's grid, the AssetGrid matching its preference. This can be done via the GetGrid method.
            //4. Save access to the asset grid in checkedGrids, alongside the direction key.
            if (adjacentBundles.ContainsKey("Left")&&adjacentPreferences.ContainsKey("Left"))
            {
                var leftAsset = adjacentPreferences["Left"];
                var leftGrid = adjacentBundles["Left"].GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault().GetGrid(adjacentPreferences["Left"]);
                checkedGrids.Add("Left", leftGrid);
                //debugDirect += "left | ";
            }
            if (adjacentBundles.ContainsKey("Right") && adjacentPreferences.ContainsKey("Right"))
            {
                var rightAsset = adjacentPreferences["Right"];
                var rightGrid = adjacentBundles["Right"].GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault().GetGrid(adjacentPreferences["Right"]);
                checkedGrids.Add("Right", rightGrid);
                //debugDirect += "right | ";
            }
            if (adjacentBundles.ContainsKey("Up") && adjacentPreferences.ContainsKey("Up"))
            {
                var upAsset = adjacentPreferences["Up"];
                var upGrid = adjacentBundles["Up"].GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault().GetGrid(adjacentPreferences["Up"]);
                checkedGrids.Add("Up", upGrid);
                //debugDirect += "up | ";
            }
            if (adjacentBundles.ContainsKey("Down") && adjacentPreferences.ContainsKey("Down"))
            {
                var downAsset = adjacentPreferences["Down"];
                var downGrid = adjacentBundles["Down"].GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault().GetGrid(adjacentPreferences["Down"]);
                checkedGrids.Add("Down", downGrid);
                //debugDirect += "down | ";
            }

            //Debug.Log(debugDirect);
            //6. When this has been done with all four directions, we move to the WFC.
            //6a. We check the list and immediately remove any "incompatible" assets we find.
            //We check opposite borders (right border in left object, so on and so forth) and, if any of the flags don't equate with the opposite border or aren't 0,
            //we remove the grid and break the for loop.
            int gridSize = gridSelector.GridSize;
            foreach(AssetConnectionGrid grid in gridSelector.GridList)
            {
                for (int i = 0; i < grid.BorderSize; i++)
                {
                    //If there's something to the left (or any direction), it'll ensure the grid flags match their respective opposite borders
                    if (checkedGrids.ContainsKey("Left"))
                    {
                        int flag = checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, i);
                        //Debug.Log("left flag "+i+ " = " + flag + " | this flag = " + grid.FlagFromVector(0, i));
                        if (flag != grid.FlagFromVector(0, i)) { assetGridList.Remove(grid); break; }
                    }

                    //Otherwise, we have to identify if the adjacent side is a border or not. For this, we check if there's a bundle preference set up.
                    //When an object has an adjacent bundle but not an adjacent preference, it means it's checking an unchecked tile.
                    //When an object has no bundle, it means it's a border.
                    else
                    {
                        if (!adjacentBundles.ContainsKey("Left"))
                        {
                            Debug.Log("left is border");
                            if (grid.FlagFromVector(0, i) != 0) { assetGridList.Remove(grid); break; }
                        }
                        else Debug.Log("left is unchecked");
                    }

                    if (checkedGrids.ContainsKey("Right"))
                    {
                        int flag = checkedGrids["Right"].FlagFromVector(0, i);
                        if (flag != grid.FlagFromVector(grid.BorderSize - 1, i)) { assetGridList.Remove(grid); break; }
                    }
                    else
                    {
                        if (!adjacentBundles.ContainsKey("Right"))
                        {
                            Debug.Log("right is border");
                            if (grid.FlagFromVector(grid.BorderSize - 1, i) != 0) { assetGridList.Remove(grid); break; }
                        }
                        else Debug.Log("right is unchecked");
                    }

                    if (checkedGrids.ContainsKey("Up"))
                    {
                        int flag = checkedGrids["Up"].FlagFromVector(i, grid.BorderSize - 1);
                        if (flag != grid.FlagFromVector(i, 0)) { assetGridList.Remove(grid); break; }
                    }
                    else
                    {
                        if (!adjacentBundles.ContainsKey("Up"))
                        {
                            Debug.Log("up is border");
                            if (grid.FlagFromVector(i, 0) != 0) { assetGridList.Remove(grid); break; }
                        }
                        else Debug.Log("up is unchecked");
                    }

                    if (checkedGrids.ContainsKey("Down"))
                    {
                        int flag = checkedGrids["Down"].FlagFromVector(i, 0);
                        if (flag != grid.FlagFromVector(i, grid.BorderSize - 1)) { assetGridList.Remove(grid); break; }
                    }
                    else
                    {
                        if (!adjacentBundles.ContainsKey("Down"))
                        {
                            Debug.Log("down is border");
                            if (grid.FlagFromVector(i, grid.BorderSize - 1) != 0) { assetGridList.Remove(grid); break; }
                        }
                        else Debug.Log("down is unchecked");
                    }
                }
            }
            //Debug.Log("options reduced to " + assetGridList.Count);
            //Hopefully it's not a lot of executions
            //7. We can assume every grid in the curating list is compatible with everything around it, so we choose a random from the remaining ones
            //Let's return the preferred object!
            var chosenObj = assetGridList.Count > 0 ? UnityEngine.Random.Range(0, assetGridList.Count) : 0;
            return assetGridList.Count > 0
                ? assetGridList[chosenObj].AssetReference.obj
                : gridSelector.GridList[gridSelector.DefaultAsset].AssetReference.obj;
        }

        public override object Clone()
        {
            return new ExteriorRuleGenerator();
        }

        public override bool Equals(object obj)
        {
            var other = obj as ExteriorRuleGenerator;

            if (other == null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override List<Message> CheckViability(LBSLayer layer)
        {
            throw new NotImplementedException(); // TODO: Implement this method to check if the rule is viable for the layer
        }
    }
}
