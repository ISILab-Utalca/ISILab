using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.Macros
{
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


        /// <summary>
        /// Captures a portion of a <see cref="GraphView"/> and returns the result as a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> that contains the graph.</param>
        /// <param name="graph">The target <see cref="GraphView"/> to capture.</param>
        /// <param name="captureArea">The area of the graph (in content coordinates) to capture.</param>
        /// <param name="onCaptured">Callback invoked with the generated <see cref="Texture2D"/>.</param>
        public static void CaptureGraphView(EditorWindow window, GraphView graph, Rect captureArea, Action<Texture2D> onCaptured)
        {
            if (window == null || graph == null)
                return;

            Action restore = DisplayGraphOnly(window, graph);
            EditorApplication.delayCall += () =>
            {
                float dpi = EditorGUIUtility.pixelsPerPoint;

                // Convert capture area (content space) → panel space
                Rect panelRect = graph.contentViewContainer
                    .ChangeCoordinatesTo(window.rootVisualElement, captureArea);

                // Panel height (needed because ReadPixels uses bottom-left origin)
                float panelHeight = window.rootVisualElement.worldBound.height;

                // Convert to pixel space
                int x = Mathf.RoundToInt(panelRect.x * dpi);
                int y = Mathf.RoundToInt((panelHeight - panelRect.y - panelRect.height) * dpi);
                int width = Mathf.RoundToInt(panelRect.width * dpi);
                int height = Mathf.RoundToInt(panelRect.height * dpi);

                if (width <= 0 || height <= 0)
                {
                    Debug.LogWarning("Capture rect invalid.");
                    restore?.Invoke();
                    return;
                }

                Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(x, y, width, height), 0, 0);
                tex.Apply();

                restore?.Invoke();
                onCaptured?.Invoke(tex);
            };
        }



        /// <summary>
        /// Hides everything in the graph view and returns an action which will restore the previous visibility. 
        /// </summary>
        /// <param name="graph">the graph we want to modify</param>
        /// <returns>Action that restores previous visibility</returns>
        private static Action DisplayGraphOnly(EditorWindow window, GraphView graph)
        {
            if (graph == null || window == null) return null;
            List<(VisualElement element, DisplayStyle previous)> modified = new();

            // hide immediate children
            foreach (VisualElement child in window.rootVisualElement.Children())
            {
                if (child.resolvedStyle.display != DisplayStyle.None)
                {
                    modified.Add((child, child.style.display.value));
                    child.style.display = DisplayStyle.None;
                }
            }               

            // make graph visible
            VisualElement current = graph;
            while (current != null)
            {
                current.style.display = DisplayStyle.Flex;

                if (current == window.rootVisualElement) break;
                current = current.parent;
            }

            graph.MarkDirtyRepaint();

            // function that restores the immediate children to their display mode
            return () =>
            {
                foreach ((VisualElement element, DisplayStyle previous) in modified)
                {
                    element.style.display = previous;
                }

                graph.MarkDirtyRepaint();
            };
        }

        /// <summary>
        /// Tries to obtain a visual element of a given class from a GraphView
        /// </summary>
        /// <typeparam name="T">class we look for</typeparam>
        /// <param name="graph">View Graph</param>
        /// <returns>First Visual Element of the type class</returns>
        public static T FindElementOfType<T>(GraphView graph) where T : VisualElement
        {
            if (graph == null)
                return null;

            return graph.Query<T>().First();
        }

    }
}



