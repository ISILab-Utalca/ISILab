using ISILab.LBS.Components;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public abstract class NodeEditor : VisualElement
    {
        private QuestActionData _actionData;

        public virtual void SetNodeData(QuestActionData data)
        {
            _actionData = data;
        }

        protected QuestPicker AssignPickerData()
        {
            ToolKit.Instance.SetActive(typeof(QuestPicker));
            if (ToolKit.Instance.GetActiveManipulatorInstance() is not QuestPicker pickerManipulator) return null;

            pickerManipulator.ActiveData = _actionData;
            pickerManipulator.PickTriggerPosition = false;

            return pickerManipulator;
        }
    }

    public abstract class NodeEditor<T> : NodeEditor where T : QuestActionData
    {
        protected T NodeData;

        public override void SetNodeData(QuestActionData data)
        {
            base.SetNodeData(data);
            NodeData = data as T;
            if (NodeData is null) return;
            OnDataAssigned();
        }

        protected abstract void OnDataAssigned();
    }
}
