using System;
using ISILab.Commons.Utility.Editor;
using ISILab.DevTools.Macros;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Bundles;
using LBS.VisualElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class PickerConnect : PickerBase
    {   
        private readonly Button _buttonPickerTarget;
     
        public Action OnClicked;
        private static VisualTreeAsset visualTree;

        public Action<SchemaTileConnectionView, DirConnection> OnConnectionClicked;
        #region Constructors

        public PickerConnect()
        {
            Clear();

           
            if (!visualTree)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerConnect");
                return;
            }

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
