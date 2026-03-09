using UnityEngine;

namespace ISILab.Commons.Extensions
{
    public static class RectExtensions
    {
        /// <summary>
        /// Gets the minimum and maximum x and y axis points of the two rectangles (forming the area), 
        /// and applies said area to itself.
        /// </summary>
        public static void GetCombinedArea(this ref Rect rect, Rect a, Rect b)
        {
            rect.xMin = Mathf.Min(a.xMin, b.xMin);
            rect.xMax = Mathf.Max(a.xMax, b.xMax);
            rect.yMin = Mathf.Min(a.yMin, b.yMin);
            rect.yMax = Mathf.Max(a.yMax, b.yMax);
        }

        /// <summary>
        /// Gets the minimum and maximum x and y axis points of it's own rectangle and another (forming the area), 
        /// and applies said area to itself.
        /// </summary>
        public static void GetCombinedArea(this ref Rect rect, Rect b)
        {
            rect.xMin = Mathf.Min(rect.xMin, b.xMin);
            rect.xMax = Mathf.Max(rect.xMax, b.xMax);
            rect.yMin = Mathf.Min(rect.yMin, b.yMin);
            rect.yMax = Mathf.Max(rect.yMax, b.yMax);
        }

        /// <summary>
        /// Indicates whether the evaluated rectangle has a non-zero equivalent area or not.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool HasArea(this Rect rect) => rect.width > 0 && rect.height > 0;

        /// <summary>
        /// Converts a 2D position to an index in a 1D array.
        /// </summary>
        /// <param name="pos">The 2D position to convert.</param>
        public static int ToIndex(this Rect rect, Vector2 pos)
        {
            if (pos.x < 0 || pos.x >= rect.width || pos.y < 0 || pos.y >= rect.height)
                return -1;
            return (int)(pos.y * rect.width + pos.x);
        }

        public static int GlobalToIndex(this Rect rect, Vector2 pos) => rect.ToIndex(pos - rect.position);

        /// <summary>
        /// Converts an index in a 1D array to a 2D position.
        /// </summary>
        /// <param name="index">The index in the 1D array to convert.</param>
        public static Vector2Int ToMatrixPosition(this Rect rect, int index)
        {
            return new Vector2Int((int)(index % rect.width), (int)(index / rect.width));
        }

        public static Vector2Int ToGlobalPosition(this Rect rect, int index) => rect.ToMatrixPosition(index) + Vector2Int.RoundToInt(rect.position);
    }
}
