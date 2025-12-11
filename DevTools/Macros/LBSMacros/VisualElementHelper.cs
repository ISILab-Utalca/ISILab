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

    }
}



