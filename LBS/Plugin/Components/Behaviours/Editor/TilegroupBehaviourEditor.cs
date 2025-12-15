using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using System.Collections.Generic;
using UnityEngine;
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
            if (visualTree is null)
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
            PatrolLoop.RegisterValueChangedCallback(evt =>
            {
                if (behaviour?.SelectedTilemap is null) return;
                behaviour.SelectedTilemap.Addons.patrol.Loop = evt.newValue;
                UpdateSelectedTilemap();

            });

            return this;
        }

        private void UpdateSelectedTilemap()
        {
            PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
            DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
        }

        private void SetAddonsList()
        {

            if (behaviour.SelectedTilemap is null)
            {
                AddonsView.Clear();
                return;
            }

            BundleTileMapAddons addons = behaviour.SelectedTilemap.Addons;
            List<TileTrigger> triggers = addons.triggers;

            AddonsView.itemsSource = triggers;

            // Create new item
            AddonsView.makeItem = () =>
            {
                TilegroupTriggerView entry = new TilegroupTriggerView();
                entry.style.flexGrow = 1;
                entry.style.marginLeft = 8;
                entry.style.marginRight = 0;
                entry.style.justifyContent = Justify.Center;
                entry.style.alignItems = Align.Center;

                UpdateSelectedTilemap();
                return entry;
            };

            // Bind item
            AddonsView.bindItem = (ve, index) =>
            {
                TilegroupTriggerView entry = ve as TilegroupTriggerView;
                if (index < 0 || index >= triggers.Count) return;

                entry.Trigger = triggers[index];

                entry.OnTriggerTypeChanged = (newType) =>
                {
                    triggers[index].Ttype = newType;
                    UpdateSelectedTilemap();


                };

                UpdateSelectedTilemap();
            };


            // Add new point
            AddonsView.onAdd = (list) =>
            {
                triggers.Add(new TileTrigger());
                AddonsView.Rebuild();
                UpdateSelectedTilemap();
            };

            // Remove selected point
            AddonsView.onRemove = (list) =>
            {
                int index = AddonsView.selectedIndex;
                if (index < 0 || index >= triggers.Count) return;

                triggers.RemoveAt(index);
                AddonsView.Rebuild();
                UpdateSelectedTilemap();
            };
        }

        private void SetPatrolList()
        {
            if (behaviour.SelectedTilemap is null)
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
                vecField.style.marginLeft = 8;
                vecField.style.marginRight = 0;
                vecField.style.justifyContent = Justify.Center;
                vecField.style.alignItems = Align.Center;

                UpdateSelectedTilemap();

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
                vecField.RegisterValueChangedCallback((_vector) =>
                {
                    patrol.Points[index] = _vector.newValue;
                    UpdateSelectedTilemap();

                });


                UpdateSelectedTilemap();
            };

            // Add new point
            PatrolPointsView.onAdd = (list) =>
            {
                patrol.Points.Add(behaviour.SelectedTilemap.GetBounds().position);
                PatrolPointsView.Rebuild();
                UpdateSelectedTilemap();
            };

            // Remove selected point
            PatrolPointsView.onRemove = (list) =>
            {
                int index = PatrolPointsView.selectedIndex;
                if (index < 0 || index >= patrol.Points.Count) return;

                patrol.Points.RemoveAt(index);
                PatrolPointsView.Rebuild();
                UpdateSelectedTilemap();
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


            SetPatrolList();
            SetAddonsList();

        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        #endregion
    }
}
