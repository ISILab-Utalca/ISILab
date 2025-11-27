using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Modules;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("PopulationTileBehaviour", typeof(PopulationTileGroupBehavior))]
    public class PopulationTileGroupBehaviorEditor : LBSCustomEditor
    {
        #region FIELDS

        private PopulationTileGroupBehavior behaviour;

        #endregion

        #region VIEW FIELD
        private VisualElement NoContent;
        private VisualElement Content;

   
        private LBSCustomLabelIcon SelectedHeader;
        private ListView AddonsView;

        #endregion

        #region CONSTRUCTORS
        public PopulationTileGroupBehaviorEditor(object target) : base(target)
        {
            behaviour = target as PopulationTileGroupBehavior;
            if (behaviour is null) return;
  
            behaviour.OnSelectedChanged += UpdateTilebundle;
            SetInfo(behaviour);
            CreateVisualElement();
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as PopulationTileGroupBehavior;
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationTileGroupBehaviorEditor", true);
            visualTree.CloneTree(this);

            NoContent = this.Q<VisualElement>("NoContent");
            Content = this.Q<VisualElement>("Content");

            SelectedHeader = this.Q<LBSCustomLabelIcon>("SelectedHeader");

            AddonsView = this.Q<ListView>("AddonsView");

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


        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        #endregion
    }
}