using ISILab.LBS.Components;
using Newtonsoft.Json;
using System.Collections.Generic;
using ISILab.LBS.Plugin.Internal;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    [System.Serializable]
    public class LBSSimulationTagsCharacteristic : LBSCharacteristic
    {
        [JsonRequired]
        string tagName = "";

        [SerializeField, JsonIgnore]
        protected SimulationTag value;

        [JsonIgnore]
        public SimulationTag Value
        {
            get
            {
                if (value == null)
                    value = LBSAssetsStorage.Instance.Get<SimulationTag>().Find(i => i.Label == tagName);
                return value;
            }
            set
            {
                this.value = value;
                tagName = value.Label;
            }
        }

        public LBSSimulationTagsCharacteristic(SimulationTag value)
        {
            this.value = value;
            if (value != null)
                tagName = value.Label;
        }

        public LBSSimulationTagsCharacteristic()
        {
            this.value = null;
        }

        public override object Clone()
        {
            return new LBSSimulationTagsCharacteristic(this.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is LBSSimulationTagsCharacteristic))
                return false;
            var ch = (LBSSimulationTagsCharacteristic)obj;
            if (ch.value != this.value)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override List<string> Validate()
        {
            return new List<string>();
            throw new System.NotImplementedException();
        }
    }

}