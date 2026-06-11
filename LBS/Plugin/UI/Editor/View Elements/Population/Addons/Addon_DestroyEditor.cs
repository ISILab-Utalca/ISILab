using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Addon_Destruct", typeof(Addon_Destruct))]
    public class Addon_DestroyEditor : LBSCustomEditor
    {
        private TileGroupBehavior behaviour;
        private LBSCustomEventHooker DestroyedHook;

        private static VisualTreeAsset visualTree;

        public Addon_DestroyEditor(object target): base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        public override void SetInfo(object paramTarget)
        {
            Addon_Destruct addonDestruct = behaviour.SelectedTilemap.GetAddon<Addon_Destruct>();
            if (addonDestruct is not null)
                DestroyedHook.Hooker = addonDestruct.Destroyed;
        }

        protected override VisualElement CreateVisualElement()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_DestroyEditor", true);


            visualTree.CloneTree(this);

            DestroyedHook = this.Q<LBSCustomEventHooker>("DestroyedHook");
            DestroyedHook.EventType = LBSEventType.Destroy;
            return this;
        }
    }
}
