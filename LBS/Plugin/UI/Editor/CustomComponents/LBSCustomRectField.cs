using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomRectField : RectField
    {

        public static readonly string LBSClassName = "lbs-field";
        public static readonly string LBSFieldClassName = "lbs-rect-field";

        public LBSCustomRectField() : base()
        {
            RemoveFromClassList(ussClassName);
            AddToClassList(LBSClassName);
            AddToClassList(LBSFieldClassName);

            //VisualElement inputSpace = this.Q<VisualElement>(classes: inputUssClassName);
            VisualElement spacer = this.Q<VisualElement>(classes: spacerUssClassName);
            spacer.SendToBack();
        }
    }
}

