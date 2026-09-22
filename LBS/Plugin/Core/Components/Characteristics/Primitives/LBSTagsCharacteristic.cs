using ISILab.LBS.Components;
using System.Collections.Generic;
using ISILab.DevTools.Macros;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Container for a single <see cref="LBSTag"/>.
    /// </summary>
    [Serializable]
    public class TagCharacteristicEntry
    {
        [SerializeField, JsonRequired]
        string tagName = "";

        [SerializeField, SerializeReference, JsonRequired/*, JsonIgnore*/]
        LBSTag value;

        [SerializeField, JsonRequired]
        string tagGUID = "";

        /// <summary>
        /// The tag contained in this entry. If for some reason it is not assigned, an attempt will be made to load it using its stored GUID.
        /// </summary>
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

        /// <summary>
        /// The GUID of the <see cref="LBSTag"/> contained. If not assigned, it will be consulted from the tag asset.
        /// </summary>
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

        /// <summary>
        /// The name of the contained tag.
        /// </summary>
        [JsonIgnore]
        public string TagName => tagName;

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public TagCharacteristicEntry() { }

        /// <summary>
        /// Creates an entry containing the specified <see cref="LBSTag"/>.
        /// </summary>
        /// <param name="value"></param>
        public TagCharacteristicEntry(LBSTag value)
        {
            this.value = value;
            UpdateInfo();
        }

        /// <summary>
        /// Updates fields values using the current tag data.
        /// </summary>
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

    /// <summary>
    /// Characteristic that holds a list of <see cref="LBSTag"/>.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Tags", "")]
    public class LBSTagsCharacteristic : LBSCharacteristic, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Overwritten to allow duplicates of this characteristic in the same <see cref="Plugin.Components.Bundles.Bundle"/>.
        /// </summary>
        public new static readonly bool unique = false;

        [SerializeField]
        List<TagCharacteristicEntry> tagEntries = new();

        /// <summary>
        /// List of <see cref="LBSTag"/> entries contained in this characteristic.
        /// </summary>
        public List<TagCharacteristicEntry> TagEntries
        {
            get => tagEntries;
        }

        /// <summary>
        /// Direct access to contained <see cref="LBSTag"/>.
        /// </summary>
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

        //[System.Obsolete]
        //public List<TagCharacteristicEntry> Value { get; internal set; }


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

        /// <summary>
        /// Creates a tag characteristic with a single <see cref="LBSTag"/> entry.
        /// </summary>
        /// <param name="tag"><see cref="LBSTag"/> to assign to this characteristic.</param>
        public LBSTagsCharacteristic(LBSTag tag)
        {
            tagEntries.Add(new TagCharacteristicEntry(tag));
        }

        /// <summary>
        /// Indexer for accessing <see cref="LBSTag"/> elements directly.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>A <see cref="LBSTag"/> from the <see cref="TagCharacteristicEntry"/> list, specified by an index.</returns>
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

        /// <summary>
        /// Creates a new entry with the specified <see cref="LBSTag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="LBSTag"/> to add to this characteristic.</param>
        public void AddTag(LBSTag tag)
        {
            tagEntries.Add(new TagCharacteristicEntry(tag));
        }

        /// <summary>
        /// Removes an existing entry of a <see cref="LBSTag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="LBSTag"/> to remove from this characteristic.</param>
        public void RemoveTag(LBSTag tag)
        {
            tagEntries.RemoveAll(t => t.Value == tag);
        }

        /// <summary>
        /// Checks if a specified <see cref="LBSTag"/> has an existing entry in this characteristic.
        /// </summary>
        /// <param name="tag">The <see cref="LBSTag"/> whose existence is being consulted.</param>
        /// <returns>True if an entry with the specified <see cref="LBSTag"/> exists in this characteristic. False otherwise.</returns>
        public bool HasTag(LBSTag tag)
        {
            return tagEntries.Exists(t => t.Value == tag);
        }

        public override string ToString()
        {
            string s = "[";
            for(int i = 0; i < tagEntries.Count; i++)
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