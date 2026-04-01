using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    [UxmlElement]
    public partial class LBSTagListGroup : LBSBaseListGroup
    {
        private LBSToolbarButton disabledRemoveButton;
        
        private bool removable = true;
        
        [UxmlAttribute]
        public bool isRemovable
        {
            get => removable;
            set
            {
                removable = value;
                if(removeButton != null)
                {
                    removeButton.SetEnabled(isRemovable);
                    removeButton.style.visibility = isRemovable ? Visibility.Visible : Visibility.Hidden;
                    disabledRemoveButton.style.visibility = isRemovable ? Visibility.Hidden : Visibility.Visible;
                    removeButton.style.display = isRemovable ? DisplayStyle.Flex : DisplayStyle.None;
                    disabledRemoveButton.style.display = isRemovable ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        public LBSTagListGroup() : base()
        {
            OnListRemoved += () => { RemoveFromHierarchy(); };
            disabledRemoveButton = this.Q<LBSToolbarButton>("DisabledRemoveButton");

        }
        public LBSTagListGroup(bool removable) : base()
        {
            OnListRemoved += () => { RemoveFromHierarchy(); };
            disabledRemoveButton = this.Q<LBSToolbarButton>("DisabledRemoveButton");
            this.removable = removable;
        }
        
    }
}
