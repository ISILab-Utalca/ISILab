using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.Commons.Interfaces
{
    public interface IEasyEditorCoroutines
    {
        Dictionary<VisualElement, List<EditorCoroutine>> ActiveCoroutines { get; }
    }
    public static class EasyEditorCoroutineExtensions
    {
        public static void StartCoroutine(this IEasyEditorCoroutines owner, IEnumerator routine, VisualElement key)
        {
            if (!owner.ActiveCoroutines.ContainsKey(key))
            {
                owner.ActiveCoroutines[key] = new List<EditorCoroutine>();
            }
            owner.StopAllRoutines(key);
            owner.ActiveCoroutines[key].Add(EditorCoroutineUtility.StartCoroutine(routine, key));
        }

        public static void StopAllRoutines(this IEasyEditorCoroutines owner, VisualElement ve)
        {
            if (!owner.ActiveCoroutines.ContainsKey(ve)) return;

            foreach (var coroutine in owner.ActiveCoroutines[ve])
            {
                EditorCoroutineUtility.StopCoroutine(coroutine);
            }
            owner.ActiveCoroutines[ve].Clear();

        }

        public static void ShowImage(this IEasyEditorCoroutines owner, VisualElement ve)
        {
            owner.StartCoroutine(owner.FadeImage(ve, 1f), ve);
        }
        public static void HideImage(this IEasyEditorCoroutines owner, VisualElement ve)
        {
            owner.StartCoroutine(owner.FadeImage(ve, 0f), ve);
        }

        public static IEnumerator FadeImage(this IEasyEditorCoroutines owner, VisualElement ve, float targetAlpha)
        {
            // Apply to children
            foreach (VisualElement son in ve.Children())
            {
                owner.StartCoroutine(owner.FadeImage(son, targetAlpha), son);
            }

            // Change color
            Color color = ve.style.unityBackgroundImageTintColor.value;
            double previousTime = UnityEditor.EditorApplication.timeSinceStartup;

            while (!Mathf.Approximately(color.a, targetAlpha))
            {
                double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - previousTime);
                previousTime = currentTime;

                color.a = Mathf.MoveTowards(
                    color.a,
                    targetAlpha,
                    8 * deltaTime);

                ve.style.unityBackgroundImageTintColor = color;

                yield return null;
            }

            color.a = targetAlpha;
            ve.style.unityBackgroundImageTintColor = color;

        }


        public static IEnumerator FadeOpacity(this IEasyEditorCoroutines owner, VisualElement ve, float targetAlpha)
        {
            // Apply to children
            foreach (VisualElement son in ve.Children())
            {
                owner.StartCoroutine(owner.FadeOpacity(son, targetAlpha), son);
            }

            // Change Opacity
            float a = ve.style.opacity.value;
            double previousTime = UnityEditor.EditorApplication.timeSinceStartup;

            while (!Mathf.Approximately(a, targetAlpha))
            {
                double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - previousTime);
                previousTime = currentTime;

                a = Mathf.MoveTowards(a, targetAlpha, 4 * deltaTime);
                ve.style.opacity = new StyleFloat(a);
                yield return null;
            }

            a = targetAlpha;
            ve.style.opacity = new StyleFloat(a);
        }
    }
}
