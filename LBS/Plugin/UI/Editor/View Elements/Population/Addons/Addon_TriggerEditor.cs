using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Data;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class Addon_TriggerEditor : LBSCustomEditor
    {
        private TileGroupBehavior behaviour;

        private ListView AddonsView;

        private static VisualTreeAsset visualTree { get; set; }

        public Addon_TriggerEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        public override void SetInfo(object paramTarget)
        {
            SetTriggerList();
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_TriggerEditor", true);
            }

            visualTree.CloneTree(this);
            AddonsView = this.Q<ListView>("AddonsView");


            return this;
        }


        private void SetTriggerList()
        {

            if (behaviour.SelectedTilemap is null)
            {
                AddonsView.Clear();
                return;
            }

            Addon_Trigger addonTrigger = behaviour.SelectedTilemap.GetAddon<Addon_Trigger>();
            if (addonTrigger is null) return;

            List<TileTrigger> triggers = addonTrigger.Triggers;

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

                //  UpdateSelectedTilemap();
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
                    PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                    DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                    //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer); //Dibujar solo el behaviour
                };

                //UpdateSelectedTilemap();
            };


            // Add new point
            AddonsView.onAdd = (list) =>
            {
                triggers.Add(new TileTrigger());
                AddonsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
            };

            // Remove selected point
            AddonsView.onRemove = (list) =>
            {
                int index = AddonsView.selectedIndex;
                if (index < 0 || index >= triggers.Count) return;

                triggers.RemoveAt(index);
                AddonsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
            };
        }

    }
}
