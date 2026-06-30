#if UNITY_EDITOR
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;
using UnityEditor;

namespace ISILab.LBS.VisualElements
{
    [CustomEditor(typeof(QuestTriggerNode), true)]
    public class QuestTriggerEditor : Editor
    {
        private SerializedProperty _stateProp;
        private SerializedProperty _nodeTypeProp;
        private SerializedProperty _previousProp;
        private SerializedProperty _nextProp;

        protected virtual void OnEnable()
        {
            _stateProp = serializedObject.FindProperty("state");
            _nodeTypeProp = serializedObject.FindProperty("nodeType");
            _previousProp = serializedObject.FindProperty("previous");
            _nextProp = serializedObject.FindProperty("next");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Always draw the base state configurations
            EditorGUILayout.PropertyField(_stateProp);
            EditorGUILayout.PropertyField(_nodeTypeProp);

            // Get the current enum value safely
            QuestNode.NodeGraphType nodeType = (QuestNode.NodeGraphType)_nodeTypeProp.enumValueIndex;

            // Rule 1: Hide Previous List if it's a Start Node
            if (nodeType != QuestNode.NodeGraphType.Start)
            {
                EditorGUILayout.PropertyField(_previousProp, true);
            }

            // Rule 2: Hide Next Field if it's a Goal Node
            if (nodeType != QuestNode.NodeGraphType.Goal)
            {
                EditorGUILayout.PropertyField(_nextProp, true);
            }

            // Draw any extra fields belonging to child classes (like QuestTriggerNode fields) automatically
            DrawPropertiesExcluding(serializedObject, "m_Script", "state", "nodeType", "previous", "next");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif