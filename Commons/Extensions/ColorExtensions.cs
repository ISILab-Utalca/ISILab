using System.Collections.Generic;
using UnityEngine;
using ISILab.Commons.Utility;

namespace ISILab.Commons.Extensions
{
    /// <summary>
    /// Class extending Unity Color structure with random generation methods.
    /// </summary>
    public static class ColorExtensions
    {
        private const float colorDifferenceValue = 0.25f;
        private static List<Color> recentColors = new List<Color>();
        
        // 😫 Should either improve the value generation or
        // create a dictionary of colors and apply small modifications on it
        //
        // （づ￣3￣）づ╭🖌️～
        /// <summary>
        /// Generates a random HSV color, with high saturation and value, ensuring not to repeat colors from last invocations.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color RandomColorHSV(this Color color, bool async = false)
        {

            do
            {
                float hue = SafeRandom.Range(0f, 1f, async);
                float saturation = SafeRandom.Range(0.75f, 1f, async);
                float value = SafeRandom.Range(0.75f, 1f, async);
                color = Color.HSVToRGB(hue, saturation, value);
            }while(!ColorDifferentEnough(color));
            
            if(recentColors.Count>5) recentColors.RemoveAt(0);
            recentColors.Add(color);
            
            return color;
        }

        private static bool ColorDifferentEnough(Color newColor)
        {
            if (recentColors.Count == 0) return true;
            
            foreach (var savedColor in recentColors)
            {
                var redDiff = Mathf.Abs(savedColor.r - newColor.r);
                var greenDiff = Mathf.Abs(savedColor.g - newColor.g);
                var blueDiff = Mathf.Abs(savedColor.b - newColor.b);
                if (redDiff < colorDifferenceValue &&
                    greenDiff <  colorDifferenceValue &&
                    blueDiff < colorDifferenceValue)
                {
                   
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a random RGB color.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color RandomColorRGB(this Color color)
        {
            color = new Color(
                Random.Range(0f, 255f) / 255f,
                Random.Range(0f, 255f) / 255f,
                Random.Range(0f, 255f) / 255f);
            return color;
        }

        /// <summary>
        /// Generates a random color in gray scale.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color RandomGrayScale(this Color color)
        {
 
            var gray = Random.Range(0f, 255f) / 255f;
            color = new Color(gray, gray, gray);
            return color;
        }

        public static Color Inverse(this Color color)
        {
            return new Color(1 - color.r, 1 - color.g, 1 - color.b);
        }
    }
}