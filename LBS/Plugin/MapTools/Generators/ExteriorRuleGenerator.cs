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
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

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
                return new GeneratedGO(null,new LBSLog("No behaviours found", LogType.Error));
            }
            
            var exteriorBehaviour = layer.Behaviours.Find(b => b is ExteriorBehaviour) as ExteriorBehaviour;
            var bundle = exteriorBehaviour?.Bundle; 
            if (bundle == null)
            {
                return new GeneratedGO(null, new LBSLog("Bundle not found", LogType.Error));
            }

            List<string> navigableTags = exteriorBehaviour.NavigableTags;
            var selected = bundle.GetCharacteristics<LBSDirectionedGroup>()[0];
            
            // Create pivot
            var mainPivot = new GameObject("Exterior");
            GameObject navContainer = new GameObject("Navigable");
            GameObject nonNavContainer = new GameObject("NotNavigable");
            navContainer.transform.parent = mainPivot.transform;
            nonNavContainer.transform.parent = mainPivot.transform;
            var scale = settings.scale;

            // Get modules
            var mapMod = layer.GetModule<TileMapModule>();
            var connctMod = layer.GetModule<ConnectedTileMapModule>();
            var tiles = new List<GameObject>();

            Dictionary<GameObject, LBSTile> goToTileMap = new Dictionary<GameObject, LBSTile>();

            //So this is where I'm working on a little thing so the characteristic that chooses the tiles could be chosen.
            //Otherwise, it just keeps mapMod.Tiles as a default and randomizes the whole thing
            //This may take a bit, though! -Alice

            //We have the tiles here
            var chosenTiles = mapMod.Tiles;

            //This is a HORRIFYING way to order the tiles. PLEASE change it if you find a better way! -Alice
            var chosenTilesOrdered = OrderBySameConnection(chosenTiles);

            //Debug.Log("MODULE | W: " + mapMod.Width + " | H: " + mapMod.Height + " | COUNT: "+chosenTiles.Count);
            var tilePrefPair = new Dictionary<LBSTile, GameObject>();

            foreach(LBSTile chosenTile in chosenTilesOrdered)
            {
                // This gets the bundle immediately
                var pair = GetBundle(selected, connctMod.GetConnections(chosenTile).ToArray());

                //Get current bundle
                var currentBundle = pair?.Item1?.Owner;
                //Debug.Log(chosenTile.Position.x + " | " + chosenTile.Position.y + " : " + currentBundle);

                //Then see if it has a selector. If not, we go for random!

                var patternSelector = currentBundle != null ?
                    currentBundle.GetCharacteristics<LBSTerrainConnectionGrid>()?.FirstOrDefault() : null;

                if (patternSelector != null)
                {
                    var adjacentBundles = new Dictionary<string, Bundle>();
                    var adjacentPrefs = new Dictionary<string, GameObject>();

                    //Left!
                    var leftTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTile.Position.x-1, chosenTile.Position.y)));
                    if (leftTile != null)
                    {
                        var leftBundle = GetBundle(selected, connctMod.GetConnections(leftTile).ToArray()).Item1.Owner;
                        if (leftBundle.Equals(currentBundle)) {
                            adjacentBundles.Add("Left", leftBundle);
                            if (tilePrefPair.ContainsKey(leftTile)) adjacentPrefs.Add("Left", tilePrefPair[leftTile]);
                        }
                    }
                    //Right!
                    var rightTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTile.Position.x + 1, chosenTile.Position.y)));
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
                    var upTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTile.Position.x, chosenTile.Position.y + 1)));
                    if (upTile != null)
                    {
                        var upBundle = GetBundle(selected, connctMod.GetConnections(upTile).ToArray()).Item1.Owner;
                        if (upBundle.Equals(currentBundle)) {
                            adjacentBundles.Add("Up", upBundle);
                            if (tilePrefPair.ContainsKey(upTile)) adjacentPrefs.Add("Up", tilePrefPair[upTile]);
                        }   
                    }
                    //Down!
                    var downTile = chosenTiles.FirstOrDefault(c => c.Position.Equals(new Vector2Int(chosenTile.Position.x, chosenTile.Position.y - 1)));
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
                        //Debug.Log("starter chosen instead of grid");
                        pref = pair?.Item1?.Owner?.Assets[0]?.obj;
                    }
                    //Debug.Log("ADDING CHOSEN PREFERENCE: " + pref);
                    tilePrefPair.Add(chosenTile, pref);
                }
                else
                {
                    //This is the same because I'm still figuring this out lol
                    var pref = pair?.Item1?.Owner?.Assets?.RandomRullete(w => w.probability)?.obj;
                    tilePrefPair.Add(chosenTile, pref);
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

                goToTileMap.Add(go, tile);

                var current = pair.Item1.Owner;
                // Add ref component
                LBSGenerated generatedComponent = go.AddComponent<LBSGenerated>();
                generatedComponent.BundleRef = current;
                
            }

            //Warning
            if (tiles.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(mainPivot);
                return new GeneratedGO(null, 
                    new LBSLog("No tiles were created in the tool. Can't generate game object.", LogType.Error));
            }

            //Decides the position of the pivot based on the average position of every object generated
            //This is after we've created every object, so don't touch it, Alice!
            var x = tiles.Average(t => t.transform.position.x);
            var y = tiles.Min(t => t.transform.position.y);
            var z = tiles.Average(t => t.transform.position.z);

            mainPivot.transform.position = new Vector3(x, y, z);

            foreach (var tile in tiles)
            {
                LBSTile logicalTile = goToTileMap[tile];
                List<string> connections = connctMod.GetConnections(logicalTile);
                int validConnectionsCount = connections.Count(c => navigableTags.Contains(c));

                // Determine if the tile is navigable based on its connections (can be changed if you want more or less connections to be navigable)
                bool isNavigable = validConnectionsCount >= 2;

                if (isNavigable)
                {
                    tile.transform.parent = navContainer.transform;
                }
                else
                {
                    tile.transform.parent = nonNavContainer.transform;
                }
            }

            mainPivot.transform.position += settings.position;

            return new GeneratedGO(mainPivot, new LBSLog(0));
        }

        private IOrderedEnumerable<LBSTile> OrderBySameConnection(List<LBSTile> list)
        {
            var reorderedTiles = list.OrderByDescending(c => new bool[] {
            (list.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x - 1, c.Position.y))) == null),
            (list.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x + 1, c.Position.y))) == null),
            (list.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x, c.Position.y + 1))) == null),
            (list.FirstOrDefault(d => d.Position.Equals(new Vector2Int(c.Position.x, c.Position.y - 1))) == null)
            }.Count(t => t));

            return reorderedTiles;

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

            //First of all, let's identify all 4 directions
            var leftSide = checkedGrids.ContainsKey("Left") ? "Generated" : adjacentBundles.ContainsKey("Left") ? "Unchecked" : "Border";
            var rightSide = checkedGrids.ContainsKey("Right") ? "Generated" : adjacentBundles.ContainsKey("Right") ? "Unchecked" : "Border";
            var topSide = checkedGrids.ContainsKey("Up") ? "Generated" : adjacentBundles.ContainsKey("Up") ? "Unchecked" : "Border";
            var downSide = checkedGrids.ContainsKey("Down") ? "Generated" : adjacentBundles.ContainsKey("Down") ? "Unchecked" : "Border";
            //Debug.Log("LEFT: " + leftSide + "| RIGHT: " + rightSide + "| UP: " + topSide + " | DOWN: " + downSide);
            
            foreach (AssetConnectionGrid grid in gridSelector.GridList)
            {
                bool removalClause = false;
                //Okay, let's remake the entire thing now that we have to take into account whether the sides are corners, unchecked or verified.
                //First of all, let's check out the borders
                for (int i = 0; i < grid.BorderSize; i++)
                {
                    switch (leftSide)
                    {
                        case "Border": 
                            removalClause = grid.FlagFromVector(0, i) != -1;
                            break;
                        case "Unchecked":
                            if (grid.FlagFromVector(0, i) == -1)
                            {
                                if (i == 0)
                                {
                                    if (topSide == "Generated")
                                    {
                                        if (checkedGrids["Up"].FlagFromVector(0, grid.BorderSize - 1) != -1) removalClause = true;
                                    }
                                    else if (topSide != "Border") removalClause = true;
                                }
                                else if (i == grid.BorderSize - 1)
                                {
                                    if (downSide == "Generated")
                                    {
                                        if (checkedGrids["Down"].FlagFromVector(0, 0) != -1) removalClause = true;
                                    }
                                    else if (downSide != "Border") removalClause = true;
                                }
                            }
                            break;
                        case "Generated":
                            //If the grid flag has a code other than 0, it HAS to be compared to the generated tile near it. If it is 0, it can connect to anything except borders.
                            int flag = checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, i);
                            if (flag != 0)
                            {
                                removalClause = (flag != grid.FlagFromVector(0, i));
                            } else
                            {
                                removalClause = grid.FlagFromVector(0, i) == -1;
                            } break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); /*Debug.Log(grid.AssetReference.obj + "removed in left side because of " + i);*/ break; }

                    switch(rightSide)
                    {
                        case "Border":
                            removalClause = grid.FlagFromVector(grid.BorderSize - 1, i) != -1;
                            break;
                        case "Unchecked":
                            if (grid.FlagFromVector(grid.BorderSize - 1, i) == -1)
                            {
                                if (i == 0)
                                {
                                    if (topSide == "Generated")
                                    {
                                        if (checkedGrids["Up"].FlagFromVector(grid.BorderSize - 1, grid.BorderSize - 1) != -1) removalClause = true;
                                    }
                                    else if (topSide != "Border") removalClause = true;

                                }
                                else if (i == grid.BorderSize - 1)
                                {
                                    if (downSide == "Generated")
                                    {
                                        if (checkedGrids["Down"].FlagFromVector(grid.BorderSize - 1, 0) != -1) removalClause = true;
                                    }
                                    else if (downSide != "Border") removalClause = true;
                                }
                            }
                            break;
                        case "Generated":
                            int flag = checkedGrids["Right"].FlagFromVector(0, i);
                            if (flag != 0)
                            {
                                removalClause = (flag != grid.FlagFromVector(grid.BorderSize - 1, i));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(grid.BorderSize - 1, i) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); /*Debug.Log(grid.AssetReference.obj + "removed in right side because of "+i);*/ break; }

                    switch (topSide)
                    {
                        case "Border":
                            removalClause = grid.FlagFromVector(i, 0) != -1;
                            break;
                        case "Unchecked":
                            if (grid.FlagFromVector(i, 0) == -1)
                            {
                                if (i == 0)
                                {
                                    if (leftSide == "Generated")
                                    {
                                        if (checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, 0) != -1) removalClause = true;
                                    }
                                    else if (leftSide != "Border") removalClause = true;

                                }
                                else if (i == grid.BorderSize - 1)
                                {
                                    if (rightSide == "Generated")
                                    {
                                        if (checkedGrids["Right"].FlagFromVector(0, 0) != -1) removalClause = true;
                                    }
                                    else if (rightSide != "Border") removalClause = true;
                                }
                            }
                            break;
                        case "Generated":
                            int flag = checkedGrids["Up"].FlagFromVector(i, grid.BorderSize-1);
                            if (flag != 0)
                            {
                                removalClause = (flag != grid.FlagFromVector(i, 0));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(i, 0) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); /*Debug.Log(grid.AssetReference.obj + "removed in up side because of " + i);*/ break; }

                    switch (downSide)
                    {
                        case "Border":
                            removalClause = grid.FlagFromVector(i, grid.BorderSize - 1) != -1;
                            break;
                        case "Unchecked":
                            if(grid.FlagFromVector(i, grid.BorderSize-1) == -1)
                            {
                                if (i == 0)
                                {
                                    if (leftSide == "Generated")
                                    {
                                        if (checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, grid.BorderSize - 1) != -1) removalClause = true; 
                                    }
                                    else if (leftSide != "Border") removalClause = true;

                                }
                                else if (i == grid.BorderSize-1)
                                {
                                    if (rightSide == "Generated")
                                    {
                                        if (checkedGrids["Right"].FlagFromVector(0, grid.BorderSize - 1) != -1) removalClause = true;
                                    }
                                    else if (rightSide != "Border") removalClause = true;
                                }
                            }
                            break;
                        case "Generated":
                            int flag = checkedGrids["Down"].FlagFromVector(i, 0);
                            if (flag != 0)
                            {
                                removalClause = (flag != grid.FlagFromVector(i, grid.BorderSize - 1));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(i, grid.BorderSize - 1) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); /*Debug.Log(grid.AssetReference.obj + "removed in down side because of " + i);*/ break; }
                }
            }
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

        public override bool CheckViability(LBSLayer layer)
        {
            return true; // TODO: Implement this method to check if the rule is viable for the layer
        }
    }

    public class ExteriorRuleGeneratorNew : LBSGeneratorRule
    {
        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            throw new NotImplementedException();
        }

        public override bool CheckViability(LBSLayer layer)
        {
            throw new NotImplementedException();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        
    }
}
