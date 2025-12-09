
using ISILab.Commons.Extensions;
using ISILab.Extensions;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ISILab.LBS.Macros
{
    public static class LBSAssetMacro
    {

        private const string PLACEHOLDER_TEXTURE_GUID = "edcbfe04a88995d49aabd5bf8ee28e79";
        private const string PLACEHOLDER_UI_VECTOR_ICON_G_UID = "5aa5737462342b24c866198641cdaf08";

        /// <summary>
        /// Loads an asset of type T from its GUID.
        /// </summary>
        /// <typeparam name="T">Type of the asset to load.</typeparam>
        /// <param name="guid">The GUID of the asset.</param>
        /// <returns>The loaded asset of type T, or null if not found.</returns>
        public static T LoadAssetByGuid<T>(string _guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(_guid);
            return !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<T>(path) : null;
        }


        /// <summary>
        /// Retrieves the GUID of the given asset.
        /// </summary>
        /// <param name="_asset">The asset to retrieve the GUID from.</param>
        /// <returns>The GUID as a string, or null if not found.</returns>
        public static string GetGuidFromAsset(Object _asset)
        {
            string path = AssetDatabase.GetAssetPath(_asset);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        /// <summary>
        /// Tries to return a LBSTag
        /// </summary>
        /// <param name="tag">The tag name that you are looking for</param>
        /// <returns></returns>
        public static LBSTag GetLBSTag(string tag)
        {
            List<LBSTag> lbsTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            return lbsTags.FirstOrDefault(lbsTag => lbsTag.Label == tag);
        }


        public static Texture2D LoadPlaceholderTexture()
        {
            return LoadAssetByGuid<Texture2D>(PLACEHOLDER_TEXTURE_GUID);
        }


        public static VectorImage LoadPlaceholderVectorImage()
        {
            return LoadAssetByGuid<VectorImage>(PLACEHOLDER_UI_VECTOR_ICON_G_UID);
        }

        public static IEnumerable<LBSTag> GetTagsFromBundle(Bundle bundle, List<string> Filter = null)
        {
            List<LBSTagsCharacteristic> characteristics = bundle.GetCharacteristics<LBSTagsCharacteristic>();

            foreach (LBSTagsCharacteristic ch in characteristics)
            {
                if (ch is LBSTagsCharacteristic tagChar)
                {
                    foreach (TagCharacteristicEntry entry in tagChar.TagEntries)
                    {
                        LBSTag tag = entry?.Value;

                        if (tag == null) continue;

                        if (Filter == null || Filter.Count == 0) yield return tag;

                        else if (Filter.Contains(tag.name)) yield return tag;

                    }
                }
            }
        }

        public static T GetRandomBundleWithTag<T>(IEnumerable<T> bundles, string tagName) where T : Bundle
        {
            List<T> valid = new List<T>();

            foreach (var b in bundles)
            {
                // Check bundle tags with your filter-based function
                var tags = GetTagsFromBundle(b, new List<string> { tagName });

                if (tags.Any())
                    valid.Add(b);
            }

            if (valid.Count == 0)
                return null; // or throw, or handle however you want

            return valid.Random();
        }


        public static bool BundleHasTag(Bundle b, string tagName)
        {
            if (b == null || string.IsNullOrEmpty(tagName))
                return false;

            // Get all LBSTagsCharacteristic associated with the bundle
            List<LBSTagsCharacteristic> tagCharacteristics = b.GetCharacteristics<LBSTagsCharacteristic>();
            if (tagCharacteristics == null)
                return false;

            // Check whether any TagCharacteristicEntry matches the provided name
            foreach (LBSTagsCharacteristic ch in tagCharacteristics)
            {
                if (ch.TagEntries.Any(t => t.TagName == tagName))
                    return true;
            }

            return false;
        }

        public static List<string> GetAllTagNames(Bundle b)
        {
            List<string> result = new List<string>();

            if (b == null)
                return result;

            List<LBSTagsCharacteristic> tagCharacteristics = b.GetCharacteristics<LBSTagsCharacteristic>();
            if (tagCharacteristics == null)
                return result;

            foreach (LBSTagsCharacteristic ch in tagCharacteristics)
            {
                if (ch.TagEntries == null)
                    continue;

                foreach (TagCharacteristicEntry entry in ch.TagEntries)
                {
                    if (entry?.Value != null)
                        result.Add(entry.Value.Label);
                }
            }

            return result;
        }

        public static string GetActiveSceneGUID()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (!currentScene.isLoaded) return string.Empty;

           return  AssetDatabase.AssetPathToGUID(currentScene.path);
        }

    }
}

