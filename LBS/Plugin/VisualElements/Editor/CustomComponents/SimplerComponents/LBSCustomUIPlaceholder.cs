using ISILab.DevTools.Macros;
using ISILab.LBS.CustomComponents;
using UnityEngine.UIElements;



[UxmlElement]
public partial class LBSCustomUIPlaceholder: VisualElement
{
    public LBSCustomUIPlaceholder()
    {
        Label nameLabel = new Label("Placeholder");
        Image image = new LBSCustomImage(AssetMacro.LoadPlaceholderVectorImage());
        image.style.height = 16;
        image.style.width = 16;
        this.Add(nameLabel);
        this.Add(image);
        this.style.flexDirection = FlexDirection.Column;
        this.style.flexGrow = 1;
        this.style.alignItems = Align.Center;
    }
}
