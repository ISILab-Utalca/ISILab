using System;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
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
        private static VisualTreeAsset visualTree;

        public Action<SchemaTileConnectionView, ConnectionData> OnConnectionClicked;

        #region Constructors

        public PickerConnect()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerConnect");
            visualTree.CloneTree(this);

            BindPickButton();
            PickButton.AddGroupEvent(SetPickerManipulator);
        }

        private void SetPickerManipulator()
        {
            ToolKit.Instance.SetActive(typeof(ConnectionPicker));

            object mani = ToolKit.Instance.GetActiveManipulator();
            var picker = ToolKit.Instance.GetTool(typeof(ConnectionPicker));
         
            if(mani is ConnectionPicker cpicker)
            {
                cpicker.Activator = this;

                cpicker.OnConnectionClicked -= OnConnect;
                cpicker.OnConnectionClicked += OnConnect;
            }

            if (picker.Value.Item2 is not null)
            {
                picker.Value.Item2.OnBlurEvent -= onBlur;
                picker.Value.Item2.OnBlurEvent += onBlur;
            }
            
        }

        private void OnConnect(SchemaTileConnectionView tile, ConnectionData direction) 
            => OnConnectionClicked?.Invoke(tile, direction);
        private void onBlur()
        {
            PickButton.ReleaseMouse();
            PickButton.Blur();
        }

        protected override void PickerLogic()
        {
            throw new NotImplementedException();
        }

        public override void SetInfo(string name, string tooltip)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
