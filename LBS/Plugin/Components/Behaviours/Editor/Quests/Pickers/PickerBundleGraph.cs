using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PickerBundleGraph : PickerBase
{
    private TextField _layer;
    private Vector2IntField _position;
    private VisualElement _warning;

    public PickerBundleGraph()
    {
        var tree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleGraph");
        tree.CloneTree(this);

        ObjectField = this.Q<LBSCustomObjectField>("TargetFieldBundle");
        PickButton = this.Q<Button>("PickerTarget");

        _layer = this.Q<TextField>("Layer");
        _position = this.Q<Vector2IntField>("Position");
        _warning = this.Q<VisualElement>("Warning");

        ObjectField.SetEnabled(false);

        BindCommonButton();
        OnClicked += ActivateQuestPicker;
    }

    public void SetInfo(string label, string tooltip)
    {
        ObjectField.labelElement.text = label + " (In Graph)";
        this.tooltip = tooltip;
    }

    public void SetLayerTarget(LayerTarget target)
    {
        if (target == null)
            return;

        _warning.style.display =
            target.Valid() ? DisplayStyle.None : DisplayStyle.Flex;

        if (target is BundleGraph bg)
        {
            _position.value = bg.Position;

            var layer = target.GetLayer();
            if (layer != null)
                _layer.value = layer.Name;
        }
    }
}