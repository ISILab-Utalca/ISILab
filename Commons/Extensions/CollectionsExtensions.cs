using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace ISILab.Commons.Extensions
{
    /// <summary>
    /// Class containing extension methods for collection types, such as List and Array.
    /// </summary>
    public static class CollectionsExtensions
    {
        #region LIST
        /// <summary>
        /// Creates a deep copy of a list.
        /// </summary>
        /// <typeparam name="T">A class type that implements ICloneable.</typeparam>
        /// <param name="list"></param>
        /// <returns>A deep copy of the list.</returns>
        public static List<T> Clone<T>(this List<T> list) where T : class, ICloneable
        {
            var clone = new List<T>();

            foreach (var item in list)
            {
                var c = item.Clone() as T;
                clone.Add(c);
                //if (item is ICloneable)
                //{
                //    var c = (item as ICloneable).Clone() as T;
                //    clone.Add(c);
                //}
                //else
                //{
                //    Debug.LogWarning("Item: '" + item + "' in '" + list + "' cannot be cloned.");
                //    clone.Add(item);
                //}
            }
            return clone;
        }

        /// <summary>
        /// Indicates whether a list contains only the specified values ​​or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="values">The values to consult.</param>
        /// <returns><b>True</b> if all elements in the list are equal to one of the passed values. <b>False</b> otherwise.</returns>
        public static bool ContainsOnly<T>(this List<T> list, params T[] values)
        {
            return !list.Except(values).Any();
        }

        /// <summary>
        /// Performs a weighted random selection of an element from a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate">Function to obtain an element's weight.</param>
        /// <returns>The randomly selected element if the list is not empty. <br/>Default otherwise.</returns>
        public static T RandomRullete<T>(this List<T> list, Func<T, float> predicate)
        {
            if (list.Count <= 0)
                return default(T);

            if(list.Count == 1) 
                return list[0];

            var pairs = new List<Tuple<T, float>>();
            for (int i = 0; i < list.Count(); i++)
            {
                var value = predicate(list[i]);
                pairs.Add(new Tuple<T, float>(list[i], value));
            }

            var total = pairs.Sum(p => p.Item2);
            var rand = (float)(new Random().NextDouble() * total);

            var cur = 0f;
            for (int i = 0; i < pairs.Count; i++)
            {
                cur += pairs[i].Item2;
                if (rand <= cur)
                {
                    return pairs[i].Item1;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Indicates whether an index is within the range of a list or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index">Index to check.</param>
        /// <returns><b>True</b> if index is in range. <b>False</b> otherwise.</returns>
        public static bool ContainsIndex<T>(this List<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Obtains a random element from a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The randomly selected element if the list is not empty. <br/>Default otherwise.</returns>
        public static T Random<T>(this List<T> list)
        {
            if (list.Count <= 0)
            {
                //Debug.Log("[ISI Lab]: Error to try get a random element in '" + list + "' because is empty.");
                return default(T);
            }

            return list[new Random().Next(0, list.Count - 1)];
        }

        public static bool IsSameRotated<T>(this List<T> list, List<T> rotated, out int rot)
        {
            rot = -1;

            if (list.Count != rotated.Count)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (rotated.SequenceEqual(list.Rotate(i)))
                {
                    rot = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shifts the elements of a list in a new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="count">Times to make a shift.</param>
        /// <returns>A new list with the shifted elements.</returns>
        public static List<T> Rotate<T>(this List<T> list, int count)
        {
            if (count == 0)
                return new List<T>(list);

            var c = ((count % list.Count) + list.Count) % list.Count;
            int rotationIndex = list.Count - c;
            List<T> rotatedList = new List<T>();

            for (int i = rotationIndex; i < list.Count; i++)
                rotatedList.Add(list[i]);

            for (int i = 0; i < rotationIndex; i++)
                rotatedList.Add(list[i]);

            return rotatedList;
        }

        /// <summary>
        /// Shifts only once the elements of a list in a new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>A new list with the shifted elements.</returns>
        public static List<T> Rotate<T>(this List<T> list)
        {
            var toR = new List<T>(list);

            if (toR.Count <= 0)
                return toR;

            var temp = toR.Last();
            toR.Remove(temp);
            toR.Insert(0, temp);

            return toR;
        }

        /// <summary>
        /// Removes all null elements from a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The same list without null elements.</returns>
        public static List<T> RemoveEmpties<T>(this List<T> list)
        {
            list = list.Where(b => b != null).ToList();
            return list;
        }

        /// <summary>
        /// Removes all duplicate elements from a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The same list with unique elements.</returns>
        public static List<T> RemoveDuplicates<T>(this List<T> list)
        {
            var toR = new List<T>();
            foreach (var item in list)
            {
                if (!toR.Contains(item))
                    toR.Add(item);
            }
            return toR;
        }

        /// <summary>
        /// Shuffles elements in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            var rnd = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(0, n + 1); 
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Creates a string that concatenates all elements in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="separator">String to separate elements.</param>
        /// <returns>A string containing all elements from the list, spaced by a separator.</returns>
        public static string ElementsToString<T>(this List<T> list, string separator = ";")
        {
            return string.Join(separator, list.ToArray());
        }

        /// <summary>
        /// Creates a string that concatenates all sorted elements in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="sorter">A custom element sorter.</param>
        /// <param name="separator">String to separate elements.</param>
        /// <returns>A string containing all sorted elements from the list, spaced by a separator.</returns>
        public static string SortedElementsToString<T>(this List<T> list, Comparison<T> sorter, string separator = ";")
        {
            if (list.Count == 0) return list.ToString();
            if (list.Count == 1) return list[0].ToString();

            var sortedList = new List<T>(list);
            sortedList.Sort(sorter);
            return sortedList.ElementsToString(separator);
        }

        #endregion

        #region Array
        /// <summary>
        /// Indicates whether an index is within the range of an array or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="index">Index to check.</param>
        /// <returns><b>True</b> if index is in range. <b>False</b> otherwise.</returns>
        public static bool ContainsIndex<T>(this T[] array, int index)
        {
            return index >= 0 && index < array.Length;
        }

        /// <summary>
        /// Obtains a random element from an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns>The randomly selected element if the array is not empty. <br/>Default otherwise.</returns>
        public static T GetRandom<T>(this T[] array)
        {
            if (array.Length <= 0)
            {
                Debug.Log("[ISI Lab]: Error to try get a random element in '" + array + "' because is empty.");
                return default(T);
            }

            return array[UnityEngine.Random.Range(0, array.Length - 1)];
        }

        #endregion

        #region DICTIONARY

        //public static Dictionary<K, V> Clone<K, V>(this Dictionary<K, V> dict) 
        //    where K : notnull 
        //    where V : class
        //{
        //    var clone = new Dictionary<K, V>();
        //
        //    foreach(var pair in dict)
        //    {
        //        if(pair.Value is ICloneable)
        //        {
        //            var v = (pair.Value as ICloneable).Clone() as V;
        //            clone.Add(pair.Key, v);
        //        }
        //        else
        //        {
        //            Debug.LogWarning("Value: '" + pair.Value + "' in '" + dict + "' cannot be cloned.");
        //            clone.Add(pair.Key, pair.Value);
        //        }
        //    }
        //    return clone;
        //}

        #endregion
    }
}