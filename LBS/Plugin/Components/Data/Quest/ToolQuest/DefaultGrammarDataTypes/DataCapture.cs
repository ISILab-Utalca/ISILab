using System;
using System.Collections.Generic;
using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Components
{

    [Serializable]
    public class DataCapture : QuestActionData
    {
        [SerializeField] public float captureTime = 5f;
        [SerializeField] public bool resetTimeOnExit = true;

        public DataCapture(QuestNode ownerNode, string tag) : base(ownerNode, tag)
        {
        }

        public override void Clone(QuestActionData data)
        {
            base.Clone(data);
            if (data is not DataCapture captureData) return;
            captureTime = captureData.captureTime;
            resetTimeOnExit = captureData.resetTimeOnExit;
        }

        public override bool Equals(QuestActionData other)
        {
            var captureOther = other as DataCapture;
            if(captureOther == null) return false;

            return Mathf.Approximately(captureOther.captureTime, captureTime);
        }

        public override bool IsValid()
        {
            return true;
        }

        public override void SetDataByTiles(List<LBSLayer> layers, List<TileBundleGroup> tiles)
        {
            // stub
        }
    }
}