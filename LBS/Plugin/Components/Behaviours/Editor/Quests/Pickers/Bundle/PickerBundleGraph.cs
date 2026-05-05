using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PickerBundleGraph : PickerBundleType
{
    private TextField _layerTextField;
    private Vector2IntField _positionField;
    
    private static VisualTreeAsset visualTree;

    public PickerBundleGraph() : base()
    {
        visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleGraph");
        visualTree.CloneTree(this);

        _layerTextField = this.Q<TextField>("Layer");
        _positionField = this.Q<Vector2IntField>("Position");

        _objectField.SetEnabled(false);
    }


    public override void SetInfo(string label, string tooltip)
    {
        _objectField.labelElement.text = label + " (In Graph)";
        this.tooltip = tooltip;
    }

    public override void SetLayerTarget(BundleTarget target)
    {
        base.SetLayerTarget(target);

        if (target == null)
            return;

        // graph only info
        if (target is BundleTargetGraph bg)
        {
            _positionField.value = bg.Position;
            if (bg.Layer != null) _layerTextField.value = bg.Layer.Name;
        }
    }
}