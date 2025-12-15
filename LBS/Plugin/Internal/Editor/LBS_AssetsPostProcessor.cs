using ISILab.LBS.Plugin.Core.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.Internal.Editor
{
    public class LBS_AssetsPostProcessor : AssetPostprocessor
    {
        const string defaultSettingsGUID = "29abd09f3cff7644da7097258d0ae978";
        const string defaultStorageGUID = "5dacd13b749bccf469893489a5d0f94b";

        
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Contains(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID)))
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
                if(packageInfo is not null)
                {
                    Debug.Log("LBS SETTINGS IMPORT");
                    InitializeLBSPackage();
                }
            }
            OnPostImportProcess(importedAssets);
            OnPostDeleteProcess(deletedAssets);
            OnPostMoveProcess(movedAssets);
            OnPostMoveFromPathsProcess(movedFromAssetPaths);
        }

        public static void OnPostImportProcess(string[] importedAssets)
        {
            var storage = LBSAssetsStorage.Instance;

            foreach (var asset in importedAssets)
            {
                if (asset.Contains(".meta"))
                    continue;

                if (!asset.Contains(".asset"))
                    continue;

                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(asset);

                if (obj == null)
                    continue;

                storage.AddElement(obj);
            }

            AssetDatabase.SaveAssets();
        }


        public static void OnPostDeleteProcess(string[] deletedAssets)
        {
            var storage = LBSAssetsStorage.Instance;

            foreach (var asset in deletedAssets)
            {
                if (asset.Contains(".meta"))
                    return;

                if (!asset.Contains(".asset"))
                    return;
            }

            storage?.CleanAllEmpties();

            AssetDatabase.SaveAssets();
        }

        public static void OnPostMoveProcess(string[] movedAssets)
        {
            // do nothing
        }

        public static void OnPostMoveFromPathsProcess(string[] movedFromAssetPaths)
        {
            // do nothing
        }

        public static void InitializeLBSPackage()
        {
            Debug.Log("LEVEL BUILDING SIDEKICK");

            // Crear carpetas de usuario LBS
            string userFolderFullPath = "Assets/LBSUserContent";
            List<string> pathFolders = new List<string>(userFolderFullPath.Split('/'));
            string userFolder = pathFolders[pathFolders.Count - 1];
            pathFolders.Remove(userFolder);
            string userFolderPath = string.Join('/', pathFolders);
            string resourcesFolderPath = userFolderFullPath + "/Resources";


            CreateFolderIfItDoesntExist(userFolderPath, userFolder);
            CreateFolderIfItDoesntExist(userFolderFullPath, "Resources");

            foreach (string subfolder in new string[] { "Bundles", "Tags", "Meshes" })
            {
                CreateFolderIfItDoesntExist(userFolderFullPath, subfolder);
            }
            foreach (string subFolder in new string[] {"Settings", "Cache" })
            {
                CreateFolderIfItDoesntExist(resourcesFolderPath, subFolder);
            }

            if (AssetDatabase.FindAssets("LBSUserSettings", new string[] { resourcesFolderPath + "/Settings" }).Length == 0)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID), resourcesFolderPath + "/Settings/LBSUserSettings.asset");
            }
            if (AssetDatabase.FindAssets("Storage", new string[] { resourcesFolderPath + "/Cache" }).Length == 0)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultStorageGUID), resourcesFolderPath + "/Cache/Storage.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LBSSettings.assetName = "LBSUserSettings";
            LBSSettings.ResetInstance();
            LBSSettings.Instance.ReplacePaths();

            LBSAssetsStorage.assetName = "Storage";
            LBSAssetsStorage.folderName = "Cache";
            LBSAssetsStorage.ResetInstance();
        }
        private static void CreateFolderIfItDoesntExist(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}