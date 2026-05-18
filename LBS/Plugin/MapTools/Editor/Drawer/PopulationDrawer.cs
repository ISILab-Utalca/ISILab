using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using LBS.Components;
using LBS.Components.TileMap;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(PopulationBehaviour))]
    public class PopulationDrawer : Drawer
    {
        private Rect previousRect;
        private PopulationTileView lastHighlight = null;

        public LBSLayer OwnerLayer { get; private set; }

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Get behaviour
            if (target is not PopulationBehaviour population) return;

            PopulationTileView.SelectedTile?.Highlight(false);

            OwnerLayer = population.OwnerLayer;
            PaintNewTiles(population, view);

            //OwnerLayer = population.OwnerLayer;
            //LoadAllTiles(population, view);

            //PaintNewTiles(population, view);
            //UpdateTilesRotation(population, view);
            //UpdateLoadedTiles(population, view);

            // Paint all tiles
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(population, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
        }

        public override void UpdateTiles(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not PopulationBehaviour population) return;

            PaintNewTiles(population, view);
            UpdateLoadedTiles(population, view);
        }

        private void PaintNewTiles(PopulationBehaviour population, MainView view)
        {
            int replaceCount = 0, createCount = 0;
            IEnumerable<TileBundleGroup> newTiles = population.RetrieveNewTiles().Cast<TileBundleGroup>();
            //Debug.Log($"POPULATION PAINT TILES: {newTiles.Count()}");
            // New tiles
            foreach (TileBundleGroup nTile in newTiles)
            {
                PopulationTileView tView;
                List<GraphElement> previousElement = view.GetElementsFromLayer(population.OwnerLayer, nTile.LocationKey);

                if (previousElement is not null && previousElement.Count > 0)
                {
                    replaceCount++;
                    tView = previousElement[0] as PopulationTileView;
                    UpdatePopulationTile(tView, nTile, population);
                }
                else
                {
                    createCount++;
                    tView = CreatePopulationTileView(nTile, population);
  
                    // Stores using TileBundleGroup as key
                    view.AddElementToLayerContainer(population.OwnerLayer, nTile.LocationKey, tView);
                }

                tView.style.display = (DisplayStyle)(population.OwnerLayer.IsVisible ? 0 : 1);
            }
            //Debug.Log($"Replaced: {replaceCount} | Created: {createCount}");
            //Debug.Log(view.graphElements.Count());
        }

        private void UpdateLoadedTiles(PopulationBehaviour population, MainView view)
        {
            population.Keys.RemoveWhere(item => item == null);

            UpdateTilesRotation(population, view);

            // Update stored tiles
            foreach (TileBundleGroup tile in population.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(population.OwnerLayer, tile.LocationKey);
                if (elements == null) continue;

                foreach (var graphElement in elements)
                {
                    var tView = (PopulationTileView)graphElement;
                    if (tView == null) continue;
                    if (!tView.visible) continue;

                    UpdatePopulationTile(tView, tile, population);
                }
            }
        }

        private void UpdateTilesRotation(PopulationBehaviour population, MainView view)
        {
            // Get rotated selection
            TileBundleGroup rotationHighlightedTile = null;
            var manipulator = ToolKit.Instance.GetActiveManipulator();
            if (manipulator is RotatePopulationTile { Selected: not null } rotate)
            {
                rotationHighlightedTile = rotate.Selected;
            }

            // Rotations
            foreach (TileBundleGroup nTileGroup in population.RetrieveNewRotations())
            {
                // Get PopulationTileViews from MainView
                foreach (var graphElement in view.GetElementsFromLayer(population.OwnerLayer, nTileGroup.LocationKey))
                {
                    // Rotate visual element
                    var tView = (PopulationTileView)graphElement;
                    tView?.SetDirection(nTileGroup.Rotation);

                    // Check for rotation manipulator highlight
                    if (rotationHighlightedTile != null && Equals(nTileGroup, rotationHighlightedTile))
                    {
                        lastHighlight?.Highlight(false);
                        lastHighlight = tView;
                        lastHighlight?.Highlight(true);
                    }
                    graphElement.layer = population.OwnerLayer.index;
                }
            }
        }

        private void UpdatePopulationTile(PopulationTileView tileView, TileBundleGroup nTile, PopulationBehaviour population)
        {
            Bundle bundle = nTile.BundleData.Bundle;

            tileView.SetColor(bundle.Color);
            tileView.SetImage(bundle.Icon);
            tileView.SetDirection(nTile.Rotation);

            if (bundle.GetHasTagCharacteristic("NonRotate"))
            {
                tileView.HideArrows();
            }

            Vector2 size = population.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;
            Vector2Int bundleSize = nTile.GetBundleSize();

            tileView.SetSize(size * bundleSize);
            tileView.SetPivot(new Vector2(LBSSettings.Instance.general.TileSize.x * bundleSize.x, LBSSettings.Instance.general.TileSize.y * bundleSize.y));

            Vector2 position = new Vector2(nTile.GetBounds().x, -nTile.GetBounds().y);
            tileView.SetPosition(new Rect(position * size, size));

            tileView.layer = population.OwnerLayer.index;
        }

        private void LoadAllTiles(PopulationBehaviour population, MainView view)
        {
            foreach (TileBundleGroup tileBundleGroup in population.TileBundleGroup)
            {
                PopulationTileView tileView;
                List<GraphElement> previousElement = view.GetElementsFromLayer(population.OwnerLayer, tileBundleGroup.LocationKey);

                if (previousElement is not null && previousElement.Count > 0)
                {
                    tileView = previousElement[0] as PopulationTileView;
                    UpdatePopulationTile(tileView, tileBundleGroup, population);
                }
                else
                {
                    // Stores using TileBundleGroup as key
                    tileView = CreatePopulationTileView(tileBundleGroup, population);
                    if (tileView is not null)
                        view.AddElementToLayerContainer(population.OwnerLayer, tileBundleGroup.LocationKey, tileView);
                    population.Keys.Add(tileBundleGroup);
                }

                if (tileView is not null)
                    tileView.style.display = (DisplayStyle)(population.OwnerLayer.IsVisible ? 0 : 1);
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not PopulationBehaviour population) return;

            foreach (TileBundleGroup tile in population.Keys)
            {
                foreach (var graphElement in view.GetElementsFromLayer(population.OwnerLayer, tile.LocationKey).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }

        public override void HideVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not PopulationBehaviour population) return;

            foreach (TileBundleGroup tile in population.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(population.OwnerLayer, tile.LocationKey);
                foreach (var graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
            }
        }

        private PopulationTileView CreatePopulationTileView(TileBundleGroup nTile, PopulationBehaviour population)
        {
            // Validates
            var bundle = nTile.BundleData.Bundle;

            if (bundle == null)
            {
                Debug.LogError($"Could not draw element \"{nTile.BundleData.BundleName}\". (Compatibility problem?)");
                return null;
            }

            // Create new graph element for the tile
            PopulationTileView tileView = new PopulationTileView(nTile, population.OwnerLayer);
            // ignore so it does not block the connection selection
            tileView.pickingMode = PickingMode.Ignore;

            UpdatePopulationTile(tileView, nTile, population);

             return tileView;
        }

        public override Texture2D GetTexture(object target, Rect sourceRect, Vector2Int tesselationSize)
        {
            var population = target as PopulationBehaviour;
            var texture = new Texture2D((int)(sourceRect.width * tesselationSize.x), (int)(sourceRect.height * tesselationSize.y));
            for(int i = 0; i < texture.GetPixels().Length / tesselationSize.x / tesselationSize.y; i++)
            {
                Vector2Int pos = sourceRect.ToMatrixPosition(i);
                BundleData bundle = population.GetBundleData(pos + Vector2Int.RoundToInt(sourceRect.position));
                if(bundle is null)
                {
                    var t = new Texture2D(1, 1);
                    t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                    t.Apply();
                    texture.InsertTextureInRect(t, pos.x * tesselationSize.x, pos.y * tesselationSize.y, tesselationSize.x, tesselationSize.y);
                }
                else
                {
                    Color color = bundle.Bundle.Color;

                    Texture2D source = new Texture2D(tesselationSize.x, tesselationSize.y, TextureFormat.ARGB32, false);
                    source.SetAllPixels(color);
                    source.Apply();

                    texture.InsertTextureInRect(source, pos.x * tesselationSize.x, pos.y * tesselationSize.y, tesselationSize.x, tesselationSize.y);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}