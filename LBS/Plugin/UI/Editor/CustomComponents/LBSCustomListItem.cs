using UnityEngine;
using UnityEngine.UIElements;


namespace  ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSCustomListItem: VisualElement
    {
        public LBSCustomListItem() : base()
        {
            this.AddToClassList("lbs-custom-list-item");
        }        
    }
}
