using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("BSP Dungeon Generator", typeof(BSPDungeonAssistant))]
    public class BSPDungeonAssistantEditor : LBSCustomEditor
    {
        BSPDungeonAssistant assistant;
        LBSCustomVector2IntField sizeField;
        LBSCustomIntField minPartitionField;
        LBSCustomIntField minRoomField;

        public BSPDungeonAssistantEditor(object target) : base(target)
        {
            assistant = (BSPDungeonAssistant)target;
            CreateVisualElement();
        }


        public override void SetInfo(object paramTarget)
        {
            assistant = paramTarget as BSPDungeonAssistant;
            SetFieldsInfo();
        }

        protected override VisualElement CreateVisualElement()
        {
            var button = new LBSCustomButton() { text = "Run" };
            button.RegisterCallback<ClickEvent>(_evt => assistant.Run());
            this.Add(button);

            sizeField = new LBSCustomVector2IntField();
            sizeField.RegisterValueChangedCallback(val =>
            {
                assistant.mapWidth = val.newValue.x;
                assistant.mapHeight = val.newValue.y;
            });

            minPartitionField = new LBSCustomIntField();
            minPartitionField.RegisterValueChangedCallback(val =>
            {
                assistant.minPartitionSize = val.newValue;
            });

            minRoomField = new LBSCustomIntField();
            minRoomField.RegisterValueChangedCallback(val =>
            {
                assistant.minRoomSize = val.newValue;
            });

            this.Add(sizeField);
            this.Add(minPartitionField);
            this.Add(minRoomField);

            SetFieldsInfo();
            return this;
        }

        private void SetFieldsInfo()
        {
            // Size
            sizeField.label = "Size";
            sizeField.value = new Vector2Int(assistant.mapWidth, assistant.mapHeight);

            // Min Partition Size
            minPartitionField.label = "Minimum Partition Size";
            minPartitionField.value = assistant.minPartitionSize;

            // Min Room Size
            minRoomField.label = "Minimum Room Size";
            minRoomField.value = assistant.minRoomSize;
        }
    }
}
