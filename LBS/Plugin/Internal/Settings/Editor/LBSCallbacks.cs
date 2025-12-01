using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

using ISILab.LBS.Settings;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.Internal.Editor;
using UnityEditor.Compilation;
using Debug = UnityEngine.Debug;

namespace ISILab.LBS.Internal.Editor
{
    [InitializeOnLoad]
    public class LBSCallbacks
    {
        private static BackUp localBackUp;
        private static Stopwatch stopwatch;
        static LBSCallbacks()
        {
            var onStart = SessionState.GetBool("start", true);
            Debug.Log("[LBS Callbacks] - On Start:" + onStart);
            if (onStart)
            {
                EditorApplication.update += OnStartEditor;
                SessionState.SetBool("start", false);
            }

            AssemblyReloadEvents.afterAssemblyReload += OnAfterReloadScript;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReloadScript;

            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationEnded;
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
            Debug.Log("[LBS Callbacks] - Before Reload Script");
            SaveBackUp();
        }

        /// <summary>
        /// called after the script is reloaded
        /// </summary>
        private static void OnAfterReloadScript()
        {
            Debug.Log("[LBS Callbacks] - After Reload Script");
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
            LBSAssetsStorage storage = LBSAssetsStorage.Instance;
            storage.SearchInProject();
        }

        public static void ReloadCurrentLevel()
        {
            var data = LBS.loadedLevel.data;
            data?.Reload();
            
        }

        public static void ReloadBundles()
        {
            LBSAssetsStorage storage = LBSAssetsStorage.Instance;
            Assert.IsNotNull(storage, "There no storage found!");
            List<Bundle> bundles = storage.Get<Bundle>();
            Assert.IsNotNull(bundles, "There no bundles in storage found!");
            Assert.AreNotEqual(bundles?.Count, 0,   "There are no bundles!");
            foreach (var bundle in bundles)
            {
                bundle.Reload();
            }
        }
        
        private static void OnCompilationStarted(object _obj)
        {
            stopwatch = Stopwatch.StartNew();
            Debug.Log("[LBS Callback] - Compilation started...");
        }
        
        private static void OnCompilationEnded(object _obj)
        {
            stopwatch.Stop();
            Debug.Log(
                $"[LBS Callbacks] - Compilation finished in:" +
                $"{stopwatch.ElapsedMilliseconds} ms" +
                $" ({stopwatch.Elapsed.TotalSeconds:F2} sec)");
        }

    }
}