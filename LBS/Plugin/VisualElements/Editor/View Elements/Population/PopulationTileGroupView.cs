using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.UIElements;
using LBS.Components;
using ISILab.LBS.Behaviours;
using UnityEditor.Experimental.GraphView;

namespace ISILab.LBS.VisualElements
{
    public class PopulationTileGroupView : GraphElement
    {
        
        #region STATIC
        private static VisualTreeAsset view;
        #endregion

        #region FIELDS
        private readonly TileGroupBehavior _tileBehaviour;

        static private VisualElement _patrolIcon;
        static private VisualElement _triggerIcon;

        #endregion

        #region CONSTRUCTOR
        public PopulationTileGroupView(TileBundleGroup tile)
        {
            LoadVisualElement();
            UpdateVisuals(tile);
        }
        #endregion

        #region INITIALIZATION
        private void LoadVisualElement()
        {
            //    if (view == null)
            view = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationTileGroup", true);

            view.CloneTree(this);

            _triggerIcon = this.Q<VisualElement>("TriggerIcon");
            _patrolIcon = this.Q<VisualElement>("PatrolIcon");


        }

        static public void UpdateVisuals(TileBundleGroup tile)
        {
            if (_patrolIcon is null || _triggerIcon is null) 
            {
                return;
            }

            _patrolIcon.style.display = DisplayStyle.None;
            _triggerIcon.style.display = DisplayStyle.None;

            if (tile is null) return;
            BundleTileMapAddons addons = tile.Addons;
            if (addons is null) return;

            if(addons.trigger.Count > 0) _triggerIcon.style.display = DisplayStyle.Flex;
            if (addons.patrol.Points.Count > 0) _patrolIcon.style.display = DisplayStyle.Flex;

            
        }

        #endregion


        #region VISUAL CONTROL
      

        public void SetPivot(Vector2 pivot)
        {
            this.style.left = pivot.x;
            this.style.top = pivot.y;
        }

        public void SetSize(Vector2 size)
        {
            this.style.width = size.x;
            this.style.height = size.y;
        }


        #endregion
    }
}
