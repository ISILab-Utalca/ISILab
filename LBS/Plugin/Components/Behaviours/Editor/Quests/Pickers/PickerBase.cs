using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using System;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{

    public abstract class PickerBase : VisualElement
    {
        private static ToolButton _activeButton;

        private ToolButton pickButton;

        public ToolButton PickButton
        {
            get => pickButton;

            set
            {
                _activeButton?.SetValueWithoutNotify(false);
                _activeButton = value;
                _activeButton.OnFocus();
            }
        }

        protected bool IsSelected()
        {
            return PickButton == _activeButton;
        }

        protected void BindPickButton()
        {
            pickButton = this.Q<ToolButton>();
            pickButton.AddGroupEvent(() =>
                {
                    PickButton = pickButton;
                    ToolKit.Instance.SetActive(typeof(QuestPicker));
                    if (IsSelected()) PickerLogic();
                }
            );
        }


        protected abstract void PickerLogic();
        public abstract void SetInfo(string name, string tooltip);
        protected QuestNodeData GetActionData()
        {
            var layer = LBSMainWindow.Instance._selectedLayer;
            if (layer == null) return null;
            var ndb = layer.GetBehaviour<NodeDataBehaviour>();
            if (ndb == null) return null;
            return ndb.SelectedNodeData;
        }
    }
}