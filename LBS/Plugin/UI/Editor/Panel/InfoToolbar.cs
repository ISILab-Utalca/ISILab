using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using LBS.VisualElements;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Panel
{
    [UxmlElement]
    public partial class InfoToolbar: VisualElement
    {
        private static VisualTreeAsset VisualTree;
        
        private LBSToolbarButton clearNotificationButton;
        private LBSToolbarButton disableNotificationButton;
        
        private VisualElement toolInformation;
        private Label toolLabel;
        
        private Label selectedLabel;
        
        private NotifierViewer notificationContainer;
        
        private Label spacer;
        private Label gridText;
        private Label positionLabel;
        
        private VisualElement warningNotification;
        private Label warningText;



        public InfoToolbar()
        {
            VisualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("InfoToolbar");
            VisualTree.CloneTree(this);
            
            this.name = "InfoToolbarWidget";
            this.AddToClassList("lbs-widget");

            clearNotificationButton = this.Q<LBSToolbarButton>("CleanNotificationsButton");
            disableNotificationButton = this.Q<LBSToolbarButton>("DisableNotificationsButton");
            
            toolInformation = this.Q<VisualElement>("ToolInformation");
            toolLabel  = this.Q<Label>("ToolText");
            
            selectedLabel = this.Q<Label>("SelectedLabel");
            
            notificationContainer = this.Q<NotifierViewer>("NotifierViewer");
            
            spacer = this.Q<Label>("Spacer");
            gridText = this.Q<Label>("GridText");
            positionLabel = this.Q<Label>("PositionLabel");
            
            warningNotification = this.Q<VisualElement>("WarningNotification");
            warningText = this.Q<Label>("WarningText");
            
            
        }


        public void Bind(LBSMainWindow _mainWindow)
        { 
            LBSMainWindow.notifier.SetButtons(clearNotificationButton, disableNotificationButton);
            warningNotification.visible = false;
        }

        public void SmallMessage(string _description)
        {
            if (toolLabel == null) return;
            toolLabel.text = _description;
        }
    }
}


