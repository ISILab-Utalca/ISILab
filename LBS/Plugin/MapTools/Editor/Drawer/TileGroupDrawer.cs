using ISILab.LBS.Behaviours;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Settings;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Bundles;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Drawers
{
    [Drawer(typeof(TileGroupBehavior))]
    public class PopulationTileDrawer : Drawer
    {
        public PopulationTileDrawer() { }

        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            // Get behaviour
            if (target is not TileGroupBehavior population) return;
          //  var gf = new VisualElement();
          //  view.AddElementToLayerContainer(population.OwnerLayer, this, gf as GraphElement);

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