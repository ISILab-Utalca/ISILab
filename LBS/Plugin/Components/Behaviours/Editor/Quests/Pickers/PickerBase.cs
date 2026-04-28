using ISILab.LBS.CustomComponents;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using System;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{

    public abstract class PickerBase : VisualElement
    {
        private static Button _activeButton;

        protected LBSCustomObjectField ObjectField;
        protected Button PickButton;

        public Action OnClicked;

        protected void BindCommonButton()
        {
            PickButton.clicked += () =>
            {
                ActivateButton(PickButton);
                OnClicked?.Invoke();
            };
        }

        protected void ActivateButton(Button button)
        {
            if(_activeButton != null) 
            _activeButton = button;
        }

        protected static void ActivateQuestPicker()
        {
            ToolKit.Instance.SetActive(typeof(QuestPicker));

            if (ToolKit.Instance.GetActiveManipulator() is QuestPicker qp)
                qp.PickTriggerPosition = false;
        }

        public void ClearPicker()
        {
            OnClicked = null;
        }
    }
}