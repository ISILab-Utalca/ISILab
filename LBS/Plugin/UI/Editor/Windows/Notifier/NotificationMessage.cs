using System;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace LBS.VisualElements
{
    [UxmlElement]
    public partial class NotificationMessage : VisualElement
    {
        private Label message;
        private VisualElement icon;

        private const string errorImageGuid = "7bdf2adeb17673349abf65c6f8f0f411";
        private const string logImageGuid = "8c0952dcbc9d49f4198ce33fdf7b4df5";
        private const string warningImageGuid = "5549d02f87d9642469d0336544f4cb88";

        private VectorImage ErrorImage => LBSAssetMacro.LoadAssetByGuid<VectorImage>(errorImageGuid);
        private VectorImage LogImage => LBSAssetMacro.LoadAssetByGuid<VectorImage>(logImageGuid);
        private VectorImage WarningImage => LBSAssetMacro.LoadAssetByGuid<VectorImage>(warningImageGuid);

        public NotificationMessage()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("NotificationMessage");
            visualTree.CloneTree(this);
            message = this.Q<Label>("MessageVe");
            icon = this.Q<VisualElement>("IconVe");
           
            pickingMode = PickingMode.Ignore;
            message.pickingMode = PickingMode.Ignore;
            icon.pickingMode = PickingMode.Ignore;

            style.flexDirection = FlexDirection.Row;
            style.flexGrow = 1;
        }

        /**
         * Currently only unique icons for LogTypes:
         * -Error
         * -Warning
         * -Log
         */
        public void SetData(string inMessage, LogType logType)
        {
            if (message == null || icon == null)
            {
                Debug.LogError("Missing VE");
                return;
            }
            VectorImage setIcon = logType switch
            {
                LogType.Error => ErrorImage,
                LogType.Assert => LogImage,
                LogType.Warning => WarningImage,
                LogType.Log => LogImage,
                LogType.Exception => ErrorImage,
                _ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
            };
            
            Color setColor = logType switch
            {
                LogType.Error => LBSSettings.Instance.view.errorColor,
                LogType.Assert => LBSSettings.Instance.view.errorColor,
                LogType.Warning => LBSSettings.Instance.view.warningColor,
                LogType.Log => LBSSettings.Instance.view.okColor,
                LogType.Exception => new Color(1, 1, 1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
            };
            
            icon.style.backgroundImage = new StyleBackground(setIcon);
            icon.style.unityBackgroundImageTintColor = setColor;
            message.text = inMessage;

        } 
    } 
}
