using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace ISILab.AI.Grammar
{
    public static class QuestScriptGenerator
    {
        private const string FolderPath = "Assets/ISILab/LBS/Plugin/Components/Data/Quest/Runtime/QuestTrigger/Generated";
        private const string Protection = ", Commons.Attributes.ReadOnlyIncludeChildren";

        // Indentation Constants for readability
        private const string Tab1 = "    ";
        private const string Tab2 = "        ";
        private const string Tab3 = "            ";

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

            if (terminal.fields != null && terminal.fields.Count > 0)
            {
                fieldDefs.AppendLine($"{Tab2}[Header(\"Grammar Fields\")]");
                foreach (var field in terminal.fields)
                {
                    string protectedAttr = field is IBundleStored ? Protection : string.Empty;
                    string typeName = field.GetType().Name;
                    string cleanName = field.name.Replace(" ", "");
                    string displayName = $", InspectorName(\"{field.name}\")";

                    // 1. Define the class-level field (The persistent storage)
                    fieldDefs.AppendLine($"{Tab2}[SerializeField{protectedAttr}{displayName}]");
                    fieldDefs.AppendLine($"{Tab2}private {typeName} _{cleanName};");
                    fieldDefs.AppendLine();

                    // 2. Map and Instantiate Logic: 
                    // Instantiates the object if it is null, finds its source metadata, and sets values safely.
                    fieldMaps.AppendLine($"{Tab3}// Ensure the target field is instantiated so it isn't null");
                    fieldMaps.AppendLine($"{Tab3}if (_{cleanName} == null) _{cleanName} = new {typeName}();");
                    fieldMaps.AppendLine();
                    fieldMaps.AppendLine($"{Tab3}var source{cleanName} = fields.Find(f => f.name == \"{field.name}\") as {typeName};");
                    fieldMaps.AppendLine($"{Tab3}if (source{cleanName} != null)");
                    fieldMaps.AppendLine($"{Tab3}{{");
                    fieldMaps.AppendLine($"{Tab3}{Tab1}_{cleanName}.SetValue(source{cleanName}.value);");
                    fieldMaps.AppendLine($"{Tab3}}}");
                }
            }

            return $@"using UnityEngine;
using System.Collections.Generic;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.MapTools.Generators;

namespace ISILab.AI.Grammar
{{
    public class {className} : QuestTriggerNode
    {{
{fieldDefs.ToString().TrimEnd()}

        protected override void BindFields(List<GrammarField> fields) 
        {{
{fieldMaps.ToString().TrimEnd()}
        }}

        protected override bool CanComplete() => true;
    }}
}}".Trim();
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