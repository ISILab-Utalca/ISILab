using System;
using System.Collections.Generic;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Core.Settings;
using LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Components
{
    [Serializable]
    public class DataSpy : QuestActionData
    {
        [SerializeField] public BundleGraph bundleToSpy;
        
        private readonly Bundle.EElementFlag[] validToSpyTags = 
        {
            Bundle.EElementFlag.Character
        }; 
        
        [SerializeField] public float spyTime = 5f;
        [SerializeField] public bool resetTimeOnExit = true;
        public DataSpy(QuestNode ownerNode, string tag) : base(ownerNode, tag)
        {
            iconGuid = FoeIcon;
            bundleToSpy = new BundleGraph(this);
            color = LBSSettings.Instance.view.colorSpy;
        }
          
        public override void Clone(QuestActionData data)
        {
            base.Clone(data);
            if (data is not DataSpy spyData) return;
            bundleToSpy = spyData.bundleToSpy;
            spyTime = spyData.spyTime;
            resetTimeOnExit = spyData.resetTimeOnExit;
        }
          
        public override List<string> ReferencedLayerNames()
        {
            List<string> list = new List<string> { bundleToSpy.GetLayerName() };
            return list;
        }
          
        public override void Resize()
        {
            if (bundleToSpy.Valid())area = bundleToSpy.Area;
        }

        public override bool Equals(QuestActionData other)
        {
            var spyOther =  other as DataSpy;
            if (spyOther is null) return false;
           
            return Equals(spyOther.bundleToSpy, bundleToSpy);
        }

        public override bool IsValid()
        {
            return bundleToSpy.Valid();
        }

        public override void SetDataByTiles(List<LBSLayer> layers, List<TileBundleGroup> tiles)
        {
            TrySetBundleGraph(layers, tiles, ref bundleToSpy, validToSpyTags);
        }
    }
}