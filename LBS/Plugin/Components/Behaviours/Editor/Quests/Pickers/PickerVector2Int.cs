using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class PickerVector2Int : PickerBase
    {

        public RectField _areaView;
        
        private static VisualTreeAsset visualTree;

        public Action<Rect> OnAreaChange;

        #region CONSTRUCTORS
        public PickerVector2Int() : base()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerVector2Int");
            visualTree.CloneTree(this);

            _areaView = this.Q<RectField>("Area");
            _areaView.tooltip = "an area in the level graph.";
            _areaView.RegisterValueChangedCallback((rect) =>
            {
                OnAreaChange.Invoke(rect.newValue);
            });

            BindPickButton();
        }

        #endregion

        #region METHODS


        public override void SetInfo(string label, string tooltip)
        {
            _areaView.labelElement.text = label;
            this.tooltip = tooltip;
        }

        protected override void PickerLogic()
        {
            if (ToolKit.Instance.GetActiveManipulatorInstance() is not QuestPicker pickerManipulator)
                return;
            pickerManipulator.ActiveType = QuestPickType.Position;
            pickerManipulator.ActiveData = GetActionData();
            if (pickerManipulator.ActiveData == null) return;

            pickerManipulator.OnPositionPicked = (pos) =>
            {
                var newRect = new Rect(
                    pos.x,
                    pos.y,
                    pickerManipulator.ActiveData.Area.width,
                    pickerManipulator.ActiveData.Area.height);
                SetArea(newRect);
            };
        }

        internal void SetArea(Rect area)
        {
            _areaView.value = area;
        }

        #endregion

    }
}