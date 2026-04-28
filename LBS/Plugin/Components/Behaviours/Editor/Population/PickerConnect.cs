using System;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using LBS.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class PickerConnect : PickerBase
    {   
        private readonly Button _buttonPickerTarget;

        private static VisualTreeAsset visualTree;

        public Action<SchemaTileConnectionView, ConnectionData> OnConnectionClicked;

        #region Constructors

        public PickerConnect()
        {
            Clear();

            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerConnect");
            visualTree.CloneTree(this);

            _buttonPickerTarget = this.Q<Button>("PickerTarget");
            if (_buttonPickerTarget == null)
            {
                Debug.LogError("PickerTarget not found in VisualElement_QuestTargetBundle.uxml");
                return;
            }

            _buttonPickerTarget.clicked += () =>
            {
                ActivateButton(_buttonPickerTarget);
                OnClicked?.Invoke();
            };

            OnClicked += SetPickerManipulator;;
        }

        private void SetPickerManipulator()
        {
            ToolKit.Instance.SetActive(typeof(ConnectionPicker));

            // by default not picking the main trigger - its set on its OnClicked Implementation on QuestNodeBehaviourEditor
            object mani = ToolKit.Instance.GetActiveManipulator();
            var picker = ToolKit.Instance.GetTool(typeof(ConnectionPicker));
         
            if(mani is ConnectionPicker cpicker)
            {
                cpicker.Activator = this;
                cpicker.OnConnectionClicked += (tile, direction) => 
                { 
                    OnConnectionClicked?.Invoke(tile, direction); 
                };
                
            }

            if(picker.Value.Item2 is not null)
            {
                picker.Value.Item2.OnBlurEvent += () => 
                {
                    _buttonPickerTarget.ReleaseMouse();
          
                    _buttonPickerTarget.Blur();
                };
            }
            
        }


        #endregion

        #region Methods

        /// <summary>
        /// Clears the picker click callback.
        /// </summary>
        public void ClearPicker()
        {
            OnClicked = null;
        }

        #endregion


    }
}
