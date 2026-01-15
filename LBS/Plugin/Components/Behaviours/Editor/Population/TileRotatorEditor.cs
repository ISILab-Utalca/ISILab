using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Behaviours.PopulationBehaviour;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class TileRotatorEditor : LBSCustomEditor
    {
        static VisualTreeAsset VisualTree;

        private PopulationBehaviour behaviour;

        private VisualElement rotateSelector;
        private LBSCustomEnumField tileMakeRotEnum;

        private readonly Dictionary<string, DirToRotVisualElement> DirectionVes = new();

        private class DirToRotVisualElement
        {
            public LBSToolbarToggle Toggle;
            public LBSCustomFloatField WeightField;
        }

        public TileRotatorEditor()
        {
            CreateVisualElement();
        }

        public override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as PopulationBehaviour;
            SelectDirection(behaviour.ActiveRotationDirection);
            tileMakeRotEnum.SetValueWithoutNotify(behaviour.TileRotationMode);

            Refresh();

        }

        protected override VisualElement CreateVisualElement()
        {
            VisualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("TileRotatorEditor");
            VisualTree.CloneTree(this);

            rotateSelector = this.Q<VisualElement>("rotateSelector");
            tileMakeRotEnum = this.Q<LBSCustomEnumField>("tileMakeRotEnum");

            RegisterDirection(LBSDirection.Up, "tUp", "UpW");
            RegisterDirection(LBSDirection.Left, "tLeft", "LeftW");
            RegisterDirection(LBSDirection.Right, "tRight", "RightW");
            RegisterDirection(LBSDirection.Down, "tDown", "DownW");

            tileMakeRotEnum.RegisterValueChangedCallback(evt =>
            {
                behaviour.TileRotationMode = (TileMakeRot)evt.newValue;
                Refresh();
            });


            return this;
        }

        private void RegisterDirection(string dir, string toggleName, string weightName)
        {
            var toggle = this.Q<LBSToolbarToggle>(toggleName);
            var weight = this.Q<LBSCustomFloatField>(weightName);

            DirectionVes[dir] = new DirToRotVisualElement
            {
                Toggle = toggle,
                WeightField = weight
            };

            toggle.RegisterCallback<ClickEvent>(_ => SelectDirection(dir));

            weight.RegisterValueChangedCallback(evt =>
                behaviour.SetDirectionWeight(dir, evt.newValue)
            );
        }

        private void Refresh()
        {
            if (behaviour is null) return;

            bool showSelector = behaviour.TileRotationMode != TileMakeRot.Random;
            bool showWeights = behaviour.TileRotationMode == TileMakeRot.Weighted;

            rotateSelector.style.display = showSelector ? DisplayStyle.Flex : DisplayStyle.None;
            if (showSelector)
            {
                foreach ((string dir, DirToRotVisualElement ves) in DirectionVes)
                {
                    ves.Toggle.SetValueWithoutNotify(behaviour.ActiveRotationDirection == dir);
                }
            }

            foreach (var ves in DirectionVes.Values) 
            { 
                ves.WeightField.style.display = showWeights ? DisplayStyle.Flex : DisplayStyle.None;
                ves.Toggle.SetEnabled(behaviour.TileRotationMode == TileMakeRot.Fixed); 
            }

            if(showWeights) RefreshWeights();
        }

        private void RefreshWeights()
        {
            if (behaviour is null) return;

            foreach ((string dir, DirToRotVisualElement ves) in DirectionVes)
            {
                ves.WeightField.SetValueWithoutNotify(behaviour.GetDirectionWeight(dir));
                ves.Toggle.SetValueWithoutNotify(false);
            }
        }

        private void SelectDirection(string direction)
        {
            if (behaviour is null) return;

            behaviour.ActiveRotationDirection = direction;

            foreach (var ui in DirectionVes.Values)
                ui.Toggle.SetValueWithoutNotify(false);

            if (DirectionVes.TryGetValue(direction, out var selected))
                selected.Toggle.SetValueWithoutNotify(true);
        }
    }
}
