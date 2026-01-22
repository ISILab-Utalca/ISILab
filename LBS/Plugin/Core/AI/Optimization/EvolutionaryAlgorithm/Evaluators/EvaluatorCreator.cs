using ISILab.LBS.Plugin.Core.Settings;
using System.IO;
using UnityEditor;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public static class EvaluatorCreator
    {
        private const string TEMPLATE_GUID = "c3670a7ec89e4ec42979f6ec60df94be";
        private const string VISUAL_ELEMENT_GUID = "fb63a01c9cdae9041a61fcd0c9b20e59";

        [MenuItem("Assets/Create/Scripting/ISI Lab/Configurable Evaluator")]
        public static void CreateConfigurableEvaluator()
        {
            string evaluatorsFolder = LBSSettings.Instance.paths.baseFolderPath + "/LBS/Plugin/Core/AI/Optimization/EvolutionaryAlgorithm/Evaluators";

            string path = EditorUtility.SaveFilePanelInProject(
                "Create new evaluator class",
                "CustomEvaluator",
                "cs",
                "Choose a location to save the new evaluator.",
                evaluatorsFolder
            );

            if (string.IsNullOrEmpty(path))
                return;

            string template = File.ReadAllText(AssetDatabase.GUIDToAssetPath(TEMPLATE_GUID));
            string className = Path.GetFileNameWithoutExtension(path);

            template = template.Replace("#SCRIPTNAME#", className);
            File.WriteAllText(path, template);

            string VEPath = evaluatorsFolder + "/Editor/" + className + "VE.cs";

            string VE = File.ReadAllText(AssetDatabase.GUIDToAssetPath(VISUAL_ELEMENT_GUID))
                .Replace("#TARGETNAME#", className);
            File.WriteAllText(VEPath, VE);

            AssetDatabase.Refresh();
        }
    }
}
