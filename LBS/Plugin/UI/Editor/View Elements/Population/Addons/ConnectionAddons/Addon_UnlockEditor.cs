using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
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
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);

        }
        public override void SetInfo(object paramTarget)
        {
            SetList();
        }

        private void SetList()
        {
            acv.SetInfo(behaviour.SelectedTilemap);
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
