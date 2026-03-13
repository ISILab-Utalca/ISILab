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
        /// <returns>a clone of the object</returns>
        void KeepAreaData(Vector2Int min, Vector2Int max);

        /// <summary>
        /// Add an offset to the content of a given object type
        /// </summary>
        /// <param name="offset">the coordinates to offset by</param>
        void OffsetObject(Vector2Int offset);
    }
}
