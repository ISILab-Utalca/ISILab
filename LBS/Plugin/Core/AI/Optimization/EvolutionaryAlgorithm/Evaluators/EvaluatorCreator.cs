using ISILab.Commons.Extensions;
using ISILab.LBS.Plugin.Core.Settings;
using System.IO;
using UnityEditor;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    public static class EvaluatorCreator
    {
        private const string TEMPLATE_GUID = "c3670a7ec89e4ec42979f6ec60df94be";
        private const string VISUAL_ELEMENT_GUID = "fb63a01c9cdae9041a61fcd0c9b20e59";

        private const string BASE_TEMPLATE                      = "406a51c1e818e754b9f6052ea90d2e30";

        private const string CONTEXT_NAMESPACES                 = "cfb941310a5243a439b079ca654f0ca9";
        private const string CONTEXT_PROPERTIES                 = "decb9c51d80b07d469ce0a53e7aba1a3";
        private const string CONTEXT_TOOLTIP                    = "4c869250504663d4e8cea42e34807ba3";
        private const string CONTEXT_EVALUATION_INVOKE          = "52eaea3ac981bf443bb5bb446fe6918d";
        private const string CONTEXT_EVALUATION_IMPLEMENTATION  = "9a353f7f8804410468481a797379d177";
        private const string CONTEXT_INITIALIZATION             = "fe933b72bf108c7458ab832a8f20f7b7";
        private const string CONTEXT_CLONE                      = "031ce8d69170dc84c85ef06170623761";

        private const string CONFIGURATION_NAMESPACES           = "9eed48cb8a4c3044192f1c91c2b3abe6";
        private const string CONFIGURATION_STATIC               = "9e182e8a80364da40b12c2a23d8dcd4f";
        private const string CONFIGURATION_INITIALIZATION       = "3020bae79b0ff3c44b151cc0b22edaf9";
        private const string CONFIGURATION_IMPLEMENTATION       = "3dfcbfe68be3b604db1135c74900fdec";

        private const string VE_CONFIGURATION_NAMESPACES        = "ff7cf6816f4fc0644a0591985b414fa5";
        private const string VE_CONFIGURATION_BUTTON_A          = "d2924a739a65cc843ab252dcb722bce8";
        private const string VE_CONFIGURATION_BUTTON_B          = "46f9cb89963ffd84985985e0c66bccba";

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

            EditorApplication.delayCall += () =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                string template = GetText(BASE_TEMPLATE);
                string className = Path.GetFileNameWithoutExtension(path);

                bool context = true,
                    configurable = true;

                string interfaces = "";
                if (context) interfaces += ", IContextualEvaluator";
                if (configurable) interfaces += ", IConfigurableEvaluator";

                template = template
                    .ReplaceOrErase ("#CONTEXT_NAMESPACES#"                 , GetText(CONTEXT_NAMESPACES)               , context)
                    .ReplaceOrErase ("#CONFIGURATION_NAMESPACES#"           , GetText(CONFIGURATION_NAMESPACES)         , configurable)
                    .Replace        ("#SCRIPTNAME#"                         , className)
                    .Replace        ("#SPACED_SCRIPTNAME#"                  , className.AddSpacesToSentence() + " Evaluator")
                    .Replace        ("#INTERFACES#"                         , interfaces)
                    .ReplaceOrErase ("#CONTEXT_PROPERTIES#"                 , GetText(CONTEXT_PROPERTIES)               , context)
                    .ReplaceOrErase ("#CONTEXT_TOOLTIP#"                    , GetText(CONTEXT_TOOLTIP)                  , context)
                    .ReplaceOrErase ("#CONTEXT_EVALUATION_INVOKE#"          , GetText(CONTEXT_EVALUATION_INVOKE)        , context)
                    .ReplaceOrErase ("#CONTEXT_EVALUATION_IMPLEMENTATION#"  , GetText(CONTEXT_EVALUATION_IMPLEMENTATION), context)
                    .ReplaceOrErase ("#CONTEXT_INITIALIZATION#"             , GetText(CONTEXT_INITIALIZATION)           , context)
                    .ReplaceOrErase ("#CONTEXT_CLONE#"                      , GetText(CONTEXT_CLONE)                    , context)
                    .ReplaceOrErase ("#CONFIGURATION_STATIC#"               , GetText(CONFIGURATION_STATIC)             , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_INITIALIZATION#"       , GetText(CONFIGURATION_INITIALIZATION)     , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_IMPLEMENTATION#"       , GetText(CONFIGURATION_IMPLEMENTATION)     , configurable);
                File.WriteAllText(path, template);

                string VEPath = evaluatorsFolder + "/Editor/" + className + "VE.cs";

                string VE = GetText(VISUAL_ELEMENT_GUID)
                    .ReplaceOrErase ("#CONFIGURATION_NAMESPACES#"   , GetText(VE_CONFIGURATION_NAMESPACES)  , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_BUTTON_A#"     , GetText(VE_CONFIGURATION_BUTTON_A)    , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_BUTTON_B#"     , GetText(VE_CONFIGURATION_BUTTON_B)    , configurable)
                    .Replace        ("#TARGETNAME#"                 , className);
                File.WriteAllText(VEPath, VE);

                AssetDatabase.Refresh();
            };
        }

        private static string GetText(string fileGUID)
        {
            string nullString = string.Empty;
            if (string.IsNullOrWhiteSpace(fileGUID)) return nullString;
            string path = AssetDatabase.GUIDToAssetPath(fileGUID);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return nullString;
            return File.ReadAllText(path);
        }
    }
}
