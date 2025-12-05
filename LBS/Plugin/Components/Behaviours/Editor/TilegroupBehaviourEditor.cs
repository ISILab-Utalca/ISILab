using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements.Editor;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
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

        private VisualElement Patrol;
        private VisualElement Trigger;

        private ListView PatrolPointsView;
        private ListView AddonsView;

        private LBSCustomToggleField PatrolLoop;

        public VisualTreeAsset visualTree { get; private set; }

        #endregion

        #region CONSTRUCTORS
        public TileGroupBehaviorEditor(object target) : base(target)
        {
   
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        #endregion
        
        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as TileGroupBehavior;
            behaviour.OnSelectedChanged += UpdateTilebundle;
            behaviour.OnSelectedChanged += (tilemap) =>
            {
                DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
            };
            UpdateTilebundle(null);
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            if(visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TileGroupBehaviorEditor", true);
            }
          
            visualTree.CloneTree(this);

            NoContent = this.Q<VisualElement>("NoContent");
            Content = this.Q<VisualElement>("Content");
            
            SelectedHeader = this.Q<LBSCustomLabelIcon>("SelectedHeader");

            Patrol = this.Q<VisualElement>("Patrol");
            Trigger = this.Q<VisualElement>("Triggers");

            AddonsView = this.Q<ListView>("AddonsView");
            PatrolPointsView = this.Q<ListView>("PatrolPointsView");

            PatrolLoop = this.Q<LBSCustomToggleField>("PatrolLoop");
            PatrolLoop.RegisterValueChangedCallback(evt => {
                if (behaviour?.SelectedTilemap is null) return;
                BundleTileMapAddons addons = behaviour.SelectedTilemap.Addons;
                TilePatrol patrol = addons.patrol;
                patrol.Loop = evt.newValue;
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
            });
                 
            return this;
        }

        private void SetAddonsList()
        {
            //throw new NotImplementedException();
        }

        private void UpdatePatrolList()
        {

            if(behaviour.SelectedTilemap is null)
            {
                PatrolPointsView.Clear();
                PatrolLoop.SetValueWithoutNotify(false);
                return;
            }

            BundleTileMapAddons addons = behaviour.SelectedTilemap.Addons;
            TilePatrol patrol = addons.patrol;

            PatrolLoop.SetValueWithoutNotify(patrol.Loop);

            PatrolPointsView.itemsSource = patrol.Points;

            // Create new item
            PatrolPointsView.makeItem = () =>
            {
                var vecField = new Vector2Field();
                vecField.style.flexGrow = 1;
                vecField.style.marginLeft = 4;
                vecField.style.marginRight = 4;
                vecField.style.justifyContent = Justify.Center;
                vecField.style.alignItems = Align.Center;
                return vecField;
            };

            // Bind item to patrol.Points[index]
            PatrolPointsView.bindItem = (ve, index) =>
            {
                var vecField = ve as Vector2Field;
                if (index < 0 || index >= patrol.Points.Count) return;

                // Apply value without triggering callback
                vecField.SetValueWithoutNotify(patrol.Points[index]);

                // Register fresh callback
                vecField.RegisterValueChangedCallback((_vector )=> 
                {
                    patrol.Points[index] = _vector.newValue;
                 //   EditorUtility.SetDirty(behaviour);

                    PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);

                });

         
            };

            // Add new point
            PatrolPointsView.onAdd = (list) =>
            {
                patrol.Points.Add(new Vector2(0, 0));
                PatrolPointsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                //    EditorUtility.SetDirty(behaviour);
            };

            // Remove selected point
            PatrolPointsView.onRemove = (list) =>
            {
                int index = PatrolPointsView.selectedIndex;
                if (index < 0 || index >= patrol.Points.Count) return;

                patrol.Points.RemoveAt(index);
                PatrolPointsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                //    EditorUtility.SetDirty(behaviour);
            };

        }


        private void UpdateTilebundle(TileBundleGroup TileBundleGroup)
        {

           
            // Set init options
            if (TileBundleGroup is null)
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

            UpdatePatrolList();

        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        #endregion
    }
}