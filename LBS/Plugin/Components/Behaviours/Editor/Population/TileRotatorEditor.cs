using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
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

        private VisualElement rotateSelector;
        private LBSCustomEnumField tileMakeRotEnum;
        private PopulationBehaviour behaviour;
        private Dictionary<string, LBSToolbarToggle> toggles;
        private readonly Dictionary<string, string> directionButtons = new()
        {
            { LBSDirection.Up,    "tUp" },
            { LBSDirection.Left,  "tLeft" },
            { LBSDirection.Right, "tRight" },
            { LBSDirection.Down,  "tDown" },
        };

        public TileRotatorEditor()
        {
            CreateVisualElement();
        }

        public override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as PopulationBehaviour;
            SelectDirection(behaviour.ActiveRotationDirection, toggles);
        }

        protected override VisualElement CreateVisualElement()
        {
            VisualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("TileRotatorEditor");
            VisualTree.CloneTree(this);


            rotateSelector = this.Q<VisualElement>("rotateSelector");
            tileMakeRotEnum = this.Q<LBSCustomEnumField>("tileMakeRotEnum");

            tileMakeRotEnum.RegisterValueChangedCallback(evt =>
            {
                behaviour.TileRotationMode = (TileMakeRot)evt.newValue;

                switch (behaviour.TileRotationMode)
                {
                    case TileMakeRot.Fixed:
                        rotateSelector.style.display = DisplayStyle.Flex;
                        break;

                    case TileMakeRot.Random:
                        rotateSelector.style.display = DisplayStyle.None;
                        break;

                    case TileMakeRot.Weighted:
                        rotateSelector.style.display = DisplayStyle.Flex;
                        break;
                }
            });

            toggles = new Dictionary<string, LBSToolbarToggle>();

            foreach (var (direction, name) in directionButtons)
            {
                var toggle = this.Q<LBSToolbarToggle>(name);
                toggles[direction] = toggle;

                toggle.RegisterCallback<ClickEvent>(_ =>
                    SelectDirection(direction, toggles)
                );
            }

            return this;
        }



        private void SelectDirection(
            string direction,
            Dictionary<string, LBSToolbarToggle> toggles)
        {
            behaviour.ActiveRotationDirection = direction;

            if (!toggles.ContainsKey(direction)) return;

            foreach (var toggle in toggles.Values)
                toggle.SetValueWithoutNotify(false);

            toggles[direction].SetValueWithoutNotify(true);
        }
    }
}
