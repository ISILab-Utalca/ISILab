using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class PickerVector2Int : PickerBase
    {

        public RectField areaView;

        private static VisualTreeAsset visualTree;

        #region CONSTRUCTORS
        public PickerVector2Int() : base()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerVector2Int");
            visualTree.CloneTree(this);

            areaView = this.Q<RectField>("Area");
            areaView.tooltip = "an area in the level graph.";
            areaView.RegisterValueChangedCallback<Rect>((rect) =>
            {
                var activeData = GetActionData();
                if (activeData == null) return;
                activeData.SetArea(rect.newValue);

            });

            BindPickButton();
        }

        #endregion

        #region METHODS


        public override void SetInfo(string label, string tooltip)
        {
            areaView.labelElement.text = label;
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

                areaView.value = newRect;
            };
        }

        #endregion

    }
}