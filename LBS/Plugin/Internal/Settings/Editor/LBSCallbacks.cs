using LBS.Bundles;
using ISILab.LBS.Settings;
using ISILab.LBS.Editor.Windows;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Internal.Editor
{
    [InitializeOnLoad]
    public class LBSCallbacks
    {
        private static BackUp localBackUp;

        const string defaultSettingsGUID = "29abd09f3cff7644da7097258d0ae978";
        const string defaultStorageGUID = "5dacd13b749bccf469893489a5d0f94b";

        static LBSCallbacks()
        {
            var onStart = SessionState.GetBool("start", true);
            Debug.Log("On Start:" + onStart);
            if (onStart)
            {
                EditorApplication.update += OnStartEditor;
                SessionState.SetBool("start", false);
            }

            AssemblyReloadEvents.afterAssemblyReload += OnAfterReloadScript;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReloadScript;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            if (packageInfo is not null && packageInfo.name.Equals("com.isilab.lbs"))
            {
                AssemblyReloadEvents.afterAssemblyReload += InitializeLBSPackage;
            }
        }

        /// <summary>
        /// called when the editor starts for the first time
        /// </summary>
        private static void OnStartEditor()
        {
            SettingsEditor.SearchSettingsInstance();
            ReloadBundles();

            EditorApplication.update -= OnStartEditor;
        }

        /// <summary>
        /// called before the script is reloaded
        /// </summary>
        private static void OnBeforeReloadScript()
        {
            Debug.Log("Before Reload Script");
            SaveBackUp();
        }

        /// <summary>
        /// called after the script is reloaded
        /// </summary>
        private static void OnAfterReloadScript()
        {
            Debug.Log("After Reload Script");
            LoadBackUp();
            ReloadCurrentLevel();
        }

        /// <summary>
        /// save the level in the backup temporarily
        /// </summary>
        private static void SaveBackUp()
        {
            LoadedLevel level = LBS.loadedLevel;
            LBSLevelData data = level?.data;

            if (data != null)
            {
                //Instance
                localBackUp = ScriptableObject.CreateInstance<BackUp>();

                //Backup file setup
                var settings = LBSSettings.Instance;
                var path = settings.paths.backUpPath;
                var folderPath = Path.GetDirectoryName(path);

                //Directory making
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                //Save the level into the backup
                switch(level == null)
                {
                    case true:
                        localBackUp.level = LBSController.CreateNewLevel("new file");
                        localBackUp.level.data = level.data;
                        break;
                    case false:
                        localBackUp.level = level;
                        break;
                }

                //Make the asset
                AssetDatabase.CreateAsset(localBackUp, path);
                AssetDatabase.SaveAssets();
            }
            else
            {
                LBSMainWindow.MessageNotify("Error on save BackUp", LogType.Error);
            }
        }

        /// <summary>
        /// Load the level from the backup and delete it
        /// </summary>
        private static void LoadBackUp()
        {
            // search and set the instance of "LBS Settings" in its singleton
            var settings = LBSSettings.Instance;
            var path = settings.paths.backUpPath;
            var backUp = AssetDatabase.LoadAssetAtPath<BackUp>(path);
            
            if (backUp?.level != null)
            {
                // load the level from the backup
                LBS.loadedLevel = backUp.level;
            } 
            else
            {
                LBS.loadedLevel = LoadedLevel.CreateInstance(new LBSLevelData(), "New level");
            }
        }

        public static void ReloadStorage()
        {
            var storage = LBSAssetsStorage.Instance;
            storage.SearchInProject();
        }

        public static void ReloadCurrentLevel()
        {
            var data = LBS.loadedLevel.data;
            data?.Reload();
            
        }

        public static void ReloadBundles()
        {
            var storage = LBSAssetsStorage.Instance;
            var bundles = storage.Get<Bundle>();
            foreach (var bundle in bundles)
            {
                bundle.Reload();
            }
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
            foreach (string subFolder in new string[] { "Settings", "Cache" })
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