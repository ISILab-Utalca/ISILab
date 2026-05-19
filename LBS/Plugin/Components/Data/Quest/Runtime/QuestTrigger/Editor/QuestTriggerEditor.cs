#if UNITY_EDITOR
using ISILab.LBS.Components;
using UnityEditor;

namespace ISILab.LBS.VisualElements
{
    [CustomEditor(typeof(QuestTrigger), true)]
    public class QuestTriggerEditor : Editor
    {
        private SerializedProperty _stateProp;
        private SerializedProperty _nodeTypeProp;
        private SerializedProperty _allPreviousProp;
        private SerializedProperty _nextProp;

        protected virtual void OnEnable()
        {
            _stateProp = serializedObject.FindProperty("state");
            _nodeTypeProp = serializedObject.FindProperty("nodeType");
            _allPreviousProp = serializedObject.FindProperty("allPrevious");
            _nextProp = serializedObject.FindProperty("next");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Always draw the base state configurations
            EditorGUILayout.PropertyField(_stateProp);
            EditorGUILayout.PropertyField(_nodeTypeProp);

            // Get the current enum value safely
            QuestNode.ENodeType nodeType = (QuestNode.ENodeType)_nodeTypeProp.enumValueIndex;

            // Rule 1: Hide Previous List if it's a Start Node
            if (nodeType != QuestNode.ENodeType.Start)
            {
                EditorGUILayout.PropertyField(_allPreviousProp, true);
            }

            // Rule 2: Hide Next Field if it's a Goal Node
            if (nodeType != QuestNode.ENodeType.Goal)
            {
                EditorGUILayout.PropertyField(_nextProp, true);
            }

            // Draw any extra fields belonging to child classes (like QuestTriggerNode fields) automatically
            DrawPropertiesExcluding(serializedObject, "m_Script", "state", "nodeType", "allPrevious", "next");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif