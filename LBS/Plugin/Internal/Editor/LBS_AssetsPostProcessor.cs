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
            bool databaseChanged = false;
            if (importedAssets.Contains(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID)))
            {
                Debug.Log("LBS SETTINGS IMPORT");
                InitializeLBSPackage(out databaseChanged);
            }

            if (!databaseChanged) PostProcessAll();
            else EditorApplication.delayCall += () => PostProcessAll();


            void PostProcessAll()
            {
                OnPostImportProcess(importedAssets);
                OnPostDeleteProcess(deletedAssets);
                OnPostMoveProcess(movedAssets);
                OnPostMoveFromPathsProcess(movedFromAssetPaths);
            }
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

        public static void InitializeLBSPackage(out bool databaseChaged)
        {
            Debug.Log("LEVEL BUILDING SIDEKICK");

            databaseChaged = false;

            // Crear carpetas de usuario LBS
            string userFolderFullPath = "Assets/LBSUserContent";
            List<string> pathFolders = new List<string>(userFolderFullPath.Split('/'));
            string userFolder = pathFolders[pathFolders.Count - 1];
            pathFolders.Remove(userFolder);
            string userFolderPath = string.Join('/', pathFolders);
            string resourcesFolderPath = userFolderFullPath + "/Resources";


            if( CreateFolderIfItDoesntExist(userFolderPath, userFolder) ||
                CreateFolderIfItDoesntExist(userFolderFullPath, "Resources"))
                databaseChaged = true;

            foreach (string subfolder in new string[] { "Bundles", "Tags", "Meshes" })
            {
                if(CreateFolderIfItDoesntExist(userFolderFullPath, subfolder))
                    databaseChaged = true;
            }
            foreach (string subFolder in new string[] {"Settings", "Cache" })
            {
                if(CreateFolderIfItDoesntExist(resourcesFolderPath, subFolder))
                    databaseChaged = true;
            }

            if (AssetDatabase.FindAssets("LBSUserSettings", new string[] { resourcesFolderPath + "/Settings" }).Length == 0)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultSettingsGUID), resourcesFolderPath + "/Settings/LBSUserSettings.asset");
                databaseChaged = true;
            }
            if (AssetDatabase.FindAssets("Storage", new string[] { resourcesFolderPath + "/Cache" }).Length == 0)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(defaultStorageGUID), resourcesFolderPath + "/Cache/Storage.asset");
                databaseChaged = true;
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
        private static bool CreateFolderIfItDoesntExist(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
                return true; // Was created
            }
            return false; // Was not created
        }
    }
}