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
    protected LBSCustomObjectField objectField;

    public Action<LBSLayer, TileBundleGroup> OnBundlePicked { get; set; }

    public PickerBundleType()
    {
        visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleType");
        visualTree.CloneTree(this);

        objectField = this.Q<LBSCustomObjectField>("TargetFieldBundle");
        objectField.SetEnabled(true);

        BindPickButton();
    }

    public override void SetInfo(string label, string tooltip)
    {
        objectField.labelElement.text = label + " (Type)";
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

    public virtual void SetLayerTarget(BundleTarget target)
    {
        if (target == null)
            return;

        // display bundle
        objectField.value = LBSAssetMacro.LoadAssetByGuid<Bundle>(target.GUID);

    }
}