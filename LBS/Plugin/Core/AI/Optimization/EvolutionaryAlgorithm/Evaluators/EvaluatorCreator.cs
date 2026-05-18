using ISILab.Commons.Extensions;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
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
        
        private const string PERMA_PRESEARCH                    = "8907b6a77d8857d4a95a084fd672c369";
        private const string PERMA_SEARCH                       = "af0f63521f82f914680739ea6a8933f5";

        private const string DISTANCE_PROPERTIES                = "b7b22a7bedd067842a622a3390d0d648";
        private const string DISTANCE_INITIALIZATION            = "348db66a4f6642346b555447c1f9d6da";
        private const string DISTANCE_MEASURING                 = "674b731557920f94385f67dddb69c79b";
        private const string DISTANCE_POST_MEASURING            = "8f9d53df450328646885c250a7290da8";
        private const string PATHFIND_INITIALIZATION            = "ebd85878ef9988041a215f8ac55463a5";
        private const string DISTANCE_CLONATION                 = "2c8bb7625a67a9e44a2af255e5699f40";

        private const string CONFIGURATION_NAMESPACES           = "9eed48cb8a4c3044192f1c91c2b3abe6";
        private const string CONFIGURATION_STATIC               = "9e182e8a80364da40b12c2a23d8dcd4f";
        private const string CONFIGURATION_INITIALIZATION       = "3020bae79b0ff3c44b151cc0b22edaf9";
        private const string CONFIGURATION_IMPLEMENTATION       = "3dfcbfe68be3b604db1135c74900fdec";

        private const string VE_CONFIGURATION_NAMESPACES        = "ff7cf6816f4fc0644a0591985b414fa5";
        private const string VE_CONFIGURATION_BUTTON_A          = "d2924a739a65cc843ab252dcb722bce8";
        private const string VE_CONFIGURATION_BUTTON_B          = "46f9cb89963ffd84985985e0c66bccba";

        //[MenuItem("Assets/Create/Scripting/ISI Lab/Configurable Evaluator")]
        public static void CreateConfigurableEvaluator() => CreateConfigurableEvaluator("", false, false, false);
        public static void CreateConfigurableEvaluator(string name, bool configurable, bool context, bool distance)
        {
            string evaluatorsFolder = LBSSettings.Instance.paths.evaluatorsPath;
            string evaluatorsVEsFolder = LBSSettings.Instance.paths.evaluatorsVEPath;

            string path = evaluatorsFolder + Path.DirectorySeparatorChar + name + ".cs";
            //    EditorUtility.SaveFilePanelInProject(
            //    "Create new evaluator class",
            //    "CustomEvaluator",
            //    "cs",
            //    "Choose a location to save the new evaluator.",
            //    evaluatorsFolder
            //);
            
            EditorApplication.delayCall += () =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                string template = GetText(BASE_TEMPLATE);
                string className = Path.GetFileNameWithoutExtension(path);

                if(!context) distance = false;

                string fieldsDeclaration = "", fieldsInitialization = "", configurationLoad = "", configurationPreCreation = "", configurationCreation = "", fieldsClonation = "", tagPreSearch = "", tagSearch = "";
                string permanentDeclaration = "", permanentPreSearch = "", permanentSearch = "", permanentPostSearch = "", permanentInitialization = "", permanentClonation = "";
                typeCount.Clear();
                List<Tuple<Type, bool>> fields = new() {
                    //new(typeof(Characteristics.LBSCharacteristic), false),
                    //new(typeof(Characteristics.LBSCharacteristic), true),
                    //new(typeof(Characteristics.LBSCharacteristic), true),
                    //new(typeof(Characteristics.LBSCharacteristic), false),
                    //new(typeof(int), false),
                    //new(typeof(float), false)
                };
                if (distance) fields.Insert(0, new(typeof(PathfindingAlgorithm), false));
                string firstTagChar = null;
                string permaCondition = "";
                foreach(Tuple<Type, bool> type in fields)
                {
                    GetDummyField(type, "", out string declaration, out string initialization, out string load, out string preCreation, out string creation, out string clonation, out string _tagPreSearch, out string _tagSearch, 
                        out string permaDeclaration, out string permaPreSearch, out string permaSearch, out string permaPostSearch, out string permaInitialization, out string permaClonation,
                        out string fieldFinalName, name);

                    if(type.Item1.Equals(typeof(Characteristics.LBSCharacteristic)))
                    {
                        if(string.IsNullOrEmpty(firstTagChar))
                            firstTagChar = fieldFinalName + "Ind";

                        if (permaCondition.Length > 0)
                            permaCondition += " || ";
                        permaCondition += "perma" + fieldFinalName.UpperFirst() + " is null";
                    }

                    //if (string.IsNullOrEmpty(firstTagChar) && type.Item1.Equals(typeof(Characteristics.LBSCharacteristic)))
                    //    firstTagChar = fieldFinalName + "Ind";

                    //if(type.Item1.Equals)
                    //if (permaCondition.Length > 0)
                    //    permaCondition += " || ";
                    //permaCondition += "perma" + fieldFinalName.UpperFirst() + " is null";
                    
                    fieldsDeclaration           += "\n\t\t"         + declaration;
                    fieldsInitialization        += "\n\t\t\t"       + initialization;
                    configurationLoad           += "\n\t\t\t"       + load;
                    if(preCreation.Length > 0)
                        configurationPreCreation += "\n\t\t\t" + preCreation;
                    configurationCreation       += "\n\t\t\t\t"     + creation;
                    fieldsClonation             += "\n\t\t\t"       + clonation;
                    tagPreSearch                += "\n\t\t\t"       + _tagPreSearch;
                    tagSearch                   += "\n\t\t\t\t\t"   + _tagSearch;

                    permanentDeclaration        += "\n\t\t"         + permaDeclaration;
                    permanentPreSearch          += "\n\t\t\t"       + permaPreSearch;
                    permanentSearch             += "\n\t\t\t\t\t"     + permaSearch;
                    permanentPostSearch         += "\n\t\t\t"       + permaPostSearch;
                    permanentInitialization     += "\n\t\t\t"       + permaInitialization;
                    permanentClonation          += "\n\t\t\t"       + permaClonation;
                }
                fieldsClonation += "\n";
                permanentPreSearch = $"\n\t\t\tbool checkPermaIndices = " +
                $"{(string.IsNullOrWhiteSpace(permanentPreSearch) ? "false; //#PRESEARCH_CONDITION#" : $"({permaCondition}) && bundleTM is not null; //#PRESEARCH_CONDITION#{permanentPreSearch}")}";

                //Debug.Log(fieldsDeclaration);
                //Debug.Log(fieldsInitialization);
                //Debug.Log(configurationLoad);
                //Debug.Log(configurationCreation);
                //Debug.Log(fieldsClonation);

                string interfaces = "";
                if (context) interfaces += ", IContextualEvaluator";
                if (configurable) interfaces += ", IConfigurableEvaluator";
                if (distance) interfaces += ", IDistanceEvaluator";

                template = template
                    .ReplaceOrErase ("#CONTEXT_NAMESPACES#"                 , GetText(CONTEXT_NAMESPACES)                   , context)
                    .ReplaceOrErase ("#CONFIGURATION_NAMESPACES#"           , GetText(CONFIGURATION_NAMESPACES)             , configurable)
                    .Replace        ("#SCRIPTNAME#"                         , className)
                    .Replace        ("#SPACED_SCRIPTNAME#"                  , className.AddSpacesToSentence() + " Evaluator")
                    .Replace        ("#INTERFACES#"                         , interfaces)
                    .ReplaceOrErase ("#CONTEXT_PROPERTIES#"                 , GetText(CONTEXT_PROPERTIES)                   , context)
                    .ReplaceOrErase ("#CONTEXT_TOOLTIP#"                    , GetText(CONTEXT_TOOLTIP)                      , context)
                    .ReplaceOrErase ("#CONTEXT_EVALUATION_INVOKE#"          , GetText(CONTEXT_EVALUATION_INVOKE)            , context)
                    .ReplaceOrErase ("#CONTEXT_EVALUATION_IMPLEMENTATION#"  , GetText(CONTEXT_EVALUATION_IMPLEMENTATION)    , context)
                    .ReplaceOrErase ("#CONTEXT_INITIALIZATION#"             , GetText(CONTEXT_INITIALIZATION)               , context)
                    .ReplaceOrErase ("#CONTEXT_CLONE#"                      , GetText(CONTEXT_CLONE)                        , context)
                    .ReplaceOrErase ("#PERMA_DECLARATION#"                  , permanentDeclaration + "\n//#PERMA_DECLARATION#"                          , context)
                    .ReplaceOrErase ("#PERMA_PRESEARCH#"                    , GetText(PERMA_PRESEARCH) + permanentPreSearch + "\n//#PERMA_PRESEARCH#"   , context)
                    .ReplaceOrErase ("#PERMA_SEARCH#"                       , GetText(PERMA_SEARCH) + permanentSearch + "\n//#PERMA_SEARCH#"            , context)
                    .ReplaceOrErase ("#PERMA_POSTSEARCH#"                   , permanentPostSearch + "\n//#PERMA_POSTSEARCH#"                            , context)
                    .ReplaceOrErase ("#PERMA_INITIALIZATION#"               , permanentInitialization + "\n//#PERMA_INITIALIZATION#"                    , context)
                    .ReplaceOrErase ("#PERMA_CLONATION#"                    , permanentClonation + "\n//#PERMA_CLONATION#"                              , context)
                    .ReplaceOrErase ("#DISTANCE_PROPERTIES#"                , GetText(DISTANCE_PROPERTIES)                  , distance)
                    .ReplaceOrErase ("#DISTANCE_INITIALIZATION#"            , GetText(DISTANCE_INITIALIZATION)              , distance)
                    .ReplaceOrErase ("#DISTANCE_MEASURING#"                 , GetText(DISTANCE_MEASURING)                   , distance)
                    .ReplaceOrErase ("#DISTANCE_POST_MEASURING#"            , GetText(DISTANCE_POST_MEASURING)              , distance)
                    .ReplaceOrErase ("#PATHFIND_INITIALIZATION#"            , GetText(PATHFIND_INITIALIZATION)              , distance)
                    .ReplaceOrErase ("#DISTANCE_CLONATION#"                 , GetText(DISTANCE_CLONATION)                   , distance)
                    .Replace        ("#FIRST_TAGCHAR#"                      , firstTagChar ?? "new()")
                    .ReplaceOrErase ("#CONFIGURATION_STATIC#"               , GetText(CONFIGURATION_STATIC)                 , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_INITIALIZATION#"       , GetText(CONFIGURATION_INITIALIZATION)         , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_IMPLEMENTATION#"       , GetText(CONFIGURATION_IMPLEMENTATION)         , configurable)
                    .Replace        ("#FIELDS_DECLARATION#"                 , fieldsDeclaration + "\n//#FIELDS_DECLARATION#")
                    .Replace        ("#FIELDS_PRESEARCH#"                   , tagPreSearch + "\n//#FIELDS_PRESEARCH#")
                    .Replace        ("#FIELDS_SEARCH#"                      , tagSearch + "\n//#FIELDS_SEARCH#")
                    .Replace        ("#FIELDS_INITIALIZATION#"              , fieldsInitialization + "\n//#FIELDS_INITIALIZATION#")
                    .ReplaceOrErase ("#FIELDS_LOAD#"                        , configurationLoad + "\n//#FIELDS_LOAD#"               , configurable)
                    .ReplaceOrErase ("#FIELDS_PRECREATION#"                 , configurationPreCreation + "\n//#FIELDS_PRECREATION#" , configurable)
                    .ReplaceOrErase ("#FIELDS_CREATION#"                    , configurationCreation + "\n//#FIELDS_CREATION#"       , configurable)
                    .Replace        ("#FIELDS_CLONATION#"                   , fieldsClonation + "\n//#FIELDS_CLONATION#");
                File.WriteAllText(path, template);

                //string VEPath = evaluatorsVEsFolder + "/Editor/" + className + "VE.cs";
                string VEPath = evaluatorsVEsFolder + "/" + className + "VE.cs";

                string VE = GetText(VISUAL_ELEMENT_GUID)
                    .ReplaceOrErase ("#CONFIGURATION_NAMESPACES#"   , GetText(VE_CONFIGURATION_NAMESPACES)  , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_BUTTON_A#"     , GetText(VE_CONFIGURATION_BUTTON_A)    , configurable)
                    .ReplaceOrErase ("#CONFIGURATION_BUTTON_B#"     , GetText(VE_CONFIGURATION_BUTTON_B)    , configurable)
                    .Replace        ("#TARGETNAME#"                 , className);
                File.WriteAllText(VEPath, VE);

                AssetDatabase.Refresh();
            };
        }

        public static void AddParameter(string evalName, string evalPath, Type type, string initValue)
        {
            bool list = false;
            if(type.GenericTypeArguments.Length > 0)
            {
                type = type.GenericTypeArguments[0];
                list = true;
            }
            GetDummyField(new(type, list), initValue, out string declaration, out string initialization, out string load, out string preCreation, out string creation, out string clonation, out string _tagPreSearch, out string _tagSearch,
                        out string permaDeclaration, out string permaPreSearch, out string permaSearch, out string permaPostSearch, out string permaInitialization, out string permaClonation,
                        out string fieldFinalName, evalName);
            string fileText = File.ReadAllText(evalPath);
            fileText = fileText
                .Replace("#PERMA_DECLARATION#"      ,                               "\n\t\t"        + permaDeclaration      + "\n//#PERMA_DECLARATION#")
                .Replace("#PERMA_PRESEARCH#"        , /*GetText(PERMA_PRESEARCH)  +   */"\n\t\t\t"      + permaPreSearch        + "\n//#PERMA_PRESEARCH#")
                .Replace("#PERMA_SEARCH#"           , /*GetText(PERMA_SEARCH)     +   */"\n\t\t\t\t\t"  + permaSearch           + "\n//#PERMA_SEARCH#")
                .Replace("#PERMA_POSTSEARCH#"       ,                               "\n\t\t\t"      + permaPostSearch       + "\n//#PERMA_POSTSEARCH#")
                .Replace("#PERMA_INITIALIZATION#"   ,                               "\n\t\t\t"      + permaInitialization   + "\n//#PERMA_INITIALIZATION#")
                .Replace("#PERMA_CLONATION#"        ,                               "\n\t\t\t"      + permaClonation        + "\n//#PERMA_CLONATION#")
                .Replace("#FIELDS_DECLARATION#"     ,                               "\n\t\t"        + declaration           + "\n//#FIELDS_DECLARATION#")
                .Replace("#FIELDS_PRESEARCH#"       ,                               "\n\t\t\t"      + _tagPreSearch         + "\n//#FIELDS_PRESEARCH#")
                .Replace("#FIELDS_SEARCH#"          ,                               "\n\t\t\t\t\t"  + _tagSearch            + "\n//#FIELDS_SEARCH#")
                .Replace("#FIELDS_INITIALIZATION#"  ,                               "\n\t\t\t"      + initialization        + "\n//#FIELDS_INITIALIZATION#")
                .Replace("#FIELDS_LOAD#"            ,                               "\n\t\t\t"      + load                  + "\n//#FIELDS_LOAD#")
                .Replace("#FIELDS_PRECREATION#"     ,                               "\n\t\t\t"      + preCreation           + "\n//#FIELDS_PRECREATION#")
                .Replace("#FIELDS_CREATION#"        ,                               "\n\t\t\t\t"    + creation              + "\n//#FIELDS_CREATION#")
                .Replace("#FIELDS_CLONATION#"       ,                               "\n\t\t\t"      + clonation             + "\n//#FIELDS_CLONATION#")
                ;

            File.WriteAllText(evalPath, fileText);

            if (!type.Equals(typeof(LBS.Components.LBSTag)))
                return;

            string[] fileLines = File.ReadAllLines(evalPath);
            for(int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i].Contains("#PRESEARCH_CONDITION#"))
                {
                    string permaCondition = "perma" + fieldFinalName.UpperFirst() + " is null";
                    if (fileLines[i].Contains("false;"))
                    {
                        fileLines[i] = $"\n\t\t\tbool checkPermaIndices = ({permaCondition}) && bundleTM is not null; //#PRESEARCH_CONDITION#";
                    }
                    else
                    {
                        int ind = fileLines[i].IndexOf(") &&");
                        UnityEngine.Debug.Log(ind);
                        fileLines[i] = fileLines[i].Insert(ind, " || " + permaCondition);
                    }
                    break;
                }
            }
            File.WriteAllLines(evalPath, fileLines);

            AssetDatabase.Refresh();
        }

        public static void DeleteParameter(string evalName, string evalPath)
        {
            string[] fileLines = File.ReadAllLines(evalPath);
            for(int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i].Contains(evalName))
                {
                    if (fileLines[i].Contains("if") || fileLines[i].Contains("for") || fileLines[i].Contains("foreach"))
                    {
                        int j = i;
                        int brackets = 0;
                        do
                        {
                            j++;
                            if (fileLines[j].Contains('{'))
                                brackets++;
                            else if (fileLines[j].Contains('}'))
                                brackets--;
                            fileLines[j] = "";
                        } while (brackets > 0);
                    }
                    fileLines[i] = "";
                    continue;
                }

                string permaName = "perma" + evalName.UpperFirst();
                if (fileLines[i].Contains(permaName))
                {
                    if (fileLines[i].Contains("#PRESEARCH_CONDITION#"))
                    {
                        string toRemove = permaName + " is null";
                        int ind = fileLines[i].IndexOf(toRemove);
                        fileLines[i] = fileLines[i].Remove(ind, toRemove.Length);
                        if (fileLines[i].Substring(ind, 4) == " || ")
                            fileLines[i] = fileLines[i].Remove(ind, 4);
                        else if (ind >= 4 && fileLines[i].Substring(ind - 4, 4) == " || ")
                            fileLines[i] = fileLines[i].Remove(ind - 4, 4);
                        else
                            fileLines[i] = fileLines[i].Replace("() && bundleTM is not null", "false");
                    }
                    else
                    {
                        fileLines[i] = "";
                    }
                    continue;
                }

                string indName = evalName + "Ind";
                if (fileLines[i].Contains(indName))
                    fileLines[i] = "";
            }
            File.WriteAllLines(evalPath, fileLines);
            AssetDatabase.Refresh();
        }

        private static string GetText(string fileGUID)
        {
            string nullString = string.Empty;
            if (string.IsNullOrWhiteSpace(fileGUID)) return nullString;
            string path = AssetDatabase.GUIDToAssetPath(fileGUID);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return nullString;
            return File.ReadAllText(path);
        }

        // Dummies. Dummies! DUMMIES! Remember how I said NOT to shoot at me? - Mad Dummy (2015)
        static Dictionary<Tuple<Type, bool>, int> typeCount = new();
        private static void GetDummyField(Tuple<Type, bool> type, string value, out string declaration, out string initialization, out string load, out string preCreation, out string creation, out string clonation, out string preTagSearch, out string tagSearch,
            out string permaDeclaration, out string permaPreSearch, out string permaSearch, out string permaPostSearch, out string permaInitialization, out string permaClonation,
            out string finalName, string name = "")
        {
            declaration = initialization = load = preCreation = creation = clonation = preTagSearch = tagSearch
                = permaDeclaration = permaPreSearch = permaSearch = permaPostSearch = permaInitialization = permaClonation = "";
            if (string.IsNullOrWhiteSpace(name))
            {
                if (typeCount.ContainsKey(type)) typeCount[type]++;
                else typeCount.Add(type, 1);
                name = $"{type.Item1.Name}{(type.Item2 ? "List" : "")}Field{(char)(64 + typeCount[type])}";
            }
            string permaName = "perma" + name.UpperFirst();

            switch (type.Item1.Name)
            {
                case nameof(PathfindingAlgorithm):
                    name = "searchType";
                    declaration = $"[SerializeField] public PathfindingAlgorithm {name};";
                    initialization = $"{name} = PathfindingAlgorithm.JPS_Plus;";
                    load = $"{name} = config.GetValue<PathfindingAlgorithm>(\"Pathfinding Algorithm\");";
                    creation = $"new EnumConfigurationField(\"Pathfinding Algorithm\", {name},\n\t\t\t\t" +
                        $"\"Method to use for calculating distances between items.\"),";
                    clonation = $"clone.{name} = {name};";
                    break;

                case nameof(LBS.Components.LBSTag) when !type.Item2:
                    declaration = $"[SerializeField, SerializeReference] public LBSCharacteristic {name};";
                    initialization = $"{name} = new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag(\"{value}\"));";
                    load = $"{name} = config.GetValue<LBSCharacteristic>(\"{name}\");";
                    creation = $"new MainTagField(\"{name}\", {name}.FirstTag().Label, {name}),";
                    clonation = $"clone.{name} = {name};";
                    preTagSearch = $"List<int> {name}Ind = new();";
                    tagSearch = $"if(genes[i].HasTag({name}.FirstTag()))\n" +
                        $"\t\t\t\t\t{{\n" +
                        $"\t\t\t\t\t\t{name}Ind.Add(i);\n" +
                        $"\t\t\t\t\t\tfound = true;\n" +
                        //$"\t\t\t\t\t\tcontinue;\n" +
                        $"\t\t\t\t\t}}";

                    permaDeclaration = $"private List<int> {permaName} = null;";
                    permaPreSearch = $"{permaName} ??= new List<int>();";
                    permaSearch = $"if(group.BundleData.HasTag({name}.FirstTag()))\n" +
                        $"\t\t\t\t\t{{\n" +
                        $"\t\t\t\t\t\t{permaName}.Add(i);\n" +
                        $"\t\t\t\t\t\tgroups.Add(group);\n" +
                        $"\t\t\t\t\t}}";
                    permaPostSearch = $"{name}Ind.AddRange({permaName});";
                    permaInitialization = $"{permaName} = null;";
                    permaClonation = $"clone.{permaName} = {permaName};";
                    break;

                case nameof(LBS.Components.LBSTag) when type.Item2:
                    declaration = $"[SerializeField, SerializeReference] public List<LBSCharacteristic> {name} = new List<LBSCharacteristic>();";
                    initialization = $"{name}.Clear();\n";
                    int tagNumber = 3;
                    for (int i = 0; i < tagNumber; i++)
                        initialization += $"\t\t\t{name}.Add(new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag(\"TAG NAME\")));\n";
                    load = $"{name}.Clear();\n" +
                        $"\t\t\t{name}.AddRange(config.GetValues<LBSCharacteristic>(\"{name}\"));";
                    preCreation = $"var _{name} = new List<System.Tuple<string, LBSCharacteristic>>();\n" +
                        $"\t\t\tfor(int i = 0; i < {name}.Count; i++)\n" +
                        $"\t\t\t\t_{name}.Add(new({name}[i].FirstTag().Label, {name}[i]));";
                    creation = $"new GroupedTagsField(\"{name}\", _{name}),";
                    clonation = $"clone.{name} = new List<LBSCharacteristic>({name});";
                    preTagSearch = $"List<int> {name}Ind = new();";
                    tagSearch = $"foreach(LBSCharacteristic LBSChar in {name})\n" +
                        $"\t\t\t\t\t{{\n" +
                        $"\t\t\t\t\t\tif(genes[i].HasTag(LBSChar.FirstTag()))\n" +
                        $"\t\t\t\t\t\t{{\n" +
                        $"\t\t\t\t\t\t\t{name}Ind.Add(i);\n" +
                        $"\t\t\t\t\t\t\tfound = true;\n" +
                        $"\t\t\t\t\t\t\tbreak;\n" +
                        $"\t\t\t\t\t\t}}\n" +
                        $"\t\t\t\t\t}}";

                    permaDeclaration = $"private List<int> {permaName} = null;";
                    permaPreSearch = $"{permaName} ??= new List<int>();";
                    permaSearch = $"if({name}.Any(cha => cha is not null && group.BundleData.HasTag(cha.FirstTag())))\n" +
                        $"\t\t\t\t\t{{\n" +
                        $"\t\t\t\t\t\t{permaName}.Add(i);\n" +
                        $"\t\t\t\t\t\tgroups.Add(group);\n" +
                        $"\t\t\t\t\t}}";
                    permaPostSearch = $"{name}Ind.AddRange({permaName});";
                    permaInitialization = $"{permaName} = null;";
                    permaClonation = $"clone.{permaName} = {permaName};";
                    break;

                case nameof(Int32):
                case nameof(Single):
                    string primitive = type.Item1.Name == nameof(Int32) ? "int" : "float";
                    string fullPrimitive = type.Item1.Name == nameof(Int32) ? "Integer" : "Float";

                    declaration = $"[SerializeField] public {primitive} {name};";
                    initialization = $"{name} = {value};";
                    load = $"{name} = config.GetValue<{primitive}>(\"{name}\");";
                    creation = $"new {fullPrimitive}ConfigurationField(\"{name}\", {name}),";
                    clonation = $"clone.{name} = {name};";
                    break;
            }
            finalName = name;
        }
    }
}
