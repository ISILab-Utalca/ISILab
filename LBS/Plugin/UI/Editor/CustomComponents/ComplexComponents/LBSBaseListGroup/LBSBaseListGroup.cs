using ISILab.LBS.CustomComponents;
using UnityEditor.VersionControl;using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSBaseListGroup : LBSComplexVisualElement
    {
        
        private bool isEmpty = false;
        
        // VisualElement references
        private LBSCustomButton overlayButton;
        private Label titleLabel;
        
      
        [UxmlAttribute]
        public bool IsEmpty
        {
            get { return isEmpty; }
            set
            {
                isEmpty = value;
                if (overlayButton != null){
                    overlayButton.SetEnabled(isEmpty); 
                    overlayButton.style.visibility = isEmpty ? Visibility.Visible : Visibility.Hidden;
                    overlayButton.style.display = isEmpty? DisplayStyle.Flex: DisplayStyle.None;
                }
            }
        }
        
        
        
        public LBSBaseListGroup() : base()
        {
            GetVisualTreeForThis();
            AddToClassList("lbs-base-list-group");

            overlayButton = this.Q<LBSCustomButton>("EmptyOverlayButton");
            overlayButton.RegisterCallback<ClickEvent>(evt =>
            {
               isEmpty = false; 
               overlayButton.SetEnabled(false);
               overlayButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            });

            titleLabel = this.Q<Label>("TitleLabel");

        }
        
        
    }
}

