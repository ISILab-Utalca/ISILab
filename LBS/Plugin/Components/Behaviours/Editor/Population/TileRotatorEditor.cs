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
            public LBSCustomUnsignedIntegerField WeightField;
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
            LBSToolbarToggle toggle = this.Q<LBSToolbarToggle>(toggleName);
            LBSCustomUnsignedIntegerField weight = this.Q<LBSCustomUnsignedIntegerField>(weightName);

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

            bool weighted = behaviour.TileRotationMode == TileMakeRot.Weighted;
            bool random = behaviour.TileRotationMode == TileMakeRot.Random;
            bool fix = behaviour.TileRotationMode == TileMakeRot.Fixed;
            foreach ((string dir, DirToRotVisualElement ves) in DirectionVes)
            {
                if(fix) ves.Toggle.SetValueWithoutNotify(behaviour.ActiveRotationDirection == dir);
                if (random) ves.Toggle.SetValueWithoutNotify(true);

                ves.WeightField.style.display = weighted ? DisplayStyle.Flex : DisplayStyle.None;
                ves.Toggle.SetEnabled(!weighted);
            }

            if(weighted) RefreshWeights();
        }

        private void RefreshWeights()
        {
            if (behaviour is null) return;

            foreach ((string dir, DirToRotVisualElement ves) in DirectionVes)
            {
                ves.WeightField.SetValueWithoutNotify((uint)behaviour.GetDirectionWeight(dir));
                ves.Toggle.SetValueWithoutNotify(false);
            }
        }

        private void SelectDirection(string direction)
        {
            if (behaviour is null) return;
            if (string.IsNullOrEmpty(direction)) return;
            if (behaviour.TileRotationMode == TileMakeRot.Fixed)
            {
                // set the new direction (value to 1) all other directions value to 0
                behaviour.ActiveRotationDirection = direction;

                foreach (KeyValuePair<string, DirToRotVisualElement> entry in DirectionVes)
                {
                    entry.Value.Toggle.SetValueWithoutNotify(direction == entry.Key);
                }
            }
            else if(behaviour.TileRotationMode == TileMakeRot.Random)
            {
                // if the toggle is on, weight is 1, else 0.
                foreach (KeyValuePair<string, DirToRotVisualElement> entry in DirectionVes)
                {
                    var value = entry.Value.Toggle.value ? 1f : 0f;
                    behaviour.SetDirectionWeight(entry.Key,value);
                }

            }

        }
    }
}
