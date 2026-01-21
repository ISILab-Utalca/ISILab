using ISILab.Commons.Utility;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ISILab.LBS
{
    public class LBSCustomEditorAttribute : LBSAttribute
    {
        public Type type;
        public string name;

        public LBSCustomEditorAttribute(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
    }

    public static class LBS_Editor
    {
        public static List<Tuple<Type, IEnumerable<LBSCustomEditorAttribute>>> pairsEditors;

        public static List<Tuple<Type, IEnumerable<DrawerAttribute>>> pairDrawers;

        public static Type GetEditor<T>()
        {
            return GetEditor(typeof(T));
        }

        public static Type GetEditor(Type targetType)
        {
            if (pairsEditors == null)
                pairsEditors = Reflection.GetClassesWith<LBSCustomEditorAttribute>();

            foreach (var pair in pairsEditors)
            {
                if (pair.Item2.ToList()[0].type == targetType)
                {
                    return pair.Item1;
                }
            }
            return null;

        }

        public static void LoadEditor(VisualElement container, object target)
        {
            container.Clear();

            var veType = GetEditor(target.GetType());

            if (veType == null)
            {
                return;
            }

            var ve = Activator.CreateInstance(veType, new object[] { target }) as VisualElement;
            if (ve is ClassFoldout cf)
            {
                //cf.OnCreate(veType, target);
            }

            container.Add(ve);
        }

        public static Type GetDrawer<T>()
        {
            return GetDrawer(typeof(T));
        }

        public static Type GetDrawer(Type targetType)
        {
            if (pairDrawers == null)
                pairDrawers = Reflection.GetClassesWith<DrawerAttribute>();

            foreach (var pair in pairDrawers)
            {
                var t = pair.Item2.ToList()[0].type;
                if (t == targetType)
                {
                    return pair.Item1;
                }
            }
            return null;
        }

    }
}