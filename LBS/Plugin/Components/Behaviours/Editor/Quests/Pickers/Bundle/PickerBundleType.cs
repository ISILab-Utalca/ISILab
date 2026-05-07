using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements;
using LBS.Components;
using LBS.VisualElements;
using System;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PickerBundleType : PickerBase
{
    private static VisualTreeAsset visualTree;
    protected LBSCustomObjectField _objectField;
    private VisualElement _warning;

    public Action<LBSLayer, TileBundleGroup> OnBundlePicked { get; set; }

    public PickerBundleType()
    {
        visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleType");
        visualTree.CloneTree(this);

        _objectField = this.Q<LBSCustomObjectField>("TargetFieldBundle");
        _objectField.SetEnabled(true);

        _warning = this.Q<VisualElement>("Warning");

        BindPickButton();
    }

    public override void SetInfo(string label, string tooltip)
    {
        _objectField.labelElement.text = label + " (Type)";
        this.tooltip = tooltip;
    }

    protected override void PickerLogic()
    {
        if (ToolKit.Instance.GetActiveManipulatorInstance() is not QuestPicker pickerManipulator)
            return;

        pickerManipulator.ActiveType = QuestPickType.Bundle;
        pickerManipulator.ActiveData = GetActionData();

        if (pickerManipulator.ActiveData == null) return;

        pickerManipulator.OnBundlePicked = (layer, tile) =>
        {
            OnBundlePicked?.Invoke(layer, tile);
        };
    }

    public virtual void SetLayerTarget(BundleTarget target, bool WithoutNotify = false)
    {
        if (target == null)
            return;

        // display bundle
        _objectField.value = LBSAssetMacro.LoadAssetByGuid<Bundle>(target.GUID);

        _warning.style.display = target.IsValid() ? DisplayStyle.None : DisplayStyle.Flex;
    }
}