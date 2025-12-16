using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Modules;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class PathOSTileView : GraphElement
    {

        #region FIELDS
        #endregion

        #region FIELDS VIEW
        private static VisualTreeAsset view;

        VisualElement elementTag;

        VisualElement dynamicTagObject;
        VisualElement dynamicTagTrigger;
        VisualElement dynamicObstacleObject;
        VisualElement dynamicObstacleTrigger;
        #endregion

        #region CONSTRUCTORS
        public PathOSTileView(PathOSTile tile)
        {
            if (view == null)
            {
                view = DirectoryTools.GetAssetByName<VisualTreeAsset>("PathOSTileView");
            }
            view.CloneTree(this);


            elementTag = this.Q<VisualElement>("ElementTag");
            dynamicTagObject = this.Q<VisualElement>("DynamicTagObject");
            dynamicTagTrigger = this.Q<VisualElement>("DynamicTagTrigger");
            dynamicObstacleObject = this.Q<VisualElement>("DynamicObstacleObject");
            dynamicObstacleTrigger = this.Q<VisualElement>("DynamicObstacleTrigger");

            PathOSStorage storage = PathOSStorage.Instance;
            // Set data
            if (tile.Tag != null)
            {
                SimulationEntityData data = tile.Tag.Label.Equals("Player") ?
                    storage.agentData :
                    storage.entityDataPool[tile.EntityType];
                SetImage(data.image);
            }
            SetEvents(tile);
        }
        #endregion

        #region METHODS
        public void SetImage(VectorImage image)
        {
            elementTag.style.backgroundImage = new StyleBackground(image);
        }

        public void SetEvents(PathOSTile tile)
        {

            if (tile == null) { Debug.LogWarning("PathOSTileView.SetEvents(): Tile nulo!"); return; }
            if (tile.Tag == null) { Debug.LogWarning("PathOSTileView.SetEvents(): Tile tiene tag nulo!"); }

            dynamicTagObject.style.display = tile.IsDynamicTagObject ? DisplayStyle.Flex : DisplayStyle.None;
            dynamicTagTrigger.style.display = tile.IsDynamicTagTrigger ? DisplayStyle.Flex : DisplayStyle.None;
            dynamicObstacleObject.style.display = tile.IsDynamicObstacleObject ? DisplayStyle.Flex : DisplayStyle.None;
            dynamicObstacleTrigger.style.display = tile.IsDynamicObstacleTrigger ? DisplayStyle.Flex : DisplayStyle.None;

        }
        #endregion
    }

}

