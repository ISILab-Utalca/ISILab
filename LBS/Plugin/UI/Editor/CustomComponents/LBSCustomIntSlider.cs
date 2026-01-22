using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.CustomComponents
{
    
    
    [UxmlElement]
    public partial class LBSCustomIntSlider: SliderInt
    {
        public LBSCustomIntSlider() : base()
        {
            fill = true;
            showInputField = true;
        }
        
        
    }
}
