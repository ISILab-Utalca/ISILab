using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEditor;
using UnityEngine;

namespace LBS.Components
{
    /// <summary>
    /// A static class that finds bundles with a certain characteristic within their children or themselves.
    /// Useful for LBSButtonListFilter component.
    /// </summary>
    public static class BundleQueryUtility
    {
        public static List<Bundle> FindBundlesWithCharacteristic<TCharacteristic>(bool includeChildren = true)
            where TCharacteristic : LBSCharacteristic
        {
            return FindBundlesWithCharacteristic(typeof(TCharacteristic), includeChildren);
        }

        /// <summary>
        /// Returns the bundles that contain the specified characteristic type.
        /// </summary>
        public static List<Bundle> FindBundlesWithCharacteristic(Type characteristicType, bool includeChildren = true)
        {
            if (characteristicType == null)
            {
                LBSMainWindow.MessageNotify(new ArgumentNullException(nameof(characteristicType)).ToString(), LogType.Error);
                return null;
            }

            if (!typeof(LBSCharacteristic).IsAssignableFrom(characteristicType))
            {
                LBSMainWindow.MessageNotify($"El tipo {characteristicType.Name} no deriva de {nameof(LBSCharacteristic)}.", LogType.Error);
            }

            var result = new List<Bundle>();
            var bundleGuids = AssetDatabase.FindAssets("t:Bundle");

            foreach (var guid in bundleGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var bundle = AssetDatabase.LoadAssetAtPath<Bundle>(path);
                if (bundle == null) continue;

                if (IsValidExteriorBundle(bundle, characteristicType))
                {
                    result.Add(bundle);
                }
            }

            return result.OrderBy(b => b.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool IsValidExteriorBundle(Bundle bundle, Type charType)
        {
            if (bundle == null) return false;

            var getChildrenCharacteristicsMethod = typeof(Bundle).GetMethod("GetChildrenCharacteristics");
            var getCharacteristicsMethod = typeof(Bundle).GetMethod("GetCharacteristics");

            if (getChildrenCharacteristicsMethod != null)
            {
                var genericMethod = getChildrenCharacteristicsMethod.MakeGenericMethod(charType);
                if (genericMethod.Invoke(bundle, null) is IList groups && groups.Count > 0) return true;
            }

            if (getCharacteristicsMethod != null)
            {
                var genericMethod = getCharacteristicsMethod.MakeGenericMethod(charType);
                if (genericMethod.Invoke(bundle, null) is IList localGroups && localGroups.Count > 0) return true;
            }

            return false;
        }
    }
}

