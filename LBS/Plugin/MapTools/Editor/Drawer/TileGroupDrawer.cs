using ISILab.AI.Optimization.Populations;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Settings;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Bundles;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(TileGroupBehavior))]
    public class PopulationTileDrawer : Drawer
    {
        bool hookedToOnChange = false;
        TileGroupBehavior tgb;
        public PopulationTileDrawer() { }

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Get behaviour
   
            if (target is not TileGroupBehavior itgb) return;
            tgb = itgb;
            if (!hookedToOnChange)
            {
                hookedToOnChange = true;
                tgb.OnSelectedChanged += (tile) =>
                {
                    PopulationTileGroupView.UpdateVisuals(null);
                };
            }

            view.ClearLayerComponentView(tgb.OwnerLayer, this);

            TileBundleGroup selected = tgb.SelectedTilemap;
            if (selected is null) return;

            PopulationTileGroupView addonview = new PopulationTileGroupView(selected);

            Vector2 size = tgb.OwnerLayer.TileSize * LBSSettings.Instance.general.TileSize;
            Vector2Int bundleSize = selected.GetBundleSize();

          //  addonview.SetSize(size * bundleSize);
          //  addonview.SetPivot(new Vector2(LBSSettings.Instance.general.TileSize.x * bundleSize.x, LBSSettings.Instance.general.TileSize.y * bundleSize.y));

            Vector2 position = new Vector2(selected.GetBounds().x, -selected.GetBounds().y);
            addonview.SetPosition(new Rect(position * size, size));

            addonview.layer = tgb.OwnerLayer.index;

            view.AddElementToLayerContainer(tgb.OwnerLayer, this, addonview as GraphElement);

        }

        public override void HideVisuals(object target, MainView view)
        {
           // throw new System.NotImplementedException();
        }

        public override void ShowVisuals(object target, MainView view)
        {
          //  throw new System.NotImplementedException();
        }

        public override void Update(object target, MainView view, Vector2 teselationSize)
        {
            if (target is not TileGroupBehavior population) return;

          //  PaintNewTiles(population, view);
          //  UpdateLoadedTiles(population, view);
        }

    }
}