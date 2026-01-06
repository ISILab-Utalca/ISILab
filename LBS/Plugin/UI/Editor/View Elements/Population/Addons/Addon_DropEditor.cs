using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using LBS.Components;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class Addon_DropEditor : LBSCustomEditor, IBundleFilter
    {
        private TileGroupBehavior behaviour;
        private LBSCustomObjectField DropObjectField;

        public LBSButtonListFilter BundlePickerWindow { get; set; }
        private static VisualTreeAsset visualTree { get; set; }

        public Addon_DropEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        public override void SetInfo(object paramTarget)
        {
            if (behaviour.SelectedTilemap is null) return;

            var drop = behaviour.SelectedTilemap.GetAddon<Addon_Drop>();
            if (drop is null) return;
            if (drop.OnDestroyDrop is null) return;

            DropObjectField.SetValueWithoutNotify(drop.OnDestroyDrop);
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_DropEditor", true);
            }

            visualTree.CloneTree(this);

            DropObjectField = this.Q<LBSCustomObjectField>("DropObjectField");
            DropObjectField.UseCustomFilter = true;
            DropObjectField.CustomFilter = pick =>
            {
                List<BundleFlags> flags = new List<BundleFlags>() { BundleFlags.Population };
                var bundles = BundleQueryUtility.FindBundlesWithFlag(flags);
                (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
            };

            DropObjectField.RegisterValueChangedCallback(evt =>
            {
                if (behaviour?.SelectedTilemap is null) return;
                Addon_Drop addonDrop = behaviour.SelectedTilemap.GetAddon<Addon_Drop>();
                if (addonDrop is not null)
                    addonDrop.OnDestroyDrop = DropObjectField.value as Bundle;
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
            });

            return this;
        }
    }
}
