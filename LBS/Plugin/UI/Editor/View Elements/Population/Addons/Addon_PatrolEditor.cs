using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class Addon_PatrolEditor : LBSCustomEditor
    {
        private TileGroupBehavior behaviour;

        private LBSCustomToggleField PatrolLoop;
        private ListView PatrolPointsView;

        private static VisualTreeAsset visualTree { get; set; }

        public Addon_PatrolEditor(object target) : base(target)
        {
            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }
        public override void SetInfo(object paramTarget)
        {
            SetPatrolList();
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Addon_PatrolEditor", true);
            }

            visualTree.CloneTree(this);

            PatrolPointsView = this.Q<ListView>("PatrolPointsView");

            PatrolLoop = this.Q<LBSCustomToggleField>("PatrolLoop");
            PatrolLoop.RegisterValueChangedCallback(evt =>
            {
                if (behaviour?.SelectedTilemap is null) return;
                Addon_Patrol addonPatrol = behaviour.SelectedTilemap.GetAddon<Addon_Patrol>();
                if (addonPatrol is not null)
                    addonPatrol.Loops = evt.newValue;
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);

            });

            return this;
        }

        private void SetPatrolList()
        {
            if (behaviour.SelectedTilemap is null)
            {
                PatrolPointsView.Clear();
                PatrolLoop.SetValueWithoutNotify(false);
                return;
            }


            Addon_Patrol addonPatrol = behaviour.SelectedTilemap.GetAddon<Addon_Patrol>();
            if (addonPatrol is not null)

                PatrolLoop.SetValueWithoutNotify(addonPatrol.Loops);

            PatrolPointsView.itemsSource = addonPatrol.Points;


            // Create new item
            PatrolPointsView.makeItem = () =>
            {
                var vecField = new Vector2Field();
                vecField.style.flexGrow = 1;
                vecField.style.marginLeft = 8;
                vecField.style.marginRight = 0;
                vecField.style.justifyContent = Justify.Center;
                vecField.style.alignItems = Align.Center;

                //UpdateSelectedTilemap();

                return vecField;
            };

            // Bind item to patrol.Points[index]
            PatrolPointsView.bindItem = (ve, index) =>
            {
                var vecField = ve as Vector2Field;
                if (index < 0 || index >= addonPatrol.Points.Count) return;

                // Apply value without triggering callback
                vecField.SetValueWithoutNotify(addonPatrol.Points[index]);

                // Register fresh callback
                vecField.RegisterValueChangedCallback((_vector) =>
                {
                    addonPatrol.Points[index] = _vector.newValue;
                    PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                    DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                    //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
                });


                // UpdateSelectedTilemap();
            };

            // Add new point
            PatrolPointsView.onAdd = (list) =>
            {
                addonPatrol.Points.Add(behaviour.SelectedTilemap.GetBounds().position);
                PatrolPointsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
            };

            // Remove selected point
            PatrolPointsView.onRemove = (list) =>
            {
                int index = PatrolPointsView.selectedIndex;
                if (index < 0 || index >= addonPatrol.Points.Count) return;

                addonPatrol.Points.RemoveAt(index);
                PatrolPointsView.Rebuild();
                PopulationTileGroupView.UpdateVisuals(behaviour.SelectedTilemap);
                DrawManager.Instance.DrawSingleComponent(behaviour, behaviour.OwnerLayer);
                //DrawManager.Instance.RedrawLayer(behaviour.OwnerLayer);
            };

        }
    }
}
