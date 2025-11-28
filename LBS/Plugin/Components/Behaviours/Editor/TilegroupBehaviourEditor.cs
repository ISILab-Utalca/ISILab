using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("TileGroupBehavior", typeof(TileGroupBehavior))]
    public class TileGroupBehaviorEditor : LBSCustomEditor
    {
        #region FIELDS

        private TileGroupBehavior behaviour;

        #endregion

        #region VIEW FIELD
        private VisualElement NoContent;
        private VisualElement Content;

        private LBSCustomLabelIcon SelectedHeader;

        private VisualElement Tier;
        private VisualElement Patrol;
        private VisualElement Trigger;

        private ListView PatrolPointsView;
        private ListView AddonsView;

        #endregion

        #region CONSTRUCTORS
        public TileGroupBehaviorEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;
  
            behaviour.OnSelectedChanged += UpdateTilebundle;
            SetInfo(behaviour);
            CreateVisualElement();
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as TileGroupBehavior;
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TileGroupBehaviorEditor", true);
            visualTree.CloneTree(this);

            NoContent = this.Q<VisualElement>("NoContent");
            Content = this.Q<VisualElement>("Content");
            
            SelectedHeader = this.Q<LBSCustomLabelIcon>("SelectedHeader");

            Tier = this.Q<VisualElement>("Tier");
            Patrol = this.Q<VisualElement>("Patrol");
            Trigger = this.Q<VisualElement>("Triggers");

            AddonsView = this.Q<ListView>("AddonsView");
            PatrolPointsView = this.Q<ListView>("PatrolPointsView");

            UpdateTilebundle(null);
     
            return this;
        }

        private void UpdateTilebundle(TileBundleGroup TileBundleGroup)
        {
            // Set init options
            if(TileBundleGroup is null)
            {
                NoContent.style.display = DisplayStyle.Flex;
                Content.style.display = DisplayStyle.None;
                return;
            }

            NoContent.style.display = DisplayStyle.None;
            Content.style.display = DisplayStyle.Flex;
            SelectedHeader.Icon = TileBundleGroup.BundleData.Bundle.Icon;
            SelectedHeader.Label = TileBundleGroup.BundleData.BundleName;

            Bundle bundle = TileBundleGroup.BundleData.Bundle;

            DisplayStyle triggerDisplay = bundle.GetHasTagCharacteristic("LBSTag_Triggers") ? DisplayStyle.Flex : DisplayStyle.None;
            Trigger.style.display = triggerDisplay;
            DisplayStyle patrolDisplay = bundle.GetHasTagCharacteristic("LBSTag_Patrol") ? DisplayStyle.Flex : DisplayStyle.None;
            Patrol.style.display = patrolDisplay;
            DisplayStyle tierDisplay = bundle.GetHasTagCharacteristic("LBSTag_Tier") ? DisplayStyle.Flex : DisplayStyle.None;
            Tier.style.display = tierDisplay;
        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        #endregion
    }
}