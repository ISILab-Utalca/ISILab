using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class PathOSTileView : GraphElement
    {

        #region FIELDS

        string TierLowGuid = "ea89544be79a4924388045079405084d";
        string TierMedGuid = "f2dcad360ed296c4f824a3a2f77cdfc3";
        string TierHighGuid = "c2490f3c8ed54d04c8ad91c90c969e39";

        #endregion

        #region FIELDS VIEW
        private static VisualTreeAsset view;

        VisualElement background;
        VisualElement elementTag;
        VisualElement tier;

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

            background = this.Q<VisualElement>("Background");
            elementTag = this.Q<VisualElement>("ElementTag");
            tier = this.Q<VisualElement>("TierElement");
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
                SetColor(data.color);
            }
            SetEvents(tile);


            switch (PathOSStorage.GetTier(tile.EntityType))
            {
                case TierEntity.None:
                    tier.style.display = DisplayStyle.None; break;
                case TierEntity.Low:
                    tier.style.backgroundImage = new StyleBackground(Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(TierLowGuid)); break;
                case TierEntity.Med:
                    tier.style.backgroundImage = new StyleBackground(Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(TierMedGuid)); break;
                case TierEntity.High:
                    tier.style.backgroundImage = new StyleBackground(Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(TierHighGuid)); break;


            }

        }


        #endregion

        #region METHODS
        public void SetImage(VectorImage image)
        {
            background.style.display = image is null ? DisplayStyle.None : DisplayStyle.Flex;
            elementTag.style.backgroundImage = new StyleBackground(image);
        }

        private void SetColor(Color color)
        {
            background.style.backgroundColor = new StyleColor(color);
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

