using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace ISILab.LBS.Plugin.UI.Editor
{
    /// <summary>
    /// Stores visual GraphElements associated with logical components (keys), grouped by layer.
    /// </summary>
    public class LayerContainer
    {
        private readonly Dictionary<object, List<GraphElement>> _pairs = new();


        /// <summary>
        /// Adds a GraphElement under a specific key (component or drawer).
        /// </summary>
        /// <param name="obj">Key object (e.g., tile, behavior, etc.).</param>
        /// <param name="element">GraphElement to be added.</param>
        public void AddElement(object obj, GraphElement element)
        {
            if (!_pairs.TryGetValue(obj, out var list))
            {
                list = new List<GraphElement>();
                _pairs[obj] = list;
            }

            list.Add(element);
        }

        /// <summary>
        /// Gets the list of GraphElements associated with a key.
        /// </summary>
        /// <param name="obj">Key object.</param>
        /// <returns>List of GraphElements, or null if not found.</returns>
        public List<GraphElement> GetElement(object obj)
        {
            return _pairs.GetValueOrDefault(obj);
        }

        /// <summary>
        /// Clears and removes the GraphElements for a specific key.
        /// </summary>
        /// <param name="obj">Key object.</param>
        /// <returns>Removed list of GraphElements, or null if not found.</returns>
        public List<GraphElement> ClearElement(object obj)
        {
            return _pairs.Remove(obj, out var list) ? list : null;
        }

        /// <summary>
        /// Forces all GraphElements tied to a key to repaint.
        /// </summary>
        /// <param name="obj">Key object.</param>
        public void Repaint(object obj)
        {
            if (!_pairs.TryGetValue(obj, out var elements)) return;
            foreach (var element in elements)
            {
                element.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Clears the entire container, removing all keys and their GraphElements.
        /// </summary>
        /// <returns>A flat list of all removed GraphElements.</returns>
        public List<GraphElement> Clear()
        {
            var erasedElements = new List<GraphElement>();
            for (int i = 0; i < _pairs.Count; i++)
            {
                var list = _pairs.ElementAt(i).Value;
                for (int j = 0; j < list.Count; j++)
                {
                    var graph = list[j];

                    erasedElements.Add(graph);
                    list.RemoveAt(j);
                    j--;
                }

                if (list.Count > 0) continue;
                _pairs.Remove(_pairs.ElementAt(i).Key);
                i--;
            }
            return erasedElements;
        }

        public List<GraphElement> Delete()
        {
            var elements = Clear();
            _pairs.Clear();
            return elements;
        }

        /// <summary>
        /// Returns all GraphElements in this container, flattened into a single list.
        /// </summary>
        public List<GraphElement> GetAllElements()
        {
            return _pairs.Values.SelectMany(list => list).ToList();
        }

    }

}