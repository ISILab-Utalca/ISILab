using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;


namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class Addon_TriggerUnlockEditor : LBSCustomEditor
    {
        #region VIEW FIELDS
        LBSCustomListView triggerUnlockList;
        private static VisualTreeAsset visualTree { get; set; }
        #endregion

        private TileGroupBehavior behaviour;
        private Addon_TriggerUnlock addon;

        public Addon_TriggerUnlockEditor() { }

        public Addon_TriggerUnlockEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);

        }
        public override void SetInfo(object paramTarget)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            if(behaviour.SelectedTilemap is null) return;

            behaviour.OnSelectedChanged += (newTile) =>
            {
                addon = newTile?.GetAddon<Addon_TriggerUnlock>();
                if (addon is null) return;
                SetList();
            };

            addon = behaviour.SelectedTilemap.GetAddon<Addon_TriggerUnlock>();
            if (addon is null) return;
            SetList();

        }

        private void SetList()
        {
            if (addon is null) return;

            triggerUnlockList.itemsSource = addon.Connections;

            triggerUnlockList.makeItem = () =>
            {
                return new AddonTriggerConnectionView();
            };

            triggerUnlockList.onAdd = listView =>
            {
                addon.Connections.Add(new TriggerUnlockEntry());
                triggerUnlockList.RefreshItems();
            };

            triggerUnlockList.bindItem = (element, index) =>
            {
                AddonTriggerConnectionView view = element as AddonTriggerConnectionView;    
                TriggerUnlockEntry entry = addon.Connections[index];
                view.Entry = entry;
            };

            triggerUnlockList.RefreshItems();
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_TriggerUnlockEditor", true);
            }

            visualTree.CloneTree(this);
            triggerUnlockList = this.Q<LBSCustomListView>("TriggerUnlockList");




            return this;
        }

    }
}
