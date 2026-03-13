using UnityEngine;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    public interface IBlueprintable
    {
        /// <summary>
        /// Should return an object (module, behavior, assistant, etc) with a clone 
        /// of the selected area
        /// </summary>
        /// <param name="min">top left corner</param>
        /// <param name="max">bottom right corner</param>
        /// <returns>true if there is any content within the area. Meaning to store the layer</returns>
        bool CaptureAreaData(Vector2Int min, Vector2Int max);

        /// <summary>
        /// Add an offset to the content of a given object type
        /// </summary>
        /// <param name="delta">value to apply to move an obejct</param>
        void SetPosition(Vector2Int parentAnchor, Vector2Int delta);

        /// <summary>
        /// Gets the anchor/origin of this layer's objects
        /// </summary>
        /// <returns>the top left object coordinate</returns>
        Vector2Int GetAnchor();
    }
}
