using System;
using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Characteristics;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ISILab.LBS.Assistants
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/WFCPreset")]
    public class WFCPreset : ScriptableObject
    {
        [SerializeField]
        string presetName = "New WFC preset";

        [SerializeField]
        List<LBSDirectionedGroup.WeightStruct> weights = new List<LBSDirectionedGroup.WeightStruct>();

        [SerializeField, HideInInspector]
        string assetGUID;

        public string Name
        {
            get => presetName;
            set => presetName = name = value;
        }

        public string AssetGUID
        {
            get => assetGUID;
            private set => assetGUID = value;
        }

        public List<LBSDirectionedGroup.WeightStruct> GetWeights() => new List<LBSDirectionedGroup.WeightStruct>(weights);

        public void SetWeights(List<LBSDirectionedGroup.WeightStruct> newWeights)
        {
            weights = new List<LBSDirectionedGroup.WeightStruct>(newWeights);
        }

        public void SetAssetGUID(string guid) => AssetGUID = guid;
    }
}
