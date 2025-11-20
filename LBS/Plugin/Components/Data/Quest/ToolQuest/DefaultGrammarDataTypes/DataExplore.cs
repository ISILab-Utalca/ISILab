using System;
using System.Collections.Generic;
using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class DataExplore : QuestActionData
    {
        [SerializeField] public int subdivisions = 4;
    
            
        // if find random position is true, then upon generation a random position is created and that's what the 
        // player must trigger
        [SerializeField] public bool findRandomPosition;

        public DataExplore(QuestNode ownerNode, string tag) : base(ownerNode, tag)
        {
        }
            
        public override void Clone(QuestActionData data)
        {
            base.Clone(data);
            if (data is not DataExplore exploreData) return;
            subdivisions = exploreData.subdivisions;
            findRandomPosition = exploreData.findRandomPosition;
        }

        public override bool Equals(QuestActionData other)
        {
            var exploreOther = other as DataExplore;
            if(exploreOther == null) return false;
            
            return exploreOther.subdivisions == subdivisions && 
                   exploreOther.findRandomPosition ==  findRandomPosition;
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