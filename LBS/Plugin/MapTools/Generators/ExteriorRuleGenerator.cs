using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public class ExteriorRuleGenerator : LBSGeneratorRule
    {
        public ExteriorRuleGenerator() : base() { }
        // For template construction
        public ExteriorRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }

        private Tuple<LBSDirection, int> GetBundle(LBSDirectionedGroup group, List<string> connections, string center)
        {
            // Get connections
            List<LBSDirection> extTiles = group.GetDirs();
            return GetBundle(extTiles, connections, center);
        }

        private Tuple<LBSDirection, int> GetBundle(LBSDirectionedChance chance, List<string> connections, string center)
        {
            // Get connections
            List<LBSDirection> extTiles = chance.GetDirs();
            return GetBundle(extTiles, connections, center);
        }

        private Tuple<LBSDirection, int> GetBundle(List<LBSDirection> extTiles, List<string> connections, string center)
        {
            List<Tuple<LBSDirection, int>> possibles = new();

            foreach (LBSDirection extTile in extTiles)
            {
                for (int i = 0; i < 4; i++)
                {
                    List<string> curDir = extTile.Connections.Rotate(i);
                    if (curDir.SequenceEqual(connections))
                    {
                        if (string.IsNullOrEmpty(center) || string.IsNullOrEmpty(extTile.Center))
                        {
                            possibles.Add(new Tuple<LBSDirection, int>(extTile, i));
                        }
                        else if (center.Equals(extTile.Center))
                        {
                            return new Tuple<LBSDirection, int>(extTile, i);
                        }
                    }
                }
            }
            return possibles.FirstOrDefault();
        }

        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {

            var bundles = LBSAssetsStorage.Instance.Get<Bundle>(); // Como se esta usando esto??

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
            bool usingNavigableTags = navigableTags.Count > 0;// false;
            var s = bundle.GetCharacteristics<LBSDirectionedGroup>();
            var selected = s.Count > 0 ? s[0] : null;
            var c = bundle.GetCharacteristics<LBSDirectionedChance>();
            var chance = c.Count > 0 ? c[0] : null;
            
            // Create pivot
            var mainPivot = new GameObject("Exterior");
            GameObject navContainer = null;
            GameObject nonNavContainer = null;
            if(usingNavigableTags)
            {
                navContainer = new GameObject("Navigable");
                nonNavContainer = new GameObject("NotNavigable");
                navContainer.transform.parent = mainPivot.transform;
                nonNavContainer.transform.parent = mainPivot.transform;
            }
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

            //I use these to organize things now.
            //ToGenerate is an object on CharacteristicRule that contains the tile, the bundle AND the gameobject for referencing.
            //The directions just gather the rotation direction of the object
            var toGenerateList = new List<ToGenerateExterior>();

            foreach(LBSTile tile in chosenTiles) {

                //Identify what bundle the tile is.
                var pair = selected is not null ? GetBundle(selected, connctMod.GetConnections(tile), connctMod.GetPairCenter(tile)) :
                    chance is not null ? GetBundle(chance, connctMod.GetConnections(tile), connctMod.GetPairCenter(tile)) : null;
                //This should make things better!
                //var toGen = new ToGenerateExterior(tile, pair?.Item1?.Owner, null, pair is not null ? pair.Item2 : -1);
                if(pair is not null)
                {
                    var toGen = new ToGenerateExterior(tile, pair.Item1?.Owner, null, pair.Item2);

                    //So now we work with these
                    toGenerateList.Add(toGen);
                }
                else
                {
                    Debug.LogWarning("[ISILab]: Element generation has failed, " +
                        "make sure you have properly configured and assigned " +
                        "the Bundles you want to generate with.");
                }
            }

            //This is a HORRIFYING way to order the tiles. PLEASE change it if you find a better way! -Alice
            var toGenerateListOrdered = OrderBySameConnection(toGenerateList);
            foreach(ToGenerateExterior toGenTile in toGenerateListOrdered)
            {
                var patternSelector = toGenTile.Bundle != null ?
                toGenTile.Bundle.GetCharacteristics<LBSTerrainConnectionGrid>()?.FirstOrDefault() : null;

                if(patternSelector == null)
                {
                    //Select random if there is no pattern selector
                    var pref = toGenTile.Bundle.Assets?.RandomRullete(w => w.probability)?.obj;
                    toGenTile.GameObject = pref;
                    continue;
                }

                else
                {
                    var adjacentGens = new Dictionary<string, ToGenerateExterior>();
                    adjacentGens = SetAdjacentGens(toGenerateList, toGenTile);

                    Debug.Log("Choosing pattern for [" + toGenTile.Tile.x + " | " + toGenTile.Tile.y + "]");
                    var pref = ChoosePatternByGrid(toGenTile, adjacentGens);
                    if (pref == null)
                    {
                        //Debug.Log("starter chosen instead of grid");
                        pref = toGenTile.Bundle != null ? toGenTile.Bundle.Assets[0]?.obj : null;
                    }
                    //Debug.Log("ADDING CHOSEN PREFERENCE: " + pref);
                    toGenTile.GameObject = pref;        
                }
                
            }

            foreach(ToGenerateExterior toGen in toGenerateListOrdered) {

                if (toGen.GameObject == null)
                {
 
                    Debug.LogWarning("[ISILab]: Element generation has failed, " +
                        "make sure you have properly configured and assigned " +
                        "the Bundles you want to generate with.");
                    continue;
                }

#if UNITY_EDITOR
                var go = PrefabUtility.InstantiatePrefab(toGen.GameObject, null) as GameObject;
#else
                var go = GameObject.Instantiate(pref,null);
#endif

                var pos = new Vector3(toGen.Tile.Position.x * scale.x, 0, toGen.Tile.Position.y * scale.z);
                var delta = (new Vector3(scale.x, 0, scale.z) / 2f);
                go.transform.position = settings.position + pos - delta;

                if (toGen.Direction % 2 == 0)
                    go.transform.rotation = Quaternion.Euler(0, 90 * toGen.Direction % 360, 0);
                else
                    go.transform.rotation = Quaternion.Euler(0, 90 * (toGen.Direction - 2) % 360, 0);
                
                tiles.Add(go);
                goToTileMap.Add(go, toGen.Tile);

                // Add ref component
                LBSGenerated generatedComponent = go.AddComponent<LBSGenerated>();
                generatedComponent.BundleRef = toGen.Bundle;
                
            }

            //Warning
            //if (tiles.Count == 0)
            //{
            //    UnityEngine.Object.DestroyImmediate(mainPivot);
            //    return new GeneratedGO(null, 
            //        new LBSLog("No tiles were created in the tool. Can't generate game object.", LogType.Error));
            //}

            //Decides the position of the pivot based on the average position of every object generated
            //This is after we've created every object, so don't touch it, Alice!
            float x, y, z;
            if (tiles.Count > 0)
            {
                x = tiles.Average(t => t.transform.position.x);
                y = tiles.Min(t => t.transform.position.y);
                z = tiles.Average(t => t.transform.position.z);
            }
            else
            {
                x = y = z = 0f;
            }

            mainPivot.transform.position = new Vector3(x, y, z);

            
            foreach (GameObject tile in tiles)
            {
                if (usingNavigableTags)
                {
                    bool isNavigable = false;
                    LBSTile logicalTile = goToTileMap[tile];
                    string center = connctMod.GetPairCenter(logicalTile);

                    if (!string.IsNullOrEmpty(center))
                    {
                        isNavigable = navigableTags.Contains(center);
                    }
                    else
                    {
                        // Determine if the tile is navigable based on its connections (can be changed if you want more or less connections to be navigable)
                        List<string> connections = connctMod.GetConnections(logicalTile);
                        int validConnectionsCount = connections.Count(c => navigableTags.Contains(c));

                        isNavigable = validConnectionsCount >= 2;
                    }

                    if (isNavigable)
                    {
                        tile.transform.parent = navContainer.transform;
                    }
                    else
                    {
                        tile.transform.parent = nonNavContainer.transform;
                    }
                }
                else
                {
                    tile.transform.parent = mainPivot.transform;
                }
            }
            

            mainPivot.transform.position += settings.position;

            return new GeneratedGO(mainPivot, new LBSLog(0));
        }

        private IOrderedEnumerable<ToGenerateExterior> OrderBySameConnection(List<ToGenerateExterior> list)
        {
            var reorderedTiles = list.OrderByDescending(c => new bool[] {
            (list.FirstOrDefault(d => d.Tile.Position.Equals(new Vector2Int(c.Tile.Position.x - 1, c.Tile.Position.y)) && (c.Bundle.Equals(d.Bundle))) == null),
            (list.FirstOrDefault(d => d.Tile.Position.Equals(new Vector2Int(c.Tile.Position.x + 1, c.Tile.Position.y)) && (c.Bundle.Equals(d.Bundle))) == null),
            (list.FirstOrDefault(d => d.Tile.Position.Equals(new Vector2Int(c.Tile.Position.x, c.Tile.Position.y + 1)) && (c.Bundle.Equals(d.Bundle))) == null),
            (list.FirstOrDefault(d => d.Tile.Position.Equals(new Vector2Int(c.Tile.Position.x, c.Tile.Position.y - 1)) &&(c.Bundle.Equals(d.Bundle))) == null)
            }.Count(t => t));

            return reorderedTiles;
        }
        
        private Dictionary<string, ToGenerateExterior> SetAdjacentGens(List<ToGenerateExterior> genList, ToGenerateExterior toGenTile)
        {
            var adjacentGens = new Dictionary<string, ToGenerateExterior>();

            var leftGen = genList.FirstOrDefault(c => c.Tile.Position.Equals(new Vector2Int(toGenTile.Tile.Position.x - 1, toGenTile.Tile.Position.y)));
            if (leftGen != null) { adjacentGens.Add("Left", leftGen); }

            var rightGen = genList.FirstOrDefault(c => c.Tile.Position.Equals(new Vector2Int(toGenTile.Tile.Position.x + 1, toGenTile.Tile.Position.y)));
            if (rightGen != null) { adjacentGens.Add("Right", rightGen); }

            var upGen = genList.FirstOrDefault(c => c.Tile.Position.Equals(new Vector2Int(toGenTile.Tile.Position.x, toGenTile.Tile.Position.y + 1)));
            if (upGen != null) { adjacentGens.Add("Up", upGen); }

            var downGen = genList.FirstOrDefault(c => c.Tile.Position.Equals(new Vector2Int(toGenTile.Tile.Position.x, toGenTile.Tile.Position.y - 1)));
            if (downGen != null) { adjacentGens.Add("Down", downGen); }

            return adjacentGens;
        }

        private GameObject ChoosePatternByGrid(ToGenerateExterior toGen, Dictionary<string, ToGenerateExterior> adjacentGens)
        {
            //We know the current bundle has a selector, but we'll still put a failsafe.
            var gridSelector = toGen.Bundle.GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault();
            if (gridSelector == null) return null;
            if (adjacentGens.Count == 0)
            {
                return null;
            }

            //Make a list with a copy of every Asset Grid available.
            var assetGridList = new  List<AssetConnectionGrid>();
            foreach(AssetConnectionGrid assetGrid in gridSelector.GridList)
            {
                assetGridList.Add(assetGrid);
            }

            //Now, we check each adjacent preference. The sequence is like this:
            var checkedGrids = new Dictionary<string, AssetConnectionGrid>();
            //1. Check if each direction's bundle coincides with the current bundle. If it fails, it's ignored.
            //2. Check if each direction's preference object exists. If it fails, it's unchecked.

            //string debugDirect = "asset has the following: | ";

            //3. If the direction has a preference, find, inside the direction's grid, the AssetGrid matching its preference. This can be done via the GetGrid method.
            //4. Save access to the asset grid in checkedGrids, alongside the direction key.
            string[] directions = { "Left", "Right", "Up", "Down" };
            foreach(string direction in directions)
            {
                if(adjacentGens.ContainsKey(direction))
                {
                    var adjacentGrid = adjacentGens[direction].Bundle.GetCharacteristics<LBSTerrainConnectionGrid>().FirstOrDefault();
                    if(adjacentGrid!=null)
                    {
                        var adjacentPref = adjacentGrid.GetGrid(adjacentGens[direction].GameObject);
                        if (adjacentPref != null) checkedGrids.Add(direction, adjacentPref);
                        //debugDirect += direction + " | ";

                    }
                }
            }

            //Debug.Log(debugDirect);
            //6. When this has been done with all four directions, we move to the WFC.
            //6a. We check the list and immediately remove any "incompatible" assets we find.
            //We check opposite borders (right border in left object, so on and so forth) and, if any of the flags don't equate with the opposite border or aren't 0,
            //we remove the grid and break the for loop.

            //First of all, let's identify all 4 directions. We need to make sure the adjacent gen and current gen have the same bundles or it'll start comparing
            //incompatible grids.
            var leftSide = adjacentGens.ContainsKey("Left")
                ? adjacentGens["Left"].Bundle == toGen.Bundle
                    ? checkedGrids.ContainsKey("Left")
                        ? "Generated"
                        : "Unchecked"
                    : "Border"
                : "Border";
            var rightSide = adjacentGens.ContainsKey("Right")
                ? adjacentGens["Right"].Bundle == toGen.Bundle
                    ? checkedGrids.ContainsKey("Right")
                        ? "Generated"
                        : "Unchecked"
                    : "Border"
                : "Border";
            var topSide = adjacentGens.ContainsKey("Up")
                ? adjacentGens["Up"].Bundle == toGen.Bundle
                    ? checkedGrids.ContainsKey("Up")
                        ? "Generated"
                        : "Unchecked"
                    : "Border"
                : "Border";
            var downSide = adjacentGens.ContainsKey("Down")
                ? adjacentGens["Down"].Bundle == toGen.Bundle
                    ? checkedGrids.ContainsKey("Down")
                        ? "Generated"
                        : "Unchecked"
                    : "Border"
                : "Border";

            Debug.Log("LEFT: " + leftSide + " | RIGHT: " + rightSide + " | UP: " + topSide + " | DOWN: " + downSide);

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
                                        if (checkedGrids["Up"].FlagFromVector(0, grid.BorderSize - 1) == -1) removalClause = true;
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
                            if ((flag != 0) && (grid.FlagFromVector(0, i) != 0))
                            {
                                removalClause = (flag != grid.FlagFromVector(0, i));
                            } else
                            {
                                removalClause = grid.FlagFromVector(0, i) == -1;
                            } break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); Debug.Log(grid.AssetReference.obj + "removed in left side because of " + i); break; }

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
                                        if (checkedGrids["Down"].FlagFromVector(grid.BorderSize - 1, 0) == -1) removalClause = true;
                                    }
                                    else if (downSide != "Border") removalClause = true;
                                }
                            }
                            break;
                        case "Generated":
                            int flag = checkedGrids["Right"].FlagFromVector(0, i);
                            if ((flag != 0) && (grid.FlagFromVector(grid.BorderSize - 1, i) != 0))
                            {
                                removalClause = (flag != grid.FlagFromVector(grid.BorderSize - 1, i));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(grid.BorderSize - 1, i) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); Debug.Log(grid.AssetReference.obj + "removed in right side because of "+i); break; }

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
                                        if (checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, 0) == -1) removalClause = true;
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
                            if ((flag != 0) && (grid.FlagFromVector(i, 0)!=0))
                            {
                                removalClause = (flag != grid.FlagFromVector(i, 0));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(i, 0) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); Debug.Log(grid.AssetReference.obj + "removed in up side because of " + i); break; }

                    
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
                                        if (checkedGrids["Left"].FlagFromVector(grid.BorderSize - 1, grid.BorderSize - 1) == -1) removalClause = true; 
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
                            if ((flag != 0) && (grid.FlagFromVector(i, grid.BorderSize - 1)!=0))
                            {
                                removalClause = (flag != grid.FlagFromVector(i, grid.BorderSize - 1));
                            }
                            else
                            {
                                removalClause = grid.FlagFromVector(i, grid.BorderSize - 1) == -1;
                            }
                            break;
                    }
                    if (removalClause) { assetGridList.Remove(grid); Debug.Log(grid.AssetReference.obj + "removed in down side because of " + i); break; }
                }
            }
            //Hopefully it's not a lot of executions
            //7. We can assume every grid in the curating list is compatible with everything around it, so we choose a random from the remaining ones
            //Let's return the preferred object!
            var chosenObj = assetGridList.Count > 0 ? UnityEngine.Random.Range(0, assetGridList.Count) : 0;
            Debug.Log("Chosen object: " + chosenObj);
            return assetGridList.Count > 0
                ? assetGridList[chosenObj].AssetReference.obj
                : gridSelector.GridList[gridSelector.DefaultAsset].AssetReference.obj; // ArgumentOutOfRangeException (Simple_Exterior_Proto_Simple)
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

    public class ToGenerateExterior : ToGenerate
    {
        int direction;
        public int Direction { get => direction; set => direction = value; }

        public ToGenerateExterior(LBSTile tile = null, Bundle bundle = null, GameObject obj = null, int direct = -1) : base(tile, bundle, obj)
        {
            if(direct != -1)
            {
                direction = direct;
            }
        }
    }
}
