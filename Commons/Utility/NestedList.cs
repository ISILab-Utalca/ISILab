using System.Collections.Generic;
using UnityEngine;

namespace ISILab.Commons.Utility
{
    /// <summary>
    /// A list container that can be serialized by Unity.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class NestedList<T>
    {
        /// <summary>
        /// The real list stored.
        /// </summary>
        [SerializeField]
        public List<T> list = new();

        /// <summary>
        /// Indexer to access the list stored.
        /// </summary>
        /// <param name="index">Element in the list.</param>
        /// <returns>The element of the list at the indicated position.</returns>
        public T this[int index]
        {
            get { return list[index]; }
            set { list[index] = value; }
        }

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Add(T item) => list.Add(item);
    }
}
