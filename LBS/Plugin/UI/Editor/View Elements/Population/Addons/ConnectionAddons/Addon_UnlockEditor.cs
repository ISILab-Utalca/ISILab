using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class Addon_UnlockEditor : LBSCustomEditor
    {
        private TileGroupBehavior behaviour;
        private AddonConnectionView acv;

        private static VisualTreeAsset visualTree { get; set; }

        public Addon_UnlockEditor(object target): base(target) 
        {
            CreateVisualElement();
            SetInfo(behaviour);
        }

        public override void SetInfo(object paramTarget)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            acv.SetInfo(behaviour.SelectedTilemap.GetAddon<Addon_Unlock>());
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_UnlockEditor", true);
            }

            visualTree.CloneTree(this);
            acv = this.Q<AddonConnectionView>("ConnectionView");
            return this;
        }
    }
}
