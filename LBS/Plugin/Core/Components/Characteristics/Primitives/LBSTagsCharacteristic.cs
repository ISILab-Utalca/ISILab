using ISILab.LBS.Components;
using System.Collections.Generic;
using ISILab.DevTools.Macros;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    [Serializable]
    public class TagCharacteristicEntry
    {
        [SerializeField, JsonRequired]
        string tagName = "";

        [SerializeField, SerializeReference, JsonRequired/*, JsonIgnore*/]
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
                    value = AssetMacro.LoadAssetByGuid<LBSTag>(tagGUID);
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
                    tagGUID = AssetMacro.GetGuidFromAsset(value);
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
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            tagName = value.Label;
            //tagGUID = AssetMacro.GetGuidFromAsset(value);
        }
        public void OnBeforeSerialize()
        {
            if (value != null)
            {
                tagGUID = AssetMacro.GetGuidFromAsset(value);
                tagName = value.Label;
            }
        }
    }

    [System.Serializable]
    //[LBSCharacteristic("Tags", "")]
    public class LBSTagsCharacteristic : LBSCharacteristic, ISerializationCallbackReceiver
    {
        public new static readonly bool unique = false;

        [SerializeField]
        List<TagCharacteristicEntry> tagEntries = new();

        public List<TagCharacteristicEntry> TagEntries
        {
            get => tagEntries;
        }

        public List<LBSTag> Tags
        {
            get
            {
                // only return valid entries
                List<LBSTag> tags = new();
                foreach (var entry in tagEntries)
                {
                    if (entry.Value is null) continue;
                    tags.Add(entry.Value);
                }
                return tags;
            }
        }

        [System.Obsolete]
        public List<TagCharacteristicEntry> Value { get; internal set; }

        public LBSTagsCharacteristic(List<LBSTag> tags)
        {
            foreach (var tag in tags)
            {
                this.tagEntries.Add(new TagCharacteristicEntry(tag));
            }
        }


        public LBSTagsCharacteristic()
        {
            //Debug.Log("CONSTRUCTOR SIN PARAMETROS INVOCADO [Value: " + value + ", TagName: " + tagName + "]");
            //Value = LBSAssetMacro.LoadAssetByGuid<LBSTag>(tagGUID);
          
        }

        public LBSTagsCharacteristic(LBSTag tag)
        {
            tagEntries.Add(new TagCharacteristicEntry(tag));
        }

        public LBSTag this[int index]
        {
            get => tagEntries[index].Value;
        }

        public override object Clone()
        {
            List<LBSTag> cloneTags = new();
            foreach (var tagEntry in tagEntries)
            {
                if (tagEntry.Value != null) cloneTags.Add(tagEntry.Value);
            }
            return new LBSTagsCharacteristic(cloneTags);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj is not LBSTagsCharacteristic ch)
                return false;

            if (ch.tagEntries.Count != tagEntries.Count)
                return false;

            foreach (var tagEntry in tagEntries)
            {
                if (!ch.tagEntries.Exists(t => t.Value == tagEntry.Value))
                    return false;
            }

            return true;
        }


        public void AddTag(LBSTag tag)
        {
            tagEntries.Add(new TagCharacteristicEntry(tag));
        }

        public void RemoveTag(LBSTag tag)
        {
            tagEntries.RemoveAll(t => t.Value == tag);
        }

        public bool HasTag(LBSTag tag)
        {
            return tagEntries.Exists(t => t.Value == tag);
        }

        public override string ToString()
        {
            string s = "[";
            for(int i = 0; i <  tagEntries.Count; i++)
            {
                s += tagEntries[i].TagName + ", ";
            }
            s = s.Substring(0, s.Length - 2);
            s += "]";
            return s;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        public override List<string> Validate()
        {
            List<string> warnings = new List<string>();

            if (tagEntries is null || tagEntries.Count == 0)
                warnings.Add($"LBSTagsCharacteristic is empty.");

            foreach (var tagEntry in tagEntries)
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
            /*
            List<TagCharacteristicEntry> validEntries = new();
            List<TagCharacteristicEntry> invalidEntries = new();
            
            // get valids
            foreach (var entry in tagEntries)
            {
                if (entry.Value == null) invalidEntries.Add(entry);
            }

            // remove invalids
            foreach (var invalid in invalidEntries)
            {
                tagEntries.Remove(invalid);
            }


            foreach (var tagEntry in tagEntries)
            {
                if (tagEntry.Value != null) tagEntry.UpdateInfo();
            }
            */

            foreach (var tagEntry in tagEntries)
            {
                if (tagEntry != null) tagEntry.OnBeforeSerialize();
            }


        }

        public void OnAfterDeserialize()
        {
            //Debug.Log("After Deserialize");
        }
    }
}