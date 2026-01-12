using ISILab.AI.Optimization.Populations;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(SimulationBehaviour))]
    public class SimulationDrawer : Drawer
    {
        SimulationBehaviour behaviour;

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
      
            if (behaviour is null)
            {
                behaviour = target as SimulationBehaviour;
                behaviour.OwnerLayer.OnChange += () =>
                {
                 //   PopulationTileGroupView.UpdateVisuals(null);
                    view.ClearLayerComponentView(behaviour.OwnerLayer, this);
                };
            }
            view.ClearLayerComponentView(behaviour.OwnerLayer, this);
            LoadAllTiles(behaviour, tesselationSize, view);
            /*
            PaintNewTiles(behaviour, tesselationSize, view);
            UpdateLoadedTiles(behaviour, tesselationSize, view);

            if(!Loaded || FullRedrawRequested)
            {
                LoadAllTiles(behaviour, tesselationSize, view);
                Loaded = true;
                FullRedrawRequested = false;
            }
            */
        }

        private void PaintNewTiles(SimulationBehaviour behaviour, Vector2 teselationSize, MainView view)
        {
            foreach(SimulationTile tile in behaviour.RetrieveNewTiles())
            {
                var tView = new SimulationTileView(tile);
                Vector2 pos = new Vector2(tile.X, -tile.Y);
                Vector2 size = DefaultSize * teselationSize;
                tView.SetPosition(new Rect(pos * size, size));
                //view.AddElement(tView);
                view.AddElementToLayerContainer(behaviour.OwnerLayer, tile, tView);
            }
        }

        private void UpdateLoadedTiles(SimulationBehaviour behaviour, Vector2 teselationSize, MainView view)
        {
            behaviour.Keys.RemoveWhere(item => item == null);

            foreach(object obj in behaviour.Keys)
            {
                if (obj is not SimulationTile tile) continue;

                List<GraphElement> elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile);
                if (elements == null) continue;

                foreach(GraphElement element in elements)
                {
                    var tView = (SimulationTileView) element;

                    if(tView == null) continue;
                    if(!tView.visible) continue;

                    Vector2 pos = new Vector2(tile.X, -tile.Y);
                    Vector2 size = DefaultSize * teselationSize;

                    tView.SetPosition(new Rect(pos * size, size));

                    tView.layer = behaviour.OwnerLayer.index;
                }
            }
        }

        private void LoadAllTiles(SimulationBehaviour behaviour, Vector2 teselationSize, MainView view)
        {
            foreach (SimulationTile tile in behaviour.Tiles)
            {
                var tView = new SimulationTileView(tile);
                //var size = behaviour.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;
                Vector2 pos = new Vector2(tile.X, -tile.Y);
                Vector2 size = DefaultSize * teselationSize;
                tView.SetPosition(new Rect(pos * size, size));
                tView.style.display = (DisplayStyle)(behaviour.OwnerLayer.IsVisible ? 0 : 1);
                //view.AddElement(tView);
                view.AddElementToLayerContainer(behaviour.OwnerLayer, this, tView);
                behaviour.Keys.Add(tile);
            }
        }

        public override void HideVisuals(object target, MainView view)
        {
            if (target is not SimulationBehaviour behaviour) return;
            foreach(SimulationTile tile in behaviour.Keys)
            {
                if (tile == null) continue;

                List<GraphElement> elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile).Where(graphElement => graphElement != null).ToList();
                foreach(GraphElement element in elements)
                {
                    element.style.display = DisplayStyle.None; 
                }
            }
        }

        public override void ShowVisuals(object target, MainView view)
        {
            if (target is not SimulationBehaviour behaviour) return;
            foreach(SimulationTile tile in behaviour.Keys)
            {
                if (tile == null) continue;

                List<GraphElement> elements = view.GetElementsFromLayer(behaviour.OwnerLayer, tile);
                foreach (GraphElement element in elements)
                {
                    element.style.display = DisplayStyle.Flex;
                }
            }
        }

        public override Texture2D GetTexture(object target, Rect sourceRect, Vector2Int tesselationSize)
        {
            //return new Texture2D((int)(sourceRect.width * tesselationSize.x), (int)(sourceRect.height * tesselationSize.y));
            return null;
        }
    }
}
