#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using System.IO;
using UnityEngine.UIElements;

public class ISI_Lab_PackageManager : IPackageManagerExtension
{
    public VisualElement CreateExtensionUI()
    {
        return null;
    }

    public void OnPackageAddedOrUpdated(UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        // TODO: Crear carpetas de usuario LBS
    }

    public void OnPackageRemoved(UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        
    }

    public void OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        
    }
}
#endif
