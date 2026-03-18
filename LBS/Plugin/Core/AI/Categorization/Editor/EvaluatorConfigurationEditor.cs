using ISILab.LBS.AI.Categorization;
using ISILab.LBS.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Editor
{
    [CustomEditor(typeof(EvaluatorConfiguration))]
    public class EvaluatorConfigurationEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var window = EvaluatorConfigurationWindow.Instance;
            if (window != null)// && window.Asset == target)
                window.Close();
        }

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

    public class EvaluatorConfigurationWindow : EditorWindow
    {
        public static EvaluatorConfigurationWindow Instance { get; private set; }

        public Object Asset { get; private set; }
        private UnityEditor.Editor AssetEditor { get; set; }

        public static EvaluatorConfigurationWindow Create(EvaluatorConfiguration config)
        {
            var window = CreateWindow<EvaluatorConfigurationWindow>();
            window.titleContent = new GUIContent(config.name);
            window.Asset = config;
            window.AssetEditor = UnityEditor.Editor.CreateEditor(config); 
            window.rootVisualElement.Clear();
            window.CreateGUI();
            Instance = window;
            return window;
        }

        private void CreateGUI()
        {
            if (AssetEditor == null) return;
            rootVisualElement.Add(new InspectorElement(AssetEditor));
        }
    }
}

