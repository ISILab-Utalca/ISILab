using System.IO;
using UnityEditor;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public static class DCEvaluatorCreator
    {
        private const string TEMPLATE_GUID = "c3670a7ec89e4ec42979f6ec60df94be";

        [MenuItem("Assets/Create/Scripting/ISI Lab/Dungeon Crawler Evaluator")]
        public static void CreateDCEvaluator()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create new evaluator class",
                "DCCustomEvaluator",
                "cs",
                "Choose a location to save the new evaluator.",
                "Assets//LBS/Artificial Intelligence/Optimization/EvolutionaryAlgorithm/Evaluators/Dungeon Crawler"
            );

            if (string.IsNullOrEmpty(path))
                return;

            string template = File.ReadAllText(AssetDatabase.GUIDToAssetPath(TEMPLATE_GUID));
            string className = Path.GetFileNameWithoutExtension(path);

            template = template.Replace("#SCRIPTNAME#", className);

            File.WriteAllText(path, template);
            AssetDatabase.Refresh();
        }
    }
}
