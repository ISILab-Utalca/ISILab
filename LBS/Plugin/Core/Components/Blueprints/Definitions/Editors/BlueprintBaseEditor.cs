using UnityEditor;

namespace ISILab.LBS.Components
{

#if UNITY_EDITOR
    [CustomEditor(typeof(Blueprint), true)]
    public class BlueprintBaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("blueprintName"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("blueprintType"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

}