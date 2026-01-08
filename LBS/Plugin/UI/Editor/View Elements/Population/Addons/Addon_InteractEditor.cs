using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class Addon_InteractEditor : LBSCustomEditor
    {
        private TileGroupBehavior behaviour;
        private LBSCustomEventHooker InteractHook;

        private static VisualTreeAsset visualTree { get; set; }

        public Addon_InteractEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        public override void SetInfo(object paramTarget)
        {
            Addon_Interact addonInteract = behaviour.SelectedTilemap.GetAddon<Addon_Interact>();
            if (addonInteract is not null)
                InteractHook.Hooker = addonInteract.Interact;
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_InteractEditor", true);
            }

            visualTree.CloneTree(this);

            InteractHook = this.Q<LBSCustomEventHooker>("InteractHook");
            InteractHook.EventType = LBSEventType.Interact;
            return this;
        }
    }
}
