#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using System.IO;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ISILab.LBS.Settings;
using ISILab.LBS.Internal;

[InitializeOnLoad]
public class ISI_Lab_PackageManager : IPackageManagerExtension
{
    const string defaultSettingsGUID = "29abd09f3cff7644da7097258d0ae978";
    const string defaultStorageGUID = "5dacd13b749bccf469893489a5d0f94b";

    static ISI_Lab_PackageManager()
    {
        PackageManagerExtensions.RegisterExtension(new ISI_Lab_PackageManager());
        Debug.Log("Extension registrada");
    }

    public void OnPackageAddedOrUpdated(UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        Debug.Log("ON PACKAGE ADDED OR UPDATED");

        switch (packageInfo.name)
        {
            case "com.isilab.lbs":
                InitializeLBS(packageInfo);
                break;

            default:
                // A different package was modified.
                break;
        }
    }

    public void OnPackageRemoved(UnityEditor.PackageManager.PackageInfo packageInfo) { }

    public void OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo packageInfo) { }

    public VisualElement CreateExtensionUI() => null;

    private void InitializeLBS(UnityEditor.PackageManager.PackageInfo packageInfo)
    {
        Debug.Log("LEVEL BUILDING SIDEKICK");

        LBSSettings.assetName = "LBSUserSettings";
        LBSAssetsStorage.assetName = "Storage";

        LBSSettings.Instance.ReplacePaths(packageInfo);

        // Crear carpetas de usuario LBS
        string userFolderFullPath = "Assets/LBSUserContent";
        List<string> pathFolders = new List<string>(userFolderFullPath.Split('/'));
        string userFolder = pathFolders[pathFolders.Count - 1];
        pathFolders.Remove(userFolder);
        string userFolderPath = string.Join('/', pathFolders);

        CreateFolderIfItDoesntExist(userFolderPath, userFolder);

        foreach (string subfolder in new string[] { "Bundles", "Tags", "Meshes", "Settings", "Cache" })
        {
            CreateFolderIfItDoesntExist(userFolderFullPath, subfolder);
        }

        if (AssetDatabase.FindAssets("LBSUserSettings", new string[] { userFolderFullPath + "/Settings" }).Length == 0)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID), userFolderFullPath + "/Settings/LBSUserSettings.asset");
        }
        if (AssetDatabase.FindAssets("Storage", new string[] { userFolderFullPath + "/Cache" }).Length == 0)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultStorageGUID), userFolderFullPath + "/Cache/Storage.asset");
        }
    }

    private void CreateFolderIfItDoesntExist(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
