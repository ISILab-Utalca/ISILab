using ISILab.DevTools.Macros;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.VisualElements;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing.Imaging;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(SchemaBehaviour))]
    public class SchemaDrawer : Drawer
    {

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Get behaviour
            var schema = target as SchemaBehaviour;

            // Get modules
            var tilesMod = schema.OwnerLayer.GetModule<TileMapModule>();
            var zonesMod = schema.OwnerLayer.GetModule<SectorizedTileMapModule>();
            var connectionsMod = schema.OwnerLayer.GetModule<ConnectedTileMapModule>();


            PaintNewTiles(schema, tesselationSize, view, zonesMod, connectionsMod);

            //UpdateLoadedTiles(schema, tesselationSize, view, zonesMod, connectionsMod);
            if (!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(schema, tesselationSize, view, tilesMod, zonesMod, connectionsMod);
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

            PaintNewTiles(schema, teselationSize, view, zonesMod, connectionsMod);
            UpdateLoadedTiles(schema, teselationSize, view, zonesMod, connectionsMod);
        }

        private void PaintNewTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view,
            SectorizedTileMapModule zonesMod, ConnectedTileMapModule connectionsMod)
        {
            foreach (LBSTile newTile in schema.RetrieveNewTiles())
            {
                TileZonePair tz = zonesMod.GetPairTile(newTile);
                TileConnectionsPair tc = connectionsMod.GetPair(newTile);

                SchemaTileView tView;
                List<GraphElement> previousElement = view.GetElementsFromLayerContainer(schema.OwnerLayer, newTile);

                if(previousElement is not null && previousElement.Count > 0)
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
        }

        protected void UpdateTileKeyRelationship(SchemaBehaviour schema)
        {
            Debug.Log("tiles: " + schema.Tiles.Count + "// keys: " + schema.Keys.Count);
        }
        private void UpdateLoadedTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view,
            SectorizedTileMapModule zonesMod, ConnectedTileMapModule connectMod)
        {
            schema.Keys.RemoveWhere(item => item == null);
            
            // Update stored tiles
            foreach (object obj in schema.Keys)
            {
                if(obj is not LBSTile tile) continue;

                var elements = view.GetElementsFromLayerContainer(schema.OwnerLayer, tile);
                if(elements == null) continue;
                
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
        }
        
        private void UpdateTileView(SchemaTileView tView, LBSTile tile, Zone zone, List<string> connections, Vector2 teselationSize, int layerIndex)
        {
            AdjustTileView(tView, tile, zone, connections, teselationSize);
            tView.layer = layerIndex;
        }

        private void LoadAllTiles(SchemaBehaviour schema, Vector2 teselationSize, MainView view, 
            TileMapModule tilesMod, SectorizedTileMapModule zonesMod, ConnectedTileMapModule connectionsMod)
        {
            // Paint all tiles
            foreach (LBSTile tile in tilesMod.Tiles)
            {
                TileZonePair tz = zonesMod.GetPairTile(tile);
                TileConnectionsPair tc = connectionsMod.GetPair(tile);
                SchemaTileView tView;
                List<GraphElement> previousElement = view.GetElementsFromLayerContainer(schema.OwnerLayer, tile);

                if(previousElement is not null && previousElement.Count > 0)
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
        }

        public override void ShowVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not SchemaBehaviour schema) return;
            
            foreach (LBSTile tile in schema.Keys)
            {
                foreach (var graphElement in view.GetElementsFromLayerContainer(schema.OwnerLayer, tile).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not SchemaBehaviour schema) return;
            
            foreach (LBSTile tile in schema.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayerContainer(schema.OwnerLayer, tile);
                foreach (var graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
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

                            //Top
                            if (j >= tesselationSize.y - border && conns[1] != "Empty") { pixelColor = getSimpleColor(conns[1]); }

                            //Down
                            if (j < border && conns[3] != "Empty") { pixelColor = getSimpleColor(conns[3]); }

                            //Left
                            if (i < border && conns[2] != "Empty") { pixelColor = getSimpleColor(conns[2]); }

                            //Right
                            if (i >= tesselationSize.x - border && conns[0] != "Empty") { pixelColor = getSimpleColor(conns[0]); }

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

            if (type == "Door") return Color.red;
            if (type == "Window") return Color.cyan;
            if (type == "Wall") return Color.Lerp(_zoneColor, Color.black, 0.3f);
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
                if (connectionType != LBSDirection.ToString(0) 
                    && connectionType != LBSDirection.ToString(1))
                {
                     tView.CreateConnectionView(tile, connectionType, new Vector2(xPos, yPos), connection.Key);
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