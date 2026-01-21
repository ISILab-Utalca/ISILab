using ISILab.LBS.AI.Categorization;
using ISILab.LBS.VisualElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Editor
{
    [CustomEditor(typeof(EvaluatorConfiguration))]
    public class EvaluatorConfigurationEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new EvaluatorConfigurationVE(target);

            return root;
        }
        private void OnDestroy()
        {
            EditorUtility.SetDirty(target);
        }

        private void OnDisable()
        {
            EditorUtility.SetDirty(target);
        }

    }
}

