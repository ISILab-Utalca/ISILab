using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;




namespace ISILab.DevTools.Macros
{
    public static class AssetMacro
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
        
        
        public static Texture2D LoadPlaceholderTexture()
        {
            return LoadAssetByGuid<Texture2D>(PLACEHOLDER_TEXTURE_GUID);
        }
        
        
        public static VectorImage LoadPlaceholderVectorImage()
        {
            return LoadAssetByGuid<VectorImage>(PLACEHOLDER_UI_VECTOR_ICON_G_UID);
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
            var current = element;
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
