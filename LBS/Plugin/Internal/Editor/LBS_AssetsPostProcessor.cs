using ISILab.LBS.Internal;
using ISILab.LBS.Settings;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Internal.Editor
{
    public class LBS_AssetsPostProcessor : AssetPostprocessor
    {
        const string defaultSettingsGUID = "29abd09f3cff7644da7097258d0ae978";
        const string defaultStorageGUID = "5dacd13b749bccf469893489a5d0f94b";

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Contains(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID)))
            {
                Debug.Log("LBS SETTINGS IMPORT");
                InitializeLBS();
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

            storage.CleanAllEmpties();

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

        private static void InitializeLBS()
        {
            Debug.Log("LEVEL BUILDING SIDEKICK");

            LBSSettings.assetName = "LBSUserSettings";
            LBSAssetsStorage.assetName = "Storage";

            LBSSettings.Instance.ReplacePaths();

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
        private static void CreateFolderIfItDoesntExist(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}