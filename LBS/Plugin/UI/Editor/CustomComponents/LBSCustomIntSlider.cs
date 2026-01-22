using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.CustomComponents
{
    
    
    [UxmlElement]
    public partial class LBSCustomIntSlider: SliderInt
    {
        public LBSCustomIntSlider() : base()
        {
            AddToClassList("lbs-custom-int-slider");
            fill = true;
            showInputField = true;
        }
        
        
    }
}
