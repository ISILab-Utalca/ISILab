using ISILab.LBS.Components;
using ISILab.LBS.Internal;
using ISILab.LBS.Macros;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Characteristics
{
    [Serializable]
    public class TagCharacteristicEntry
    {
        [SerializeField, JsonRequired]
        string tagName = "";

        [SerializeField, JsonRequired/*, JsonIgnore*/]
        protected LBSTag value;

        [SerializeField, JsonRequired]
        protected string tagGUID = "";


        [JsonIgnore]
        public LBSTag Value
        {
            get
            {
                if (value == null)
                    //value = LBSAssetsStorage.Instance.Get<LBSTag>().Find(i => i.Label == tagName);
                    value = LBSAssetMacro.LoadAssetByGuid<LBSTag>(tagGUID);
                return value;
            }
            set
            {
                this.value = value;
                tagName = value.Label;
            }
        }

        [JsonIgnore]
        public string TagGUID
        {
            get
            {
                string s = "";
                if (string.IsNullOrEmpty(tagGUID))
                {
                    tagGUID = LBSAssetMacro.GetGuidFromAsset(value);
                    s += $"Null or Empty Tag GUID -> Calling GetGuidFromAsset( {value} )\n"; // Por alguna razon hay veces en que 'value' se muestra como Material??? Pero no pasa con bundles de population
                }
                s += $"Tag GUID = {tagGUID}";
                //Debug.Log(s);
                return tagGUID;
            }
        }


        [JsonIgnore]
        public string TagName => tagName;

        public TagCharacteristicEntry()
        {
        }

        public TagCharacteristicEntry(LBSTag value)
        {
            this.value = value;
            tagGUID = LBSAssetMacro.GetGuidFromAsset(value);
            UpdateName();
        }

        internal void UpdateName()
        {
            tagName = value.Label;
        }
    }

    [System.Serializable]
    [LBSCharacteristic("Tags", "")]
    public class LBSTagsCharacteristic : LBSCharacteristic, ISerializationCallbackReceiver
    {
        public new static readonly bool unique = false;

        List<TagCharacteristicEntry> tags = new();
    
        public List<TagCharacteristicEntry> Tags => tags;

        public object Value { get; internal set; }

        public LBSTagsCharacteristic(List<LBSTag> tags)
        {
            foreach (var tag in tags)
            {
                this.tags.Add(new TagCharacteristicEntry(tag));
            }
        }


        public LBSTagsCharacteristic()
        {
            //Debug.Log("CONSTRUCTOR SIN PARAMETROS INVOCADO [Value: " + value + ", TagName: " + tagName + "]");
            //Value = LBSAssetMacro.LoadAssetByGuid<LBSTag>(tagGUID);
          
        }

        public LBSTagsCharacteristic(LBSTag tag)
        {
            tags.Add(new TagCharacteristicEntry(tag));
        }

        public override object Clone()
        {
            List<LBSTag> cloneTags = new();
            foreach (var tagEntry in tags)
            {
                cloneTags.Add(tagEntry.Value);
            }
            return new LBSTagsCharacteristic(cloneTags);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not LBSTagsCharacteristic ch)
                return false;

            if (ch.tags.Count != tags.Count)
                return false;

            foreach (var tagEntry in tags)
            {
                if (!ch.tags.Exists(t => t.Value == tagEntry.Value))
                    return false;
            }

            return true;
        }


        public void AddTag(LBSTag tag)
        {
            tags.Add(new TagCharacteristicEntry(tag));
        }

        public void RemoveTag(LBSTag tag)
        {
            tags.RemoveAll(t => t.Value == tag);
        }

        public override string ToString()
        {
            return tags.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        public override List<string> Validate()
        {
            List<string> warnings = new List<string>();

            foreach (var tagEntry in tags)
            {
                if (tagEntry.Value == null)
                {
                    warnings.Add($"The tag '{tagEntry}' in LBSTagsCharacteristic is null.");
                }
            }            
            return warnings;
        }

        public void OnBeforeSerialize()
        {
            //Debug.Log("Before Deserialize");
            foreach(var tagEntry in tags)
            {
                var _ = tagEntry.TagGUID;
                if (tagEntry.Value != null) tagEntry.UpdateName();
            }
      
        }

        public void OnAfterDeserialize()
        {
            //Debug.Log("After Deserialize");
        }
    }
}