using UnityEditor;
using UnityEngine;

namespace ISILab.Commons.Attributes
{
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public readonly bool includeChildren;
        public ReadOnlyAttribute(bool includeChildren = true) => this.includeChildren = includeChildren;
    }

    public class ReadOnlyIncludeChildrenAttribute : ReadOnlyAttribute
    {
        public ReadOnlyIncludeChildrenAttribute() : base(true) { }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 1. Force the entire UI group into a disabled state
            // This is more reliable for children than GUI.enabled = false
            EditorGUI.BeginDisabledGroup(true);

            // 2. Draw the property. 
            // 'true' ensures the foldout and all nested children are drawn.
            EditorGUI.PropertyField(position, property, label, true);

            // 3. Close the disabled group so the rest of the Inspector isn't locked
            EditorGUI.EndDisabledGroup();
        }
    }
}