using UnityEngine;

namespace ISILab.Commons.Extensions
{
    /// <summary>
    /// Class containing extension methods for GameObject.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Sets the parent of a GameObject.
        /// </summary>
        /// <param name="gameObject">The child object.</param>
        /// <param name="other">The new parent object.</param>
        public static void SetParent(this GameObject gameObject, GameObject other)
        {
            gameObject.transform.parent = other.transform;
        }
    }
}