using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PickerBundleType : PickerBase
{
    public PickerBundleType()
    {
        var tree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleType");
        tree.CloneTree(this);

        ObjectField = this.Q<LBSCustomObjectField>("TargetFieldBundle");
        PickButton = this.Q<Button>("PickerTarget");

        ObjectField.SetEnabled(true);

        BindCommonButton();
    }

    public void SetInfo(string label, string tooltip)
    {
        ObjectField.labelElement.text = label + " (Type)";
        this.tooltip = tooltip;
    }

    public void SetBundle(Bundle bundle)
    {
        ObjectField.value = bundle;
    }
}