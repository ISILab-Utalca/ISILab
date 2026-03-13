using ISILab.Commons.JsonNet;
using ISILab.LBS.Plugin.MapTools.Generators;
using Newtonsoft.Json;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.Settings
{

    public struct LBSLog
    {
        public string message;
        public LogType type;
        public int duration;

        // Makes an empty message that is not actually displayed
        public LBSLog(int anyValue)
        {
            message = string.Empty;
            type = LogType.Log;
            duration = 0;
        }

        public LBSLog(string message = "ISILab:", LogType type = LogType.Log, int duration = 3)
        {
            this.message = message;
            this.type = type;
            this.duration = duration;
        }
    }
    public enum OptimizationGenMode
    {
        None,
        Batch,
        JoinGeometry,
        GpuInstancing
    }

    [System.Serializable]
    public class LBSGenerator3DSettings
    {
        [SerializeField] 
        [JsonConverter(typeof(bool))]
        public bool useBundleSize = false;
            
        [SerializeField] 
        [JsonConverter(typeof(bool))]
        public bool lightVolume = false;
            
        [SerializeField] 
        [JsonConverter(typeof(bool))]
        public bool reflectionProbe = false;
            
        [SerializeField]
        [JsonConverter(typeof(Vector2Converter))]
        public Vector3 scale = new Vector3(2, 2, 2);

        [SerializeField]
        [JsonConverter(typeof(Vector2Converter))]
        public Vector2 resize = new Vector2(0, 0);

        [SerializeField]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 position = new Vector3(0, 0, 0);

        [SerializeField]
        [JsonConverter(typeof(bool))]
        public bool replacePrevious = true;

        [SerializeField]
        [JsonConverter(typeof(bool))]
        public bool buildLightProbes = true;

        [SerializeField]
        [JsonConverter(typeof(bool))]
        public bool bakeLights = false;

        [SerializeField]
        public OptimizationGenMode optimization3d = OptimizationGenMode.None;

        [SerializeField]
        [HideInInspector]
        public string generatedRootName = "DEFAULT";
            
        [SerializeField]
        public string rootParentName = "Root_Parent";

        public override bool Equals(object obj)
        {
            var other = obj as LBSGenerator3DSettings;

            // check if other have the same type
            if (other is not LBSGenerator3DSettings) return false;

            // check if scale is the same
            if (!this.scale.Equals(other.scale)) return false;

            // check if resize is the same
            if (!this.resize.Equals(other.resize)) return false;

            // check if position is the same
            if (!this.position.Equals(other.position)) return false;

            // check if name is the same
            if (this.generatedRootName != other.generatedRootName) return false;

            // check if rootParentName is the same
            if (this.rootParentName != other.rootParentName) return false;

            if (this.useBundleSize != other.useBundleSize) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
} 