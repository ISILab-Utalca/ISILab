using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.UI.Editor;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using ISILab.LBS.VisualElements.Editor;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("TileGroupBehavior", typeof(TileGroupBehavior))]
    public class TileGroupBehaviorEditor : LBSCustomEditor, IToolProvider
    {
        #region FIELDS

        private TileGroupBehavior behaviour;
        private ConnectionPicker pickConnection;

        #endregion

        #region VIEW FIELD
        private VisualElement NoContent;
        private VisualElement Content;

        private LBSCustomLabelIcon SelectedHeader;
        public VisualElement AddonContainer;


        private static VisualTreeAsset visualTree { get; set; }


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
            behaviour.OnSelectedChanged -= OnSelectedChanged;
            behaviour.OnSelectedChanged += OnSelectedChanged;
            UpdateTilebundle(behaviour.SelectedTilemap);
        }

        private void OnSelectedChanged(TileBundleGroup tilemap)
        {
            DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
            UpdateTilebundle(tilemap);
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
            AddonContainer = this.Q<VisualElement>("AddonContainer");

            return this;
        }

        private void UpdateSelectedTilemap()
        {
            PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
            DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
            //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
        }

        private void UpdateTilebundle(TileBundleGroup TileBundleGroup)
        {
            AddonContainer.Clear();
            NoContent.style.display = DisplayStyle.Flex;
            Content.style.display = DisplayStyle.None;

            if (TileBundleGroup is null) return;

            NoContent.style.display = DisplayStyle.None;
            Content.style.display = DisplayStyle.Flex;
            SelectedHeader.Icon = TileBundleGroup.BundleData.Bundle.Icon;
            SelectedHeader.Label = TileBundleGroup.BundleData.BundleName;

            Addon_Trigger atrigger = TileBundleGroup.GetAddon<Addon_Trigger>();
            Addon_Patrol apatrol = TileBundleGroup.GetAddon<Addon_Patrol>();
            Addon_Destruct adestruct = TileBundleGroup.GetAddon<Addon_Destruct>();
            Addon_Interact ainteract = TileBundleGroup.GetAddon<Addon_Interact>();
            Addon_Drop adrop = TileBundleGroup.GetAddon<Addon_Drop>();
            Addon_Unlock aunlock = TileBundleGroup.GetAddon<Addon_Unlock>();
            Addon_TriggerUnlock atunlock = TileBundleGroup.GetAddon<Addon_TriggerUnlock>();

            if (atrigger is not null)
            {
                Addon_TriggerEditor NewEntry = new Addon_TriggerEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (apatrol is not null)
            {
                Addon_PatrolEditor NewEntry = new Addon_PatrolEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (adestruct is not null)
            {
                Addon_DestroyEditor NewEntry = new Addon_DestroyEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (ainteract is not null)
            {
                Addon_InteractEditor NewEntry = new Addon_InteractEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (adrop is not null)
            {
                Addon_DropEditor NewEntry = new Addon_DropEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (aunlock is not null)
            {
                Addon_UnlockEditor NewEntry = new Addon_UnlockEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }
            if (atunlock is not null)
            {
                Addon_TriggerUnlockEditor NewEntry = new Addon_TriggerUnlockEditor(behaviour);
                AddonContainer.Add(NewEntry);
            }

        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        public void SetTools(ToolKit toolkit)
        {
            pickConnection = new ConnectionPicker();
            LBSTool t1 = new LBSTool(pickConnection);
            t1.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            toolkit.ActivateTool(t1, behaviour.OwnerLayer, behaviour);                    

            // context exclusive from the Tilemap Panel
            VisualElement toolButton = toolkit.GetToolButton(typeof(ConnectionPicker));
            toolButton.SetEnabled(false);
        }

        #endregion
    }
}
