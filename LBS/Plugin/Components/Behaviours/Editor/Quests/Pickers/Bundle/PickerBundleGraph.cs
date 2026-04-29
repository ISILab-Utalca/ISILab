using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements;
using LBS.VisualElements;
using System.Reflection.Emit;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PickerBundleGraph : PickerBundleType
{
    private TextField _layer;
    private Vector2IntField _position;
    private VisualElement _warning;
    private static VisualTreeAsset visualTree;

    public PickerBundleGraph() : base()
    {
        visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleGraph");
        visualTree.CloneTree(this);

        _layer = this.Q<TextField>("Layer");
        _position = this.Q<Vector2IntField>("Position");
        _warning = this.Q<VisualElement>("Warning");

        objectField.SetEnabled(false);
    }


    public override void SetInfo(string label, string tooltip)
    {
        objectField.labelElement.text = label + " (In Graph)";
        this.tooltip = tooltip;
    }

    public override void SetLayerTarget(BundleTarget target)
    {
        base.SetLayerTarget(target);

        if (target == null)
            return;

        _warning.style.display = target.IsValid() ? DisplayStyle.None : DisplayStyle.Flex;

        // graph only info
        if (target is BundleTargetGraph bg)
        {
            _position.value = bg.Position;
            if (bg.Layer != null) _layer.value = bg.Layer.Name;
        }
    }
}