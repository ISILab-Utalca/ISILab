using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Modules;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Behaviours;
using UnityEditor.Experimental.GraphView;
using ISILab.LBS.Components;

namespace ISILab.LBS.VisualElements
{
    public class PopulationTileGroupView : GraphElement
    {

        #region STATIC
        private static VisualTreeAsset view;
        #endregion

        #region FIELDS
        private readonly TileGroupBehavior _tileBehaviour;

        static private VisualElement _dropIcon;
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
            _dropIcon = this.Q<VisualElement>("DropIcon");

        }

        static public void UpdateVisuals(TileBundleGroup tile)
        {

            if (_patrolIcon is null || _triggerIcon is null)
            {
                return;
            }

            _dropIcon.style.display = DisplayStyle.None;
            _patrolIcon.style.display = DisplayStyle.None;
            _triggerIcon.style.display = DisplayStyle.None;

            if (tile is null) return;
            BundleTileMapAddons addons = tile.Addons;
            if (addons is null) return;

            if (addons.OnDestroyDrop is not null) 
            {
                _dropIcon.style.backgroundImage = new StyleBackground(addons.OnDestroyDrop.Icon);
                _dropIcon.style.display = DisplayStyle.Flex; 
            }
            if (addons.Triggers.Count > 0) _triggerIcon.style.display = DisplayStyle.Flex;
            if (addons.Patrol.Points.Count > 0) _patrolIcon.style.display = DisplayStyle.Flex;

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
