using ISILab.Commons.Utility;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    [InitializeOnLoad]
    public class ConfigurableEvaluatorValidator
    {
        static readonly string[] requiredLabels =
        {
            "#FIELDS_DECLARATION#",
            "#FIELDS_PRESEARCH#",
            "#FIELDS_SEARCH#",
            "#FIELDS_INITIALIZATION#",
            "#FIELDS_LOAD#",
            "#FIELDS_PRECREATION#",
            "#FIELDS_CREATION#",
            "#FIELDS_CLONATION#",
            "#PERMA_DECLARATION#",
            "#PERMA_PRESEARCH#",
            "#PERMA_SEARCH#",
            "#PERMA_POSTSEARCH#",
            "#PERMA_INITIALIZATION#",
            "#PERMA_CLONATION#",
            "#PRESEARCH_CONDITION#"
        };

        static ConfigurableEvaluatorValidator()
        {
            string folder = LBSSettings.Instance.paths.evaluatorsPath;
            string warning = "Some evaluators are missing labels. Configurable evaluators requires these labels in order to create new parameters correctly.\n\n" +
                            "The following labels are missing:\n";

            CompilationPipeline.compilationFinished += _ =>
            {
                IEnumerable<Type> evaluators = Reflection.GetAllImplementationsOf(typeof(IConfigurableEvaluator));
                List<string> names = evaluators.Select(ev => ev.Name).ToList();
                bool validated = true;
                foreach (string name in names)
                {
                    string path = folder + Path.DirectorySeparatorChar + name + ".cs";
                    if (!File.Exists(path)) continue;
                    string fileText = File.ReadAllText(path);

                    foreach(string label in requiredLabels)
                    {
                        if (!fileText.Contains(label))
                        {
                            validated = false;
                            warning += $"(!) {name}\t{label}\n";
                        }
                    }

                    warning += "\n";
                }

                if(!validated) Debug.LogWarning(warning);
            };
        }
    }
}
