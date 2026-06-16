using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;


namespace ISILab.LBS.Macros
{ 
    public class LBSLayerHelper
    {

        #region Object Retrievals
        /// <summary>
        /// Retrieves an object of type T from the Behaviours, Assistants, or Modules list within the given layerChild's OwnerLayer.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="layerChild">An object with an OwnerLayer property that contains Behaviours, Assistants, and Modules</param>
        /// <returns>The first matching object of type T found, or null if not found.</returns>
        public static T GetObjectFromLayerChild<T>(object layerChild) where T : class
        {
            // Use reflection to get the OwnerLayer property
            PropertyInfo ownerLayerProp = layerChild.GetType().GetProperty("OwnerLayer");
            if (ownerLayerProp == null) return null;

            var ownerLayer = ownerLayerProp.GetValue(layerChild);
            if (ownerLayer == null) return null;

            // Look for the T in Behaviours, Assistants, and Modules
            foreach (var listName in new[] { "Behaviours", "Assistants", "Modules" })
            {
                PropertyInfo listProp = ownerLayer.GetType().GetProperty(listName);
                if (listProp == null) continue;

                var list = listProp.GetValue(ownerLayer) as IEnumerable<object>;

                var match = list?.FirstOrDefault(b => b is T);
                if (match != null) return match as T;
            }

            return null;
        }

        /// <summary>
        /// Retrieves an object of type T from the Behaviours, Assistants, or Modules list within the provided layer.
        /// </summary>
        /// <typeparam name="T">The type of object to find.</typeparam>
        /// <param name="layer">An object that contains Behaviours, Assistants, and Modules properties.</param>
        /// <returns>The first matching object of type T found, or null if not found.</returns>
        public static T GetObjectFromLayer<T>(object layer) where T : class
        {
            if (layer == null) return null;

            foreach (var listName in new[] { "Behaviours", "Assistants", "Modules" })
            {
                PropertyInfo listProp = layer.GetType().GetProperty(listName);
                if (listProp == null) continue;

                var list = listProp.GetValue(layer) as IEnumerable<object>;
                var match = list?.FirstOrDefault(b => b is T);
                if (match != null) return match as T;
            }

            return null;
        }
        
        public static Tuple<LBSLayer, TileBundleGroup> GetBundleTileByPosition(Vector2Int TilePosition, List<LBSLayer> Layers)
        {
            foreach (LBSLayer layer in Layers)
            {
                // Only check layers with a PopulationBehaviour
                PopulationBehaviour population = layer.GetBehaviour<PopulationBehaviour>();
                if (population == null)
                    continue;

                TileBundleGroup tileGroup = population.GetTileGroup(TilePosition);
                if (tileGroup != null)
                {
                    return Tuple.Create(layer, tileGroup);
                }
            }

            return null; // nothing found
        }

        public static Tuple<LBSLayer, TileBundleGroup> GetBundleTileByMouse(Vector2Int mousePosition, List<LBSLayer> Layers)
        {
            foreach (LBSLayer layer in Layers)
            {
                // Only check layers with a PopulationBehaviour
                PopulationBehaviour population = layer.GetBehaviour<PopulationBehaviour>();
                if (population == null)
                    continue;

                TileBundleGroup tileGroup = population.GetTileGroup(population.OwnerLayer.ToFixedPosition(mousePosition));
                if (tileGroup != null)
                {
                    return Tuple.Create(layer, tileGroup);
                }
            }

            return null; // nothing found
        }

        #endregion


        #region Debug
        /// <summary>
        /// Prints an object's anme and its memory adress
        /// </summary>
        /// <param name="obj">target object</param>
        public static void PrintReference(object obj)
        {
            if (obj == null)
            {
                Debug.Log($"PrintReference param object is null");
                return;
            }

            string name = obj.GetType().Name;

            string message =
                $"{name} | " +
                $"Type: {obj.GetType().Name} | " +
                $"RefHash: {RuntimeHelpers.GetHashCode(obj)}";

            if (obj is Object unityObject)
            {
                message +=
                    $" | UnityInstanceID: {unityObject.GetInstanceID()}" +
                    $" | UnityName: {unityObject.name}";
            }

            Debug.Log(message);
        }
        #endregion
    }
}