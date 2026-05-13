using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace ISILab.AI.Grammar
{

    public static class QuestScriptGenerator
    {
        private const string FolderPath = "Assets/ISILab/LBS/Plugin/Components/Data/Quest/Runtime/QuestTrigger/Generated";
        private const string Protection = ",Commons.Attributes.ReadOnlyIncludeChildren";
        public static void Generate(GrammarTerminal terminal)
        {
            string className = GetSafeClassName(terminal.id);
            string fileName = $"{className}Trigger";
            string scriptContent = BuildScriptContent(fileName, terminal);

            SaveScript(fileName, scriptContent);

            // Update Terminal metadata
            terminal.generatedClassName = fileName;
            EditorUtility.SetDirty(terminal);
        }

        private static string GetSafeClassName(string id)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(id.Replace(" ", "_")).Replace("_", "");
        }
        private static string BuildScriptContent(string className, GrammarTerminal terminal)
        {
            StringBuilder fieldDefs = new StringBuilder();
            StringBuilder fieldMaps = new StringBuilder();

            // 1. Only build the strings if fields actually exist
            if (terminal.fields != null && terminal.fields.Count > 0)
            {
                fieldDefs.AppendLine("    [Header(\"Grammar Fields\")]");
                foreach (var field in terminal.fields)
                {
                    string protectedAttribute = string.Empty;
                    if (field is IBundleStored) 
                        protectedAttribute = Protection;

                    string typeName = field.GetType().Name;
                    string cleanName = field.name.Replace(" ", "");
                    string displayName = $", InspectorName(\"{field.name}\")";

                    fieldDefs.AppendLine($"    [SerializeField{protectedAttribute}{displayName}] private {typeName} _{cleanName};");
                    fieldMaps.AppendLine($"        _{cleanName} = data.Fields.Find(f => f.name == \"{field.name}\") as {typeName};");
                }
            }

            // 2. The template now safely injects the strings (which will be empty if count is 0)
            return $@"using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.AI.Grammar;
using ISILab.LBS.Plugin.MapTools.Generators;
namespace ISILab.AI.Grammar
{{
    public class {className} : QuestTrigger 
    {{
        [Commons.Attributes.ReadOnly]
        [SerializeField] private GrammarTerminal _terminal;

    {fieldDefs.ToString()}
        protected override void SetData(QuestNodeData data) 
        {{
            _terminal = data.Terminal;
    {fieldMaps.ToString()}
        }}

        protected override bool CanComplete() => false;
    }}
}}";
        }

        private static void SaveScript(string fileName, string content)
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            string fullPath = Path.Combine(FolderPath, $"{fileName}.cs");
            File.WriteAllText(fullPath, content);

            Debug.Log($"[QuestGenerator] Script saved to: {fullPath}");
        }
    }

}