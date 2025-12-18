using PathOS;
using ISILab.Commons.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Modules
{
    [System.Serializable]
    public struct SimulationEntityData
    {
        public VectorImage image;
        public Color color;

        public SimulationEntityData(VectorImage image, Color color)
        {
            this.image = image;
            this.color = color;
        }
    }

    public enum TierEntity
    {
        None,
        Low,
        Med,
        High
    }

    [CreateAssetMenu(fileName = "PathOSStorage", menuName = "ISILab/LBS/PathOS/PathOSStorage")]
    public class PathOSStorage : ScriptableObject
    {
        #region FIELDS


        [System.NonSerialized]
        private static PathOSStorage instance;

        public PathOSAgent agentPrefab;
        public PathOSManager managerPrefab;
        public PathOSWorldCamera worldCameraPrefab;
        public ScreenshotManager screenshotCameraPrefab;

        public SimulationEntityData agentData;

        public LBSDictionary<EntityType, SimulationEntityData> entityDataPool = new();
        #endregion

        #region PROPERTIES

        public static PathOSStorage Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<PathOSStorage>("PathOSStorage");                    
                return instance;
            }
        }

        #endregion

        #region METHODS


        public VectorImage GetEntityImage(EntityType entity) => entityDataPool[entity].image;

        public static TierEntity GetTier(EntityType type)
        {
            string name = type.ToString();

            if (name.Contains("_LOW"))
                return TierEntity.Low;

            if (name.Contains("_MED"))
                return TierEntity.Med;

            if (name.Contains("_HIGH"))
                return TierEntity.High;

            return TierEntity.None;
        }


        private void Awake() => AssignSingleton();

        private void AssignSingleton()
        {
            if (instance == null) instance = Resources.Load<PathOSStorage>("PathOSStorage");
            else instance = this;
        }

        private void OnEnable() => AssignSingleton();

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
#endif
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
#endif
        }
        #endregion

    }
}
