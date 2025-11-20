using LBS.Bundles;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBundleFilter
{
    public LBSButtonListFilter BundlePickerWindow { get; set; }

    public void OpenFilterWindow(List<Bundle> bundles, Action<Bundle> onPick)
    {
        CloseFilterWindow();

        BundlePickerWindow = LBSButtonListFilter.Show(bundles, picked => onPick(picked));
    }

    public void CloseFilterWindow()
    {
        if (BundlePickerWindow)
            BundlePickerWindow.Close();
    }
}
