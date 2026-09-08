using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace ISILab.LBS
{

    [System.Serializable]
    public class LoadedLevel : ScriptableObject
    {
        [JsonRequired]
        public string fullName = "";
        [JsonRequired]
        public LBSLevelData data;

        public FileInfo FileInfo
        {
            get
            {
                try
                {
                    var fileInfo = new FileInfo(fullName);
                    return fileInfo;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static LoadedLevel CreateInstance(LBSLevelData data, string fullName)
        {
            var level = ScriptableObject.CreateInstance<LoadedLevel>();
            level.data = data;
            level.fullName = fullName;
            return level;
        }
    }
}
