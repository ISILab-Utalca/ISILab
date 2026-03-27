using ISILab.LBS.Characteristics;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static PathOS.PathOSNavUtility.NavmeshMemoryMapper;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(SchemaBehaviour))]
    public class SchemaDrawer : Drawer
    {
        private Color _zoneColor;
        private SchemaBehaviour schema;

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Get behaviour
            schema = target as SchemaBehaviour;

            // Get modules
            var tilesMod = schema.OwnerLayer.GetModule<TileMapModule>();
            var zonesMod = schema.OwnerLayer.GetModule<SectorizedTileMapModule>();
            var connectionsMod = schema.OwnerLayer.GetModule<ConnectedTileMapModule>();
            var stairsMod = schema.OwnerLayer.GetModule<StairsModule>();

            PaintNewTiles(schema, tesselationSize, view, zonesMod, connectionsMod, stairsMod);

            //UpdateLoadedTiles(schema, tesselationSize, view, zonesMod, connectionsMod);
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(schema, tesselationSize, view, tilesMod, zonesMod, connectionsMod, stairsMod);
                Loaded = true;
                FullRedrawRequested = false;
            }

            //Debug.Log(view.graphElements.Count());
        }

        public override void Update(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not SchemaBehaviour schema) return;
            var zonesMod = schema.OwnerLayer.GetModule<SectorizedTileMapModule>();
            var connectionsMod = schema.OwnerLayer.GetModule<ConnectedTileMapModule>();
            var stairsMod = schema.OwnerLayer.GetModule<StairsModule>();

            PaintNewTiles(schema, teselationSize, view, zonesMod, connectionsMod, stairsMod);
            UpdateLoadedTiles(schema, teselationSize, view, zonesMod, connectionsMod, stairsMod);
        }

        private void PaintNewTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view,
            SectorizedTileMapModule zonesMod, ConnectedTileMapModule connectionsMod, StairsModule stairsMod)
        {
            var newTiles = schema.RetrieveNewTiles();

            //foreach (object obj in schema.Keys)
            foreach (object obj in newTiles)
            {
                // LBSTile
                if (obj is LBSTile newTile)
                {
                    TileZonePair tz = zonesMod.GetPairTile(newTile);
                    TileConnectionsPair tc = connectionsMod.GetPair(newTile);
                    if (tz is null || tc is null) continue;

                    SchemaTileView tView;
                    List<GraphElement> previousElement = view.GetElementsFromLayer(schema.OwnerLayer, newTile);

                    if (previousElement is not null && previousElement.Count > 0)
                    {
                        tView = previousElement[0] as SchemaTileView;
                        UpdateTileView(tView, newTile, tz.Zone, tc.Connections, teselationSize, schema.OwnerLayer.index);
                    }
                    else
                    {
                        tView = GetTileView(newTile, tz.Zone, tc.Connections, teselationSize);
                        tView.layer = schema.OwnerLayer.index;

                        // Stores using LBSTile as key
                        view.AddElementToLayerContainer(schema.OwnerLayer, newTile, tView);
                    }
                    tView.style.display = (DisplayStyle)(schema.OwnerLayer.IsVisible ? 0 : 1);
                }
                // LBSStairs
                else if (obj is LBSStair newStair)
                {
                    StairsGraph sView;
                    List<GraphElement> previousElement = view.GetElementsFromLayer(schema.OwnerLayer, newStair);

                    if (previousElement is not null && previousElement.Count > 0)
                    {
                        sView = previousElement[0] as StairsGraph;
                        sView.Update(newStair);
                        sView.layer = schema.OwnerLayer.index;
                    }
                    else
                    {
                        sView = new StairsGraph(newStair, schema.OwnerLayer);
                        sView.layer = schema.OwnerLayer.index;

                        var pos = new Vector2(newStair.Positions[0].x, -newStair.Positions[0].y);
                        var size = DefaultSize * teselationSize;
                        sView.SetPosition(new Rect(pos * size, size));

                        // Stores using LBSStair as key
                        view.AddElementToLayerContainer(schema.OwnerLayer, newStair, sView);
                    }
                    sView.style.display = (DisplayStyle)(schema.OwnerLayer.IsVisible ? 0 : 1);
                }
            }
        }

        protected void UpdateTileKeyRelationship(SchemaBehaviour schema)
        {
            Debug.Log("tiles: " + schema.Tiles.Count + "// keys: " + schema.Keys.Count);
        }
        private void UpdateLoadedTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view,
            SectorizedTileMapModule zonesMod, ConnectedTileMapModule connectMod, StairsModule stairsMod)
        {
            schema.Keys.RemoveWhere(item => item == null);
            
            foreach (object obj in schema.Keys)
            {
                // LBSTile
                if (obj is LBSTile tile)
                {
                    var elements = view.GetElementsFromLayer(schema.OwnerLayer, tile);
                    if (elements == null) continue;

                    foreach (var graphElement in elements)
                    {
                        if (graphElement is not SchemaTileView tView) continue;
                        if (!tView.visible) continue;

                        TileZonePair tz = zonesMod.GetPairTile(tile);
                        var connections = connectMod.GetConnections(tile);
                        if (tz == null || connections == null)
                        {
                            Debug.LogWarning("SchemaDrawer: TileZonePair or connections not found fot tile " + tile);
                            continue;
                        }

                        UpdateTileView(tView, tile, tz.Zone, connections, teselationSize, schema.OwnerLayer.index);
                    }
                }
                // LBSStair
                else if (obj is LBSStair stair)
                {
                    var elements = view.GetElementsFromLayer(schema.OwnerLayer, stair);
                    if (elements == null) continue;

                    foreach (var graphElement in elements)
                    {
                        if (graphElement is not StairsGraph sView) continue;
                        if (!sView.visible) continue;
                        sView.Update(stair);
                        sView.layer = schema.OwnerLayer.index;

                    }
                }
            }
        }
        
        private void UpdateTileView(SchemaTileView tView, LBSTile tile, Zone zone, List<string> connections, Vector2 teselationSize, int layerIndex)
        {
            AdjustTileView(tView, tile, zone, connections, teselationSize);
            tView.layer = layerIndex;
        }

        private void LoadAllTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view, 
            TileMapModule tilesMod, SectorizedTileMapModule zonesMod, 
            ConnectedTileMapModule connectionsMod, StairsModule stairsMod)
        {
            // Paint all tiles
            foreach (LBSTile tile in tilesMod.Tiles)
            {
                TileZonePair tz = zonesMod.GetPairTile(tile);
                TileConnectionsPair tc = connectionsMod.GetPair(tile);
                SchemaTileView tView;
                List<GraphElement> previousElement = view.GetElementsFromLayer(schema.OwnerLayer, tile);

                if (previousElement is not null && previousElement.Count > 0)
                {
                    tView = previousElement[0] as SchemaTileView;
                    UpdateTileView(tView, tile, tz.Zone, tc.Connections, teselationSize, schema.OwnerLayer.index);
                }
                else
                {
                    tView = GetTileView(tile, tz.Zone, tc.Connections, teselationSize);
                    tView.layer = schema.OwnerLayer.index;
                    // Stores using LBSTile as key
                    view.AddElementToLayerContainer(schema.OwnerLayer, tile, tView);
                    schema.Keys.Add(tile);
                }

                tView.style.display = (DisplayStyle)(schema.OwnerLayer.IsVisible ? 0 : 1);
            }

            // LBSStairs
            foreach(LBSStair stair in stairsMod.Stairs)
            {
                StairsGraph sView;
                List<GraphElement> previousElement = view.GetElementsFromLayer(schema.OwnerLayer, stair);

                if (previousElement is not null && previousElement.Count > 0)
                {
                    sView = previousElement[0] as StairsGraph;
                    sView.Update(stair);
                    sView.layer = schema.OwnerLayer.index;
                }
                else
                {
                    sView = new StairsGraph(stair, schema.OwnerLayer);
                    sView.layer = schema.OwnerLayer.index;

                    var pos = new Vector2(stair.Positions[0].x, -stair.Positions[0].y);
                    var size = DefaultSize * teselationSize;
                    sView.SetPosition(new Rect(pos * size, size));

                    view.AddElementToLayerContainer(schema.OwnerLayer, stair, sView);
                    schema.Keys.Add(stair);
                }
                sView.style.display = (DisplayStyle)(schema.OwnerLayer.IsVisible ? 0 : 1);
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            if (target is not SchemaBehaviour schema) return;

            if (schema.Keys == null) return;

            foreach (var tile in schema.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(schema.OwnerLayer, tile);

                if (elements == null) continue;

                foreach (var graphElement in elements.Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        {
            if (target is not SchemaBehaviour schema) return;

            if (schema.Keys == null) return;

            foreach (var tile in schema.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayer(schema.OwnerLayer, tile);

                if (elements == null) continue;

                foreach (var graphElement in elements)
                {
                    if (graphElement != null)
                    {
                        graphElement.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        public override Texture2D GetTexture(object target, Rect sourceRect, Vector2Int tesselationSize)
        {
            var schema = target as SchemaBehaviour;
            var zones = schema.Zones;


            var texture = new Texture2D((int)(sourceRect.width * tesselationSize.x), (int)(sourceRect.height * tesselationSize.y));

            for (int j = 0; j < texture.height; j++)
            {
                for (int i = 0; i < texture.width; i++)
                {
                    texture.SetPixel(i, j, new Color(0f, 0f, 0f, 0));
                }
            }

            int border = (int)(tesselationSize.x *0.1f);

            foreach (var z in zones)
            {
                _zoneColor = z.Color;
                var tiles = schema.GetTiles(z);
                var text = GetTileTexture(tesselationSize, z.Color);

                Color floorColor = z.Color;

                foreach (var t in tiles)
                {
                    if (!sourceRect.Contains(t.Position))
                        continue;

                    var conns = schema.GetConnections(t);

                    if(conns == null || conns.Count < 4)
                        continue;

                    for (int j = 0; j < tesselationSize.y; j++)
                    {
                        for (int i = 0; i < tesselationSize.x; i++)
                        {
                            Color pixelColor = floorColor;

                            //Up
                            if (j >= tesselationSize.y - border && conns[LBSDirection.ToInt(LBSDirection.Up)] != SchemaBehaviour.Empty) { pixelColor = getSimpleColor(conns[LBSDirection.ToInt(LBSDirection.Up)]); }

                            //Down
                            if (j < border && conns[LBSDirection.ToInt(LBSDirection.Down)] != SchemaBehaviour.Empty) { pixelColor = getSimpleColor(conns[LBSDirection.ToInt(LBSDirection.Down)]); }

                            //Left
                            if (i < border && conns[LBSDirection.ToInt(LBSDirection.Left)] != SchemaBehaviour.Empty) { pixelColor = getSimpleColor(conns[LBSDirection.ToInt(LBSDirection.Left)]); }

                            //Right
                            if (i >= tesselationSize.x - border && conns[LBSDirection.ToInt(LBSDirection.Right)] != SchemaBehaviour.Empty) { pixelColor = getSimpleColor(conns[LBSDirection.ToInt(LBSDirection.Right)]); }

                            var pos = t.Position - sourceRect.position;
                            texture.SetPixel((int)(pos.x * tesselationSize.x) + i, (int)(pos.y * tesselationSize.y) + j, pixelColor);
                        }
                    }
                }

            }

            texture.Apply();

            return texture;
        }

        private Color getSimpleColor(string type)
        {
            if (type == SchemaBehaviour.Door) return Color.red;
            if (type == SchemaBehaviour.Window) return Color.cyan;
            if (type == SchemaBehaviour.Wall) return Color.Lerp(_zoneColor, Color.black, 0.3f);
            return Color.clear;
        }
        private SchemaTileView GetTileView(LBSTile tile, Zone zone, List<string> connections, Vector2 teselationSize)
        {
            var tView = new SchemaTileView();
            AdjustTileView(tView, tile, zone, connections, teselationSize);
            return tView;
        }

        private void AdjustTileView(SchemaTileView tView, LBSTile tile, Zone zone, List<string> connections, Vector2 teselationSize)
        {
            _zoneColor = zone.Color;
            var pos = new Vector2(tile.Position.x, -tile.Position.y);
            var size = DefaultSize * teselationSize;
            tView.SetPosition(new Rect(pos * size, size));
            tView.SetBackgroundColor(zone.Color);
            tView.SetBorderColor(zone.Color, zone.BorderThickness);
            tView.SetConnections(connections.ToArray());

            tView.RemoveConnectionViews();

            var Connections = SchemaTileView.GetConnectionPoints(connections);

            foreach (var connection in Connections)
            {
                if (string.IsNullOrEmpty(connection.Key) || string.IsNullOrEmpty(connection.Value)) continue;

                // divided by 4 offsets to center the background image
                var xOffset = tView.GetPosition().width / 8f;
                var yOffset = tView.GetPosition().height / 8f;

                float xPos = xOffset;
                float yPos = yOffset;


                switch (connection.Key)
                {
                    case LBSDirection.Up:
                        yPos += -(tView.GetPosition().height / 2f);
                        break;
                    case LBSDirection.Down:
                        yPos += (tView.GetPosition().height / 2f);
                        break;
                    case LBSDirection.Left:
                        xPos += -(tView.GetPosition().width / 2f);
                        break;
                    case LBSDirection.Right:
                        xPos += (tView.GetPosition().width / 2f);
                        break;
                }
                string connectionType = connection.Value;

                // Draw connection tile only if its not wall or open
                if (connectionType != SchemaBehaviour.Empty
                    && connectionType != SchemaBehaviour.Wall)
                {
                     tView.CreateConnectionView(schema.OwnerLayer, tile, connectionType, new Vector2(xPos, yPos), connection.Key);
                }
            }
        }

        private Texture2D GetTileTexture(Vector2Int size, Color color)
        {
            var t = new Texture2D(size.x, size.y);

            for (int j = 0; j < size.y; j++)
            {
                for (int i = 0; i < size.x; i++)
                {
                    t.SetPixel(i, j, color);
                }
            }

            return t;
        }

    }
}