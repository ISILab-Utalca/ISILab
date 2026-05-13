using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using ISILab.LBS.VisualElements;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

[UxmlElement]
public partial class PickerBundleType : PickerBase, IBundleFilter
{
    public LBSButtonListFilter BundlePickerWindow { get; set; }

    private static VisualTreeAsset visualTree;
    protected LBSCustomObjectField _bundleField;
    private VisualElement _warning;

    public Action<LBSLayer, TileBundleGroup> OnBundlePicked { get; set; }

    public PickerBundleType()
    {
        visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("PickerBundleType");
        visualTree.CloneTree(this);

        _bundleField = this.Q<LBSCustomObjectField>("TargetFieldBundle");
        _bundleField.SetEnabled(true);

        _bundleField.UseCustomFilter = true;
        _bundleField.CustomFilter = pick =>
        {
            List<BundleFlags> flags = new List<BundleFlags>() { BundleFlags.Population };
            var bundles = BundleQueryUtility.FindBundlesWithFlag(new List<BundleFlags>(flags));
            (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
        };

        _bundleField.RegisterValueChangedCallback(evt =>
        {
            var bundle = evt.newValue as Bundle;
            _warning.style.display = bundle != null ? DisplayStyle.None : DisplayStyle.Flex;
        });

        _warning = this.Q<VisualElement>("Warning");

        BindPickButton();
    }

    public override void SetInfo(string label, string tooltip)
    {
        _bundleField.labelElement.text = label + " (Type)";
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
        _bundleField.value = LBSAssetMacro.LoadAssetByGuid<Bundle>(target.GUID);
        _warning.style.display = target.IsValid() ? DisplayStyle.None : DisplayStyle.Flex;
    }


}