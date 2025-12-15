using UnityEngine;
using UnityEditor;

namespace PathOS
{
    [CustomPropertyDrawer(typeof(PathOSDisplayNameAttribute))]
    public class DisplayNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = (attribute as PathOSDisplayNameAttribute).displayName;
            EditorGUI.PropertyField(position, property, label);
        }
    }
}

