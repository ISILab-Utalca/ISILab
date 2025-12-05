using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.LBS.Plugin.Core.Settings
{
    [Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/Internal/LBS Settings", fileName = "LBS Settings")]
    public class LBSSettings : ScriptableObject
    {
        
        private const string USER_ASSET_FOLDER_NAME = "LBSUserContent";

        private static string mainFolder = "Assets/ISILab";

        public static string assetName = "LBSDefaultSettings";
        
        #region SINGLETON
        private static LBSSettings instance;

        /// <summary>
        /// Singleton instance of "LBSSettings".<br/>
        /// <b>[WARNING]:</b> The use of the <b>SET</b> method for this property is at your own risk.
        /// </summary>
        public static LBSSettings Instance 
        {
            get
            {
                // si es igual a null lo busco en carpeta
                if (instance == null)
                {
                    Debug.Log(assetName);
                    instance = Resources.Load<LBSSettings>(assetName);
                    // si sigue siendo null lo creo
                    if (instance == null)
                        instance = ScriptableObject.CreateInstance<LBSSettings>();

                    //instance.InitPaths();
                }
                //else Debug.Log("LBS Settings existe.");

                return instance;
            }

            set // que esto sea publico es un problema por que cualquier cosa lo puede acceder, pero
            {
                instance = value;
            }
        }

        public static void ResetInstance()
        {
            Instance = null;
            var a = Instance;
        }

        public void MarkSettingsAsDirty()
        {
            EditorUtility.SetDirty(this);
        }        
        #endregion

        public Paths paths = new Paths();
        public General general = new General();
        public Interface view = new Interface();
        public Test test = new Test();
        [System.Obsolete]
        public Generator3D generator = new Generator3D();

        public void ReplacePaths()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);

            //mainFolder = packageInfo is null ? 
            //    "Assets/isi-lab-unity-module" :
            //    "Packages/" + packageInfo.name;

            if (packageInfo is null) return;

            string userFolder = "Assets/LBSUserContent";
            string packageFolder = "Packages/" + packageInfo.name;

            instance.paths.settingsPath = userFolder + "/Resources/Settings/LBSUserSettings.asset";
            instance.paths.storagePath = userFolder + "/Resources/Cache/Storage.asset";
            instance.paths.backUpPath = userFolder + "/Resources/Cache/LBSBackUp.asset";

            //ReplacePathStart(ref instance.paths.settingsPath, userFolder); instance.paths.settingsPath.Replace("LBSDefaultSettings", "LBSUserSettings");
            //ReplacePathStart(ref instance.paths.storagePath, userFolder); instance.paths.storagePath.Replace("StorageTemplate", "Storage");
            //ReplacePathStart(ref instance.paths.pressetsPath);
            //ReplacePathStart(ref instance.paths.backUpPath, userFolder);

            instance.paths.bundleFolderPath = userFolder + "/Bundles";
            instance.paths.tagFolderPath = userFolder + "/Tags";
            instance.paths.meshFolderPath = userFolder + "/Meshes";

            instance.paths.WFCpresetsFolderPath = userFolder + "/Presets/WFC";

            //ReplacePathStart(ref instance.paths.bundleFolderPath, userFolder);
            //ReplacePathStart(ref instance.paths.tagFolderPath, userFolder);
            //ReplacePathStart(ref instance.paths.meshFolderPath, userFolder);

            //ReplacePathStart(ref instance.paths.iconPath);

            ReplacePathStart(ref instance.paths.layerPressetFolderPath, packageFolder);
            ReplacePathStart(ref instance.paths.assistantPresetFolderPath, packageFolder);
            //ReplacePathStart(ref instance.paths.assistantOptimizerPresetPath);
            //ReplacePathStart(ref instance.paths.assistantEvaluatorPresetPath);
            //ReplacePathStart(ref instance.paths.Generator3DPresetFolderPath);
            //ReplacePathStart(ref instance.paths.bundlesPresetFolderPath);

            instance.MarkSettingsAsDirty();

            void ReplacePathStart(ref string path, string newStart)
            {
                if (path.StartsWith(newStart)) return;

                int start = path.IndexOf("/LBS/");
                path = newStart + path[start..];
                //Debug.Log("Updated path: " +  path);
            }
        }

        [System.Serializable]
        public class Test
        {
            public string TestFolderPath = "";
        }

        [System.Serializable]
        public class General
        {

            public float zoomMax = 10;
            public float zoomMin = 0.1f;

            [SerializeField]
            Vector2 tileSize = new Vector2(50, 50);

            public Vector2 TileSize
            {
                get => tileSize;
                set => tileSize = value;
            }

            public String baseLayerName = "New Layer";

            public Action<float, float> OnChangeZoomValue;
            public Action<Vector2> OnChangeTileSize;
        }

        [System.Serializable]
        public class Paths
        {
            // Controller Paths
            public string settingsPath                  = "Assets/ISILab/LBS/Plugin/Internal/Settings/Resources/LBSDefaultSettings.asset";
            public string storagePath                   = "Assets/ISILab/LBS/Plugin/Internal/Resources/Storage/StorageTemplate.asset";
            //public string pressetsPath                = "Assets/ISILab/LBS/Presets/Assistants/DungeonPreset.asset";
            public string backUpPath                    = "Assets/ISILab/LBS/Plugin/Internal/Resources/BackUp/LBSBackUp.asset";
                                                                  
            // Folders for user data storages                              
            public string bundleFolderPath              = "Assets/ISILab/LBS/Content/Bundles";
            public string tagFolderPath                 = "Assets/ISILab/LBS/Content/Tags";
            public string meshFolderPath                = "Assets/ISILab/LBS/Content/Meshes";

            public string WFCpresetsFolderPath          = "Assets/ISILab/LBS/Content/Presets/WFC";

            // Folders extra storages                             
            //public string iconPath                    = "Assets/ISILab/LBS/Plugin/Internal/Icons";

            // Folders presets                                    
            public string layerPressetFolderPath        = "Assets/ISILab/LBS/Presets/Layers";
            public string assistantPresetFolderPath     = "Assets/ISILab/LBS/Presets/Assistants";
            //public string assistantOptimizerPresetPath= "Assets/ISILab/LBS/Presets/Optimizers";
            //public string assistantEvaluatorPresetPath= "Assets/ISILab/LBS/Presets/Evaluators";
            //public string Generator3DPresetFolderPath = "Assets/ISILab/LBS/Presets/Generators3D";
            //public string bundlesPresetFolderPath     = "Assets/ISILab/LBS/Presets/Bundles";

            //public string savedMapsPresetPath = "Assets/ISI Lab/LBS/Presets/SavedMaps";

        }

        [Serializable]
        public class Interface
        {
            public enum InterfaceTheme {Dark, Light, Alt}
            
            [SerializeField]
            public InterfaceTheme LBSTheme = InterfaceTheme.Dark;
            
            public Color toolkitNormal = new Color(0.28f, 0.28f, 0.28f);
            public Color toolkitNormalDark = new(0.16f, 0.16f, 0.16f);
            public Color newToolkitSelected = new Color(0.21f, 0.48f, 0.96f);
            
            public Color behavioursColor = new Color(0.53f, 0.84f, 0.96f);
            public Color assistantColor = new Color(0.76f, 0.96f, 0.44f);
            
            public Color bundlesColor = new Color(0.5f, 0.69f, 0.98f);
            public Color tagsColor = new Color(0.93f, 0.81f, 0.42f);

            public Color warningColor = new Color(1f, 0.76f, 0.03f);
            public Color errorColor = new Color(0.81f, 0.13f, 0.31f);
            public Color okColor = Color.white;
            public Color successColor = new Color(0f, 1f, 0.68f);
            public Color calloutColor = new Color(151/255f, 71/255f, 1.0f);
            
            #region Quest Node Colors
            public Color colorTrigger = new Color(0f, 1f, 0.68f);
            public Color colorKill = new Color(0.93f, 0.33f, 0.42f);
            public Color colorStealth = new Color(0.45f, 0.07f, 0.7f);
            public Color colorTake = new Color(0.16f, 0.7f, 0.57f);
            public Color colorRead = new Color(0.51f, 1f, 0.9f);
        
            public Color colorGive = new Color(1f, 0.72f, 0.92f);
            [FormerlySerializedAs("colorGiveTo")] public Color colorExchange = new Color(1f, 0.45f, 0.91f);
        
            public Color colorReport = new Color(0.41f, 0.63f, 1f);
            public Color colorSpy = new Color(0.78f, 0.79f, 1f);
            public Color colorListen = new Color(0.52f, 1f, 0.05f);
            
            
            public string DebugVectorGUID = "4fc870f9e2f488d4bb2c1bffe1f5b751";

            #endregion
        }
        
    }



}

