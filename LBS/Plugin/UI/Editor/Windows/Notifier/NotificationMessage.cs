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

            // 1. Traditional Switch Statement for the Icon
            VectorImage setIcon;
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    setIcon = ErrorImage;
                    break;

                case LogType.Assert:
                case LogType.Log:
                    setIcon = LogImage;
                    break;

                case LogType.Warning:
                    setIcon = WarningImage;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }

            // 2. Traditional Switch Statement for the Color
            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                    icon.style.unityBackgroundImageTintColor = LBSSettings.Instance.view.errorColor;
                    break;

                case LogType.Warning:
                    icon.style.unityBackgroundImageTintColor = LBSSettings.Instance.view.warningColor;
                    break;

                case LogType.Log:
                    break;

                case LogType.Exception:
                    icon.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0); 
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
            }

            icon.style.backgroundImage = new StyleBackground(setIcon);
            message.text = inMessage;
        }
    } 
}
