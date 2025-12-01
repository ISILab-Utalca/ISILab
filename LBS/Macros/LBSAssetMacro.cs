using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Internal;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


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
        
                        else if(Filter.Contains(tag.name)) yield return tag;
   
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

    }

    public class LBSLayerHelper
    {

        /// <summary>
        /// Retrieves an object of type T from the Behaviours, Assistants, or Modules list within the given layerChild's OwnerLayer.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="layerChild">An object with an OwnerLayer property that contains Behaviours, Assistants, and Modules</param>
        /// <returns>The first matching object of type T found, or null if not found.</returns>
        public static T GetObjectFromLayerChild<T>(object layerChild) where T : class
        {
            // Use reflection to get the OwnerLayer property
            PropertyInfo ownerLayerProp = layerChild.GetType().GetProperty("OwnerLayer");
            if (ownerLayerProp == null) return null;

            var ownerLayer = ownerLayerProp.GetValue(layerChild);
            if (ownerLayer == null) return null;

            // Look for the T in Behaviours, Assistants, and Modules
            foreach (var listName in new[] { "Behaviours", "Assistants", "Modules" })
            {
                PropertyInfo listProp = ownerLayer.GetType().GetProperty(listName);
                if (listProp == null) continue;

                var list = listProp.GetValue(ownerLayer) as IEnumerable<object>;

                var match = list?.FirstOrDefault(b => b is T);
                if (match != null) return match as T;
            }

            return null;
        }

        /// <summary>
        /// Retrieves an object of type T from the Behaviours, Assistants, or Modules list within the provided layer.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="layer">An object that contains Behaviours, Assistants, and Modules properties.</param>
        /// <returns>The first matching object of type T found, or null if not found.</returns>
        public static T GetObjectFromLayer<T>(object layer) where T : class
        {
            if (layer == null) return null;

            foreach (var listName in new[] { "Behaviours", "Assistants", "Modules" })
            {
                PropertyInfo listProp = layer.GetType().GetProperty(listName);
                if (listProp == null) continue;

                var list = listProp.GetValue(layer) as IEnumerable<object>;
                var match = list?.FirstOrDefault(b => b is T);
                if (match != null) return match as T;
            }

            return null;
        }

        public static Tuple<LBSLayer, TileBundleGroup> GetBundleTileByPosition(Vector2Int TilePosition, List<LBSLayer> Layers)
        {
            foreach (LBSLayer layer in Layers)
            {
                // Only check layers with a PopulationBehaviour
                PopulationBehaviour population = layer.GetBehaviour<PopulationBehaviour>();
                if (population == null)
                    continue;

                TileBundleGroup tileGroup = population.GetTileGroup(TilePosition);
                if (tileGroup != null)
                {
                    return Tuple.Create(layer, tileGroup);
                }
            }

            return null; // nothing found
        }

        public static Tuple<LBSLayer, TileBundleGroup> GetBundleTileByMouse(Vector2Int mousePosition, List<LBSLayer> Layers)
        {
            foreach (LBSLayer layer in Layers)
            {
                // Only check layers with a PopulationBehaviour
                PopulationBehaviour population = layer.GetBehaviour<PopulationBehaviour>();
                if (population == null)
                    continue;

                TileBundleGroup tileGroup = population.GetTileGroup(population.OwnerLayer.ToFixedPosition(mousePosition));
                if (tileGroup != null)
                {
                    return Tuple.Create(layer, tileGroup);
                }
            }

            return null; // nothing found
        }

    }

    public static class LBSVisualElementHelper
    {

        /// <summary>
        /// Searches on the parents of a visual element until a parent of a given VisualElement class is found
        /// </summary>
        /// <param name="element">the element from which we will search the parent</param>
        /// <typeparam name="T">VisualElement class we are searching for</typeparam>
        /// <returns></returns>
        public static T FindParentOfType<T>(VisualElement element) where T : VisualElement
        {
            VisualElement current = element;
            while (current != null)
            {
                if (current is T target)
                    return target;
                current = current.parent;
            }
            return null;
        }

    }
}
